using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HeyRed.Mime;

namespace Fileicsh.Abstraction
{
    /// <inheritdoc />
    /// <summary>
    /// A file located on the file system.
    /// </summary>
    public class FileSystemFile : IFile
    {
        private readonly string _filePath;

        public string FileName => Path.GetFileName(_filePath);

        public string ContentType => MimeTypesMap.GetMimeType(FileName);

        public FileSystemFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path must not be null or white space.");
            }

            _filePath = filePath;
        }

        public async Task CopyToAsync(Stream outputStream, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var readStream = await OpenReadStreamAsync(cancellationToken))
            {
                await readStream.CopyToAsync(outputStream);
                await readStream.FlushAsync(cancellationToken);
            }
        }

        public void Dispose()
        {
        }

        public Task<Stream> OpenReadStreamAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Stream>(File.OpenRead(_filePath));
        }
    }
}
