using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fileicsh.Abstraction;
using HeyRed.Mime;
using Microsoft.Azure.Storage.Blob;

namespace Fileicsh.CloudStorage
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

        public Task CopyToAsync(Stream outputStream, CancellationToken cancellationToken = default) => _cloudBlob
            .DownloadToStreamAsync(outputStream, cancellationToken);

        public void Dispose()
        {
        }

        public Task<Stream> OpenReadStreamAsync(CancellationToken cancellationToken = default) => _cloudBlob
            .OpenReadAsync(cancellationToken);
    }
}
