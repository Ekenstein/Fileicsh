using System.Collections.Async;
using System.Threading;
using System.Threading.Tasks;

namespace Fileicsh.Abstraction
{
    /// <summary>
    /// An <see cref="IStorage"/> that does absolutely nothing.
    /// </summary>
    public class NoOpStorage : IStorage
    {
        /// <summary>
        /// Does not create any file at all.
        /// </summary>
        /// <param name="file">The file to not create.</param>
        /// <param name="tag">The tag to not associate with the file.</param>
        /// <param name="cancellationToken">The cancellation token that has nothing to say.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public Task<bool> CreateFileAsync(IFile file, AlphaNumericString tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(false);
        }

        /// <summary>
        /// Does not delete any file at all.
        /// </summary>
        /// <param name="file">The file to not delete.</param>
        /// <param name="tag">The tag that the doesn't make any difference at all.</param>
        /// <param name="cancellationToken">The cancellation token that has nothing to say.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation, that will
        /// always contain false.
        /// </returns>
        public Task<bool> DeleteFileAsync(IFileInfo file, AlphaNumericString tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(false);
        }

        /// <summary>
        /// Does not delete any tag at all.
        /// </summary>
        /// <param name="tag">The tag that will not be deleted.</param>
        /// <param name="cancellationToken">The cancellation token that has nothing to say.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation, that will always
        /// return false.
        /// </returns>
        public Task<bool> DeleteTagAsync(AlphaNumericString tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(false);
        }

        /// <summary>
        /// Disposes nothing.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Will always return null.
        /// </summary>
        /// <param name="fileInfo">The file that will not be retrieved.</param>
        /// <param name="tag">The tag that doesn't make any difference at all.</param>
        /// <param name="cancellationToken">The cancellation token that has nothing to say.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation, that will always contain null.
        /// </returns>
        public Task<IFile> GetFileAsync(IFileInfo fileInfo, AlphaNumericString tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult<IFile>(null);
        }

        /// <summary>
        /// Will always return an empty collection of files.
        /// </summary>
        /// <param name="tag">The tag that doesn't make any difference at all.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> of <see cref="IFile"/> that will
        /// always return zero elements.
        /// </returns>
        public IAsyncEnumerable<IFile> GetFiles(AlphaNumericString tag)
        {
            return AsyncEnumerable<IFile>.Empty;
        }

        /// <summary>
        /// Will always return an empty collection of tags.
        /// </summary>
        /// <returns>
        /// An empty <see cref="IAsyncEnumerable{T}"/>.
        /// </returns>
        public IAsyncEnumerable<AlphaNumericString> GetTags()
        {
            return AsyncEnumerable<AlphaNumericString>.Empty;
        }

        /// <summary>
        /// Will not move any files at all.
        /// </summary>
        /// <param name="file">The file that will not be moved.</param>
        /// <param name="tag">The tag that doesn't make any difference at all.</param>
        /// <param name="destinationTag">The destination tag that doesn't make any difference at all.</param>
        /// <param name="cancellationToken">The cancellation token that has nothing to say.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public Task MoveFileAsync(IFileInfo file, AlphaNumericString tag, AlphaNumericString destinationTag, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }
    }
}
