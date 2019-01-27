using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ekenstein.Files.CloudStorage
{
    public class CloudStorage : IStorage
    {
        private readonly CloudBlobClient _client;

        public CloudStorage(CloudBlobClient blobClient)
        {
            _client = blobClient ?? throw new ArgumentNullException(nameof(blobClient));
        }

        private CloudBlobContainer GetContainer(string tag)
        {
            var containerName = tag?.ToLower() ?? string.Empty;
            return _client.GetContainerReference(containerName);
        }

        public async Task CreateFileAsync(IFile file, string tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            var container = GetContainer(tag);
            await container.CreateIfNotExistsAsync(cancellationToken);

            var fileReference = container.GetBlockBlobReference(file.FileName);
            fileReference.Properties.ContentType = file.ContentType;

            using (var readStream = await file.OpenReadStreamAsync(cancellationToken))
            {
                await fileReference.UploadFromStreamAsync(readStream, cancellationToken);
            }
        }

        public async Task<bool> DeleteFileAsync(IFileInfo file, string tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            var container = GetContainer(tag);
            if (!await container.ExistsAsync(cancellationToken))
            {
                return false;
            }

            var fileReference = container.GetBlockBlobReference(file.FileName);
            return await fileReference.DeleteIfExistsAsync(cancellationToken);
        }

        public async Task<bool> DeleteTagAsync(string tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            var container = GetContainer(tag);
            return await container.DeleteIfExistsAsync(cancellationToken);
        }

        public void Dispose()
        {
        }

        public async Task<IFile> GetFileAsync(IFileInfo fileInfo, string tag, CancellationToken cancellationToken = default(CancellationToken))
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

        public IAsyncEnumerable<IFile> GetFiles(string tag)
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
                        await blob.FetchAttributesAsync();
                        await yield.ReturnAsync(new CloudBlobFile(blob)).ConfigureAwait(false);
                    }

                    continuationToken = blobs.ContinuationToken;
                } while (continuationToken != null);
            });
        }

        public Task<IReadOnlyList<string>> GetTagsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var containers = _client
                .ListContainers()
                .Select(c => c.Name)
                .Union(new [] {string.Empty})
                .ToArray();

            return Task.FromResult<IReadOnlyList<string>>(containers);
        }

        public async Task MoveFileAsync(IFileInfo file, string tag, string destinationTag, CancellationToken cancellationToken = default(CancellationToken))
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
        public static IStorage DevelopmentStorage => new CloudStorage(CloudStorageAccount.DevelopmentStorageAccount.CreateCloudBlobClient());
    }
}
