using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fileicsh.Abstraction
{
    /// <inheritdoc />
    /// <summary>
    /// An abstraction of a storage which stores files
    /// associated with a certain tag.
    /// </summary>
    public interface IStorage : IDisposable
    {
        /// <summary>
        /// Returns a list of unique tags located at the storage.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that contains the retrieved tags.</returns>
        Task<IReadOnlyList<string>> GetTagsAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns the file corresponding to the given <paramref name="fileInfo"/> associated with the 
        /// given <paramref name="tag"/> or null if the file couldn't be found.
        /// </summary>
        /// <param name="fileInfo">The information about the file.</param>
        /// <param name="tag">The tag the file is associated with.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that contains the retrieved file.</returns>
        Task<IFile> GetFileAsync(IFileInfo fileInfo, string tag, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Creates the given <paramref name="file"/> and associates
        /// the file with the given <paramref name="tag"/>.
        /// </summary>
        /// <param name="file">The file to create.</param>
        /// <param name="tag">The tag that should be associated with the file.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        Task CreateFileAsync(IFile file, string tag, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Deletes the file corresponding to the given <paramref name="file"/> associated
        /// with the given <paramref name="tag"/>.
        /// </summary>
        /// <param name="file">The file to delete.</param>
        /// <param name="tag">The tag the file is associated with.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that contains a flag indicating the file could be successfully deleted or not.</returns>
        Task<bool> DeleteFileAsync(IFileInfo file, string tag, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Deletes the given <paramref name="tag"/> and all the files associated
        /// with the <paramref name="tag"/>.
        /// </summary>
        /// <param name="tag">The tag to be removed.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that contains a flag indicating indicating whether the tag was successfully deleted or not.</returns>
        Task<bool> DeleteTagAsync(string tag, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns a collection of files associated with the given <paramref name="tag"/>.
        /// </summary>
        /// <param name="tag">The tag the files should be associated with.</param>
        /// <returns>A asynchronous collection of zero or more files associated with the given <paramref name="tag"/>.</returns>
        IAsyncEnumerable<IFile> GetFiles(string tag);

        /// <summary>
        /// Associates the given <paramref name="file"/>, currently associated with the given <paramref name="tag"/>,
        /// with the given <paramref name="destinationTag"/>.
        /// </summary>
        /// <param name="file">The file to be associated with the new tag.</param>
        /// <param name="tag">The tag that the file is currently associated with.</param>
        /// <param name="destinationTag">The new tag the file should be associated with.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        Task MoveFileAsync(IFileInfo file, string tag, string destinationTag, CancellationToken cancellationToken = default(CancellationToken));
    }
}
