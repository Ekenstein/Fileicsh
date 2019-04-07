using System;
using System.Collections.Async;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fileicsh.Abstraction;
using Fileicsh.CloudStorage.Extensions;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

namespace Fileicsh.CloudStorage
{
    public class CloudStorage : IStorage
    {
        private static readonly AlphaNumericString EmptyContainer = new AlphaNumericString("empty");
        private readonly CloudBlobClient _client;

        public CloudStorage(CloudBlobClient blobClient)
        {
            _client = blobClient ?? throw new ArgumentNullException(nameof(blobClient));
        }

        private CloudBlobContainer GetContainer(AlphaNumericString tag)
        {
            /*
            A container name must be a valid DNS name, conforming to the following naming rules:

            Container names must start with a letter or number, and can contain only letters, numbers, and the dash (-) character.
            Every dash (-) character must be immediately preceded and followed by a letter or number; consecutive dashes are not permitted in container names.
            All letters in a container name must be lowercase.
            Container names must be from 3 through 63 characters long.
            */
            
            const int minLength = 3;
            const int maxLength = 63;
            const char paddingChar = '0';

            var containerName = tag.ToLower();

            if (containerName == string.Empty)
            {
                containerName = EmptyContainer;
            }

            if (containerName.Length < minLength)
            {
                containerName = containerName.PadRight(minLength, paddingChar);
            }

            if (containerName.Length > maxLength)
            {
                containerName = containerName.Substring(0, maxLength);
            }

            return _client.GetContainerReference(containerName);
        }

        public async Task<bool> CreateFileAsync(IFile file, AlphaNumericString tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            var container = GetContainer(tag);
            await container.CreateIfNotExistsAsync(cancellationToken);

            var fileReference = container.GetBlockBlobReference(file.FileName);
            fileReference.Properties.ContentType = file.ContentType;

            using (var readStream = await file.OpenReadStreamAsync(cancellationToken))
            {
                await fileReference.UploadFromStreamAsync(readStream, cancellationToken);
            }

            return true;
        }

        public async Task<bool> DeleteFileAsync(IFileInfo file, AlphaNumericString tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            var container = GetContainer(tag);
            if (!await container.ExistsAsync(cancellationToken))
            {
                return false;
            }

            var fileReference = container.GetBlockBlobReference(file.FileName);
            return await fileReference.DeleteIfExistsAsync(cancellationToken);
        }

        public async Task<bool> DeleteTagAsync(AlphaNumericString tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            var container = GetContainer(tag);
            return await container.DeleteIfExistsAsync(cancellationToken);
        }

        public void Dispose()
        {
        }

        public async Task<IFile> GetFileAsync(IFileInfo fileInfo, AlphaNumericString tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            var container = GetContainer(tag);
            if (!await container.ExistsAsync(cancellationToken))
            {
                return null;
            }

            var file = container.GetBlockBlobReference(fileInfo.FileName);
            if (!await file.ExistsAsync(cancellationToken))
            {
                return null;
            }

            return new CloudBlobFile(file);
        }

        public IAsyncEnumerable<IFile> GetFiles(AlphaNumericString tag)
        {
            return new AsyncEnumerable<IFile>(async yield =>
            {
                var container = GetContainer(tag);
                if (!await container.ExistsAsync())
                {
                    yield.Break();
                }

                BlobContinuationToken continuationToken = null;
                do
                {
                    var blobs = await container
                        .ListBlobsSegmentedAsync(continuationToken)
                        .ConfigureAwait(false);

                    foreach (var blob in blobs.Results.OfType<CloudBlockBlob>())
                    {
                        await blob.FetchAttributesAsync(yield.CancellationToken);
                        await yield.ReturnAsync(new CloudBlobFile(blob)).ConfigureAwait(false);
                    }

                    continuationToken = blobs.ContinuationToken;
                } while (continuationToken != null);
            });
        }

        public IAsyncEnumerable<AlphaNumericString> GetTags()
        {
            return new AsyncEnumerable<AlphaNumericString>(async yield =>
            {
                var containers = await _client.ListContainersAsync();
                var tags = containers
                    .Select(c => c.Name == EmptyContainer ? AlphaNumericString.Empty : new AlphaNumericString(c.Name));

                foreach (var tag in tags)
                {
                    var file = await GetFiles(tag).FirstOrDefaultAsync(yield.CancellationToken);
                    if (file != null)
                    {
                        await yield.ReturnAsync(tag);
                    }
                }
            });
        }

        public async Task MoveFileAsync(IFileInfo file, AlphaNumericString tag, AlphaNumericString destinationTag, CancellationToken cancellationToken = default(CancellationToken))
        {
            var container = GetContainer(tag);
            if (!await container.ExistsAsync(cancellationToken))
            {
                return;
            }

            var cloudBlobFile = container.GetBlockBlobReference(file.FileName);
            if (!await cloudBlobFile.ExistsAsync(cancellationToken))
            {
                return;
            }

            var destinationContainer = GetContainer(destinationTag);
            await destinationContainer.CreateIfNotExistsAsync(cancellationToken);
            
            var destinationBlob = destinationContainer.GetBlockBlobReference(file.FileName);
            await destinationBlob.StartCopyAsync(cloudBlobFile, cancellationToken);
            await cloudBlobFile.DeleteAsync(cancellationToken);
        }
        
        /// <summary>
        /// Creates a cloud storage which targets the development account.
        /// Make sure that Azure Storage Emulator is running for this storage to work.
        /// </summary>
        public static IStorage DevelopmentStorage
        {
            get
            {
                var client = CloudStorageAccount.DevelopmentStorageAccount.CreateCloudBlobClient();
                return new CloudStorage(client);
            }
        }
    }
}
