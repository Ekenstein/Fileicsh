using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Ekenstein.Files.CloudStorage
{
    public class CloudBlockBlobFile : IFile<CloudBlockBlob>
    {
        public string FileName => Extra.Name;

        public string ContentType => Extra.Properties
            .ContentType ?? "application/octet-stream";

        public CloudBlockBlob Extra { get; }

        public CloudBlockBlobFile(CloudBlockBlob blob, bool fetchAttributes = true)
        {
            Extra = blob ?? throw new ArgumentNullException(nameof(blob));
            if (fetchAttributes)
            {
                Extra.FetchAttributes();
            }
        }

        public Task CopyToAsync(Stream outputStream) => Extra.DownloadToStreamAsync(outputStream);

        public void Dispose()
        {
        }

        public Task<Stream> OpenReadStreamAsync() => Extra.OpenReadAsync();
    }
}
