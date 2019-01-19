using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ekenstein.Files
{
    /// <summary>
    /// An abstraction of a storage which stores files
    /// associated with a certain tag.
    /// </summary>
    public interface IStorage : IDisposable
    {
        /// <summary>
        /// Returns a list of unique tags located at the storage.
        /// </summary>
        Task<IReadOnlyList<string>> GetTagsAsync();

        /// <summary>
        /// Returns the file corresponding to the given <paramref name="fileInfo"/> associated with the 
        /// given <paramref name="tag"/> or null if the file couldn't be found.
        /// </summary>
        /// <param name="fileInfo">The information about the file.</param>
        /// <param name="tag">The tag the file is associated with.</param>
        Task<IFile> GetFileAsync(IFileInfo fileInfo, string tag);

        /// <summary>
        /// Creates the given <paramref name="file"/> and associates
        /// the file with the given <paramref name="tag"/>.
        /// </summary>
        /// <param name="file">The file to create.</param>
        /// <param name="tag">The tag that should be associated with the file.</param>
        Task CreateFileAsync(IFile file, string tag);

        /// <summary>
        /// Deletes the file corresponding to the given <paramref name="file"/> associated
        /// with the given <paramref name="tag"/>.
        /// </summary>
        /// <param name="file">The file to delete.</param>
        /// <param name="tag">The tag the file is associated with.</param>
        /// <returns>True if the file could be successfully deleted, otherwise false.</returns>
        Task<bool> DeleteFileAsync(IFileInfo file, string tag);

        /// <summary>
        /// Deletes the given <paramref name="tag"/> and all the files associated
        /// with the <paramref name="tag"/>.
        /// </summary>
        /// <param name="tag">The tag to be removed.</param>
        /// <returns>True if the tag was successfully deleted, otherwise false.</returns>
        Task<bool> DeleteTagAsync(string tag);

        /// <summary>
        /// Returns a collection of files associated with the given <paramref name="tag"/>.
        /// </summary>
        /// <param name="tag">The tag the files should be associated with.</param>
        /// <returns>A collection of zero or more files associated with the given <paramref name="tag"/>.</returns>
        Task<IEnumerable<IFile>> GetFilesAsync(string tag);

        /// <summary>
        /// Associates the given <paramref name="file"/>, currently associated with the given <paramref name="tag"/>,
        /// with the given <paramref name="destinationTag"/>.
        /// </summary>
        /// <param name="file">The file to be associated with the new tag.</param>
        /// <param name="tag">The tag that the file is currently associated with.</param>
        /// <param name="destinationTag">The new tag the file should be associated with.</param>
        Task MoveFileAsync(IFileInfo file, string tag, string destinationTag);
    }
}
