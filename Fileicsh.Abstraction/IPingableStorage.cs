using System.Threading;
using System.Threading.Tasks;

namespace Fileicsh.Abstraction
{
    /// <summary>
    /// An abstraction for an <see cref="IStorage"/> that can be
    /// pinged in order to check its availability.
    /// </summary>
    public interface IPingableStorage : IStorage
    {
        /// <summary>
        /// Pings the underlying storage and returns a flag
        /// indicating whether the storage is available or not.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation, containing
        /// a flag indicating whether the storage is available or not.
        /// True if the storage is available, otherwise false.
        /// </returns>
        Task<bool> PingAsync(CancellationToken cancellationToken = default);
    }
}
