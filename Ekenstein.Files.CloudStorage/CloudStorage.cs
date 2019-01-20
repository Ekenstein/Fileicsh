using HeyRed.Mime;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public async Task CreateFileAsync(IFile file, string tag)
        {
            var container = GetContainer(tag);
            await container.CreateIfNotExistsAsync();

            var fileReference = container.GetBlockBlobReference(file.FileName);
            fileReference.Properties.ContentType = file.ContentType;

            using (var readStream = await file.OpenReadStreamAsync())
            {
                await fileReference.UploadFromStreamAsync(readStream);
            }
        }

        public async Task<bool> DeleteFileAsync(IFileInfo file, string tag)
        {
            var container = GetContainer(tag);
            if (!await container.ExistsAsync())
            {
                return false;
            }

            var fileReference = container.GetBlockBlobReference(file.FileName);
            return await fileReference.DeleteIfExistsAsync();
        }

        public async Task<bool> DeleteTagAsync(string tag)
        {
            var container = GetContainer(tag);
            return await container.DeleteIfExistsAsync();
        }

        public void Dispose()
        {
        }

        public async Task<IFile> GetFileAsync(IFileInfo fileInfo, string tag)
        {
            var container = GetContainer(tag);
            if (!await container.ExistsAsync())
            {
                return null;
            }

            var file = container.GetBlockBlobReference(fileInfo.FileName);
            if (!await file.ExistsAsync())
            {
                return null;
            }

            return new CloudBlockBlobFile(file);
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
                        await yield.ReturnAsync(new CloudBlockBlobFile(blob)).ConfigureAwait(false);
                    }

                    continuationToken = blobs.ContinuationToken;
                } while (continuationToken != null);
            });
        }

        public Task<IReadOnlyList<string>> GetTagsAsync()
        {
            var containers = _client
                .ListContainers()
                .Select(c => c.Name)
                .Union(new [] {string.Empty})
                .ToArray();

            return Task.FromResult<IReadOnlyList<string>>(containers);
        }

        public async Task MoveFileAsync(IFileInfo file, string tag, string destinationTag)
        {
            var container = GetContainer(tag);
            if (!await container.ExistsAsync())
            {
                return;
            }

            var cloudBlobFile = container.GetBlockBlobReference(file.FileName);
            if (!await cloudBlobFile.ExistsAsync())
            {
                return;
            }

            var destinationContainer = GetContainer(destinationTag);
            await destinationContainer.CreateIfNotExistsAsync();
            
            var destinationBlob = destinationContainer.GetBlockBlobReference(file.FileName);
            await destinationBlob.StartCopyAsync(cloudBlobFile);
            await cloudBlobFile.DeleteAsync();
        }

        private class CloudBlockBlobFile : IFile
        {
            private readonly ICloudBlob _blob;

            public string FileName => _blob.Name;

            public string ContentType => _blob
                .Properties
                .ContentType ?? MimeTypesMap.GetMimeType(FileName);

            public CloudBlockBlobFile(ICloudBlob blob)
            {
                _blob = blob ?? throw new ArgumentNullException(nameof(blob));
            }

            public Task CopyToAsync(Stream outputStream) => _blob.DownloadToStreamAsync(outputStream);

            public void Dispose()
            {
            }

            public Task<Stream> OpenReadStreamAsync() => _blob.OpenReadAsync();
        }
    }
}
