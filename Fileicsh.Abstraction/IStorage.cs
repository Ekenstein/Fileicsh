using System;
using System.Collections.Async;
using System.Threading;
using System.Threading.Tasks;

namespace Fileicsh.Abstraction
{
    /// <inheritdoc />
    /// <summary>
    /// An abstraction of a storage which stores files
    /// associated with a certain tag.
    /// Invariants:
    /// foreach (file, tag) => storage.CreateFileAsync(file, tag) => GetFiles(tag).Contains(file)
    /// </summary>
    /// <remarks>
    /// A tag is not guaranteed to sustain the same form in order to respect
    /// various quirks in the underlying storage.
    /// However, it is always guaranteed that a tag used to create a file
    /// can be used to fetch the same file.
    /// </remarks>
    public interface IStorage : IDisposable
    {
        /// <summary>
        /// Returns an asynchronous collection of zero or more unique tags that got files associated with them.
        /// </summary>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> that contains zero ore more unique tags that got files associated with them.
        /// </returns>
        IAsyncEnumerable<AlphaNumericString> GetTags();

        /// <summary>
        /// Returns the file corresponding to the given <paramref name="file"/> associated with the 
        /// given <paramref name="tag"/> or null if the file couldn't be found.
        /// </summary>
        /// <param name="file">The information about the file.</param>
        /// <param name="tag">The tag the file is associated with.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that contains the retrieved file.</returns>
        Task<IFile> GetFileAsync(IFileInfo file, AlphaNumericString tag, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates the given <paramref name="file"/> and associates
        /// the file with the given <paramref name="tag"/>. If there is a file with the same name file name, associated with the same tag,
        /// the file will be replaced with the given <paramref name="file"/>.
        /// Invariants: (file, tag) => storage.CreateFileAsync(file, tag) => storage.GetFiles(tag).Contains(file)
        /// </summary>
        /// <param name="file">The file to create.</param>
        /// <param name="tag">The tag that should be associated with the file.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing
        /// a flag indicating whether the file was successfully created or not.</returns>
        Task<bool> CreateFileAsync(IFile file, AlphaNumericString tag, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes the file corresponding to the given <paramref name="file"/> associated
        /// with the given <paramref name="tag"/>.
        /// Invariants: (file, tag) => storage.DeleteFileAsync(file, tag) => !storage.GetFiles(tag).Contains(file)
        /// </summary>
        /// <param name="file">The file to delete.</param>
        /// <param name="tag">The tag the file is associated with.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that contains a flag indicating the file could be successfully deleted or not.</returns>
        Task<bool> DeleteFileAsync(IFileInfo file, AlphaNumericString tag, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes all the files associated with the given <paramref name="tag"/>.
        /// </summary>
        /// <param name="tag">The tag of the files to be removed.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that contains a flag indicating indicating whether all the files associated with the tag was successfully deleted or not.</returns>
        Task<bool> DeleteTagAsync(AlphaNumericString tag, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a collection zero or more files associated with the given <paramref name="tag"/>.
        /// </summary>
        /// <param name="tag">The tag the files should be associated with.</param>
        /// <returns>A asynchronous collection of zero or more files associated with the given <paramref name="tag"/>.</returns>
        IAsyncEnumerable<IFile> GetFiles(AlphaNumericString tag);

        /// <summary>
        /// Associates the given <paramref name="file"/>, currently associated with the given <paramref name="tag"/>,
        /// with the given <paramref name="destinationTag"/>.
        /// Invariants forall (file, tag, newTag) => storage.MoveFileAsync(file, tag, newTag) => !storage.GetFiles(tag).Contains(file) AND storage.GetFiles(newTag).Contains(file)
        /// </summary>
        /// <param name="file">The file to be associated with the new tag.</param>
        /// <param name="tag">The tag that the file is currently associated with.</param>
        /// <param name="destinationTag">The new tag the file should be associated with.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        Task MoveFileAsync(IFileInfo file, AlphaNumericString tag, AlphaNumericString destinationTag, CancellationToken cancellationToken = default);
    }
}
