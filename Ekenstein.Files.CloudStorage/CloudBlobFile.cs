using HeyRed.Mime;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ekenstein.Files.CloudStorage
{
    public class CloudBlobFile : IFile
    {
        private readonly ICloudBlob _cloudBlob;

        public CloudBlobFile(ICloudBlob cloudBlob)
        {
            _cloudBlob = cloudBlob ?? throw new ArgumentNullException(nameof(cloudBlob));
        }

        public string FileName => _cloudBlob.Name;

        public string ContentType => _cloudBlob.Properties?.ContentType ?? MimeTypesMap.GetMimeType(FileName);

        public Task CopyToAsync(Stream outputStream, CancellationToken cancellationToken = default(CancellationToken)) => _cloudBlob
            .DownloadToStreamAsync(outputStream, cancellationToken);

        public void Dispose()
        {
        }

        public Task<Stream> OpenReadStreamAsync(CancellationToken cancellationToken = default(CancellationToken)) => _cloudBlob
            .OpenReadAsync(cancellationToken);
    }
}
