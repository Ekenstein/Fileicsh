using System;
using System.IO;
using System.Threading.Tasks;

namespace Ekenstein.Files
{
    public interface IFile : IFileInfo, IDisposable
    {
        /// <summary>
        /// Copies the underlying data of the file to the given
        /// writable <paramref name="outputStream"/>.
        /// </summary>
        /// <param name="outputStream">The writable stream to copy the underlying data of the file to.</param>
        Task CopyToAsync(Stream outputStream);

        /// <summary>
        /// Opens and returns a readable stream of the underlying file.
        /// </summary>
        /// <returns>A readable stream of the underlying file.</returns>
        Task<Stream> OpenReadStreamAsync();
    }
}
