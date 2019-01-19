using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
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
            var files = await GetFilesAsync(tag);
            return files
                .FirstOrDefault(f => f.FileName == f.FileName);
        }

        public async Task<IEnumerable<IFile>> GetFilesAsync(string tag)
        {
            return await GetFilesInternalAsync(tag);
        }

        private async Task<IEnumerable<IFile<CloudBlockBlob>>> GetFilesInternalAsync(string tag)
        {
            var container = GetContainer(tag);
            if (!await container.ExistsAsync())
            {
                return Enumerable.Empty<IFile<CloudBlockBlob>>();
            }

            return new CloudBlockBlobEnumerator(container)
                .Select(blob => new CloudBlockBlobFile(blob));
        }

        private async Task<IFile<CloudBlockBlob>> GetFileInternalAsync(IFileInfo file, string tag)
        {
            var files = await GetFilesInternalAsync(tag);
            return files.FirstOrDefault(f => f.FileName == file.FileName);
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
            var cloudBlobFile = await GetFileInternalAsync(file, tag);
            if (cloudBlobFile == null)
            {
                return;
            }

            var destinationContainer = GetContainer(destinationTag);
            await destinationContainer.CreateIfNotExistsAsync();
            
            var destinationBlob = destinationContainer.GetBlockBlobReference(file.FileName);
            await destinationBlob.StartCopyAsync(cloudBlobFile.Extra);
            await cloudBlobFile.Extra.DeleteAsync();
        }
    }
}
