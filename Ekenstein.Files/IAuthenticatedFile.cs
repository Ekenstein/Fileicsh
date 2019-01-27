using System.Threading.Tasks;

namespace Ekenstein.Files
{
    /// <inheritdoc />
    /// <summary>
    /// An abstraction of an authenticated file that can calculate
    /// the file's hash.
    /// </summary>
    public interface IAuthenticatedFile : IFile
    {
        /// <summary>
        /// The hash algorithm used to calculate the hash of the file.
        /// </summary>
        string HashAlgorithm { get; }

        /// <summary>
        /// Returns the hash of the file as a byte array.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> containing the hash of the file as a byte array.</returns>
        Task<byte[]> GetHashAsync();
    }
}
