using System;
using System.IO;
using System.Threading.Tasks;
using HeyRed.Mime;

namespace Ekenstein.Files
{
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

        public async Task CopyToAsync(Stream outputStream)
        {
            using (var readStream = await OpenReadStreamAsync())
            {
                await readStream.CopyToAsync(outputStream);
                await readStream.FlushAsync();
            }
        }

        public void Dispose()
        {
        }

        public Task<Stream> OpenReadStreamAsync() => Task.FromResult<Stream>(File.OpenRead(_filePath));
    }
}
