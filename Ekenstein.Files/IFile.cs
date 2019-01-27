using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ekenstein.Files
{
    /// <inheritdoc cref="IFileInfo" />
    /// <summary>
    /// An abstraction of a file containing information about the file and
    /// possibilities for downloading the file.
    /// </summary>
    public interface IFile : IFileInfo, IDisposable
    {
        /// <summary>
        /// Copies the underlying data of the file to the given
        /// writable <paramref name="outputStream"/>.
        /// </summary>
        /// <param name="outputStream">The writable stream to copy the underlying data of the file to.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        Task CopyToAsync(Stream outputStream, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Opens and returns a readable stream of the underlying file.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that contains the readable stream of the file.</returns>
        Task<Stream> OpenReadStreamAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}
