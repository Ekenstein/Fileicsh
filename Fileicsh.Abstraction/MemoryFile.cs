using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Fileicsh.Abstraction
{
    /// <summary>
    /// A file containing a byte array of data.
    /// </summary>
    public class MemoryFile : IFile
    {
        private bool _disposed;
        private byte[] _data;

        /// <summary>
        /// The name of the file stored in memory.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// The content type of the file stored in memory.
        /// </summary>
        public string ContentType { get; }

        /// <summary>
        /// Creates a file containing the given <paramref name="data"/>.
        /// </summary>
        /// <param name="info">The info about the file.</param>
        /// <param name="data">The data of the file.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="data"/> or <paramref name="info"/> is null.</exception>
        public MemoryFile(IFileInfo info, byte[] data)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            _data = data ?? throw new ArgumentNullException(nameof(data));
            FileName = info.FileName;
            ContentType = info.ContentType;
        }

        /// <summary>
        /// Copies the data stored in memory to the given <paramref name="outputStream"/>.
        /// </summary>
        /// <param name="outputStream">The stream to copy the underlying data to.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">If the given <paramref name="outputStream"/> is null.</exception>
        /// <exception cref="ObjectDisposedException">If the file is disposed.</exception>
        public Task CopyToAsync(Stream outputStream, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();

            if (outputStream == null)
            {
                throw new ArgumentNullException(nameof(outputStream));
            }

            return outputStream.WriteAsync(_data, 0, _data.Length);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        /// <summary>
        /// Disposes the memory file.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _data = null;
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Opens a readable stream containing the underlying data.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> containing the readable stream of the underlying data.
        /// </returns>
        public Task<Stream> OpenReadStreamAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();

            var stream = new MemoryStream(_data);
            stream.Seek(0, SeekOrigin.Begin);
            return Task.FromResult<Stream>(stream);
        }
    }
}
