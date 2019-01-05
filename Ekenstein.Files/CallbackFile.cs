using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Ekenstein.Files
{
    public class CallbackFile : IFile
    {
        private readonly IFileInfo _fileInfo;
        private readonly Func<Task<Stream>> _readStream;
        private readonly Func<Stream, Task> _copyTo;

        private static async Task<Stream> GetReadStreamAsync(Func<Stream, Task> load)
        {
            var ms = new MemoryStream();
            await load(ms);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        private static async Task CopyStreamAsync(Stream outputStream, Func<Task<Stream>> readStream)
        {
            using (var stream = await readStream())
            {
                await stream.CopyToAsync(outputStream);
                await stream.FlushAsync();
            }
        }

        public CallbackFile(IFileInfo fileInfo, Func<Task<Stream>> readStream) : this(fileInfo, readStream, s => CopyStreamAsync(s, readStream))
        {
        }

        public CallbackFile(IFileInfo fileInfo, Func<Stream, Task> copyTo) : this(fileInfo, () => GetReadStreamAsync(copyTo), copyTo)
        {
        }

        public CallbackFile(IFileInfo fileInfo, Func<Task<Stream>> readStream, Func<Stream, Task> copyTo)
        {
            _fileInfo = fileInfo ?? throw new ArgumentNullException(nameof(fileInfo));
            _readStream = readStream ?? throw new ArgumentNullException(nameof(readStream));
            _copyTo = copyTo ?? throw new ArgumentNullException(nameof(copyTo));
        }

        public string FileName => _fileInfo.FileName;

        public string ContentType => _fileInfo.ContentType;

        public void Dispose()
        {
        }

        public Task CopyToAsync(Stream outputStream) => _copyTo(outputStream);

        public Task<Stream> OpenReadStreamAsync() => _readStream();
    }
}
