using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fileicsh.Abstraction.Extensions;

namespace Fileicsh.Abstraction
{
    /// <summary>
    /// A storage that stores all the files in memory.
    /// </summary>
    public class MemoryStorage : IStorage
    {
        private sealed class FileEqualityComparer : IEqualityComparer<IFileInfo>
        {
            public bool Equals(IFileInfo x, IFileInfo y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return string.Equals(x.FileName, y.FileName);
            }

            public int GetHashCode(IFileInfo obj)
            {
                unchecked
                {
                    return (obj.FileName.GetHashCode() * 397);
                }
            }
        }

        private static readonly IEqualityComparer<IFileInfo> FileComparer = new FileEqualityComparer();

        private readonly IDictionary<string, ISet<IFile>> _files = new Dictionary<string, ISet<IFile>>();

        /// <summary>
        /// Saves the file in memory and associates the file with the given <paramref name="tag"/>.
        /// If a file with the same name exists, the file will be replaced with the given <paramref name="file"/>.
        /// </summary>
        /// <param name="file">The file to save to the memory storage.</param>
        /// <param name="tag">The tag to associate the file with.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="file"/> is null.</exception>
        public async Task CreateFileAsync(IFile file, string tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            tag = tag ?? string.Empty;
            if (!_files.TryGetValue(tag, out var files))
            {
                files = new HashSet<IFile>(FileComparer);
                _files.Add(tag, files);
            }

            if (files.Contains(file))
            {
                files.Remove(file);
            }

            files.Add(await file.ToMemoryAsync());
        }

        /// <summary>
        /// Deletes the file associated with the given <paramref name="file"/> and <paramref name="tag"/>.
        /// If the file doesn't exist, false will be returned, otherwise true.
        /// </summary>
        /// <param name="file">The file to remove.</param>
        /// <param name="tag">The tag the file is located in.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// True if the file was successfully deleted, otherwise false if there was no file associated with the given <paramref name="file"/>
        /// and <paramref name="tag"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="file"/> is null.</exception>
        public Task<bool> DeleteFileAsync(IFileInfo file, string tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            tag = tag ?? string.Empty;

            if (!_files.TryGetValue(tag, out var files))
            {
                return Task.FromResult(false);
            }

            var existingFile = files.FirstOrDefault(f => f.FileName == file.FileName);
            if (existingFile == null)
            {
                return Task.FromResult(false);
            }

            files.Remove(existingFile);
            return Task.FromResult(true);
        }

        /// <summary>
        /// Deletes the given <paramref name="tag"/> and all the files associated with the <paramref name="tag"/>.
        /// </summary>
        /// <param name="tag">The tag to remove.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// True if the tag and all its underlying files were successfully deleted, otherwise false if there was
        /// no tag corresponding to the given <paramref name="tag"/>.
        /// </returns>
        public Task<bool> DeleteTagAsync(string tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            tag = tag ?? string.Empty;

            return Task.FromResult(_files.Remove(tag));
        }

        /// <summary>
        /// Disposes the memory storage and removes
        /// all the files stored in memory.
        /// </summary>
        public void Dispose() => _files.Clear();

        /// <summary>
        /// Returns the file associated with the given <paramref name="fileInfo"/> and <paramref name="tag"/>.
        /// If there was no file associated with the <paramref name="fileInfo"/> and <paramref name="tag"/>,
        /// null will be returned, otherwise the file.
        /// </summary>
        /// <param name="fileInfo">The information about the file to retrieve.</param>
        /// <param name="tag">The tag the file is associated with.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> containing the file associated with the <paramref name="fileInfo"/> and <paramref name="tag"/>.
        /// If no such file exists, the <see cref="Task{TResult}"/> will contain null.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="fileInfo"/> is null.</exception>
        public Task<IFile> GetFileAsync(IFileInfo fileInfo, string tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (fileInfo == null)
            {
                throw new ArgumentNullException(nameof(fileInfo));
            }

            tag = tag ?? string.Empty;
            var file = _files.TryGetValue(tag, out var files)
                ? files.FirstOrDefault(f => f.FileName == fileInfo.FileName)
                : null;

            return Task.FromResult(file);
        }

        /// <summary>
        /// Returns a collection of zero or more files associated with the given <paramref name="tag"/>.
        /// </summary>
        /// <param name="tag">The tag to retrieve all the associated files from.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> containing the zero or more files associated with the
        /// given <paramref name="tag"/>.
        /// </returns>
        public IAsyncEnumerable<IFile> GetFiles(string tag)
        {
            tag = tag ?? string.Empty;
            return _files.TryGetValue(tag, out var files)
                ? files.ToAsyncEnumerable()
                : AsyncEnumerable<IFile>.Empty;
        }

        /// <summary>
        /// Returns all the tags associated with this memory storage.
        /// <see cref="string.Empty"/> will always be included amongst the tags.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> containing a <see cref="IReadOnlyList{T}"/> of tags associated with this memory storage.
        /// </returns>
        public Task<IReadOnlyList<string>> GetTagsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tags = _files.Keys.Union(new [] {string.Empty}).ToArray();
            return Task.FromResult<IReadOnlyList<string>>(tags);
        }

        /// <summary>
        /// Re-associates the file corresponding to the given <paramref name="file"/> and <paramref name="tag"/>
        /// to the <paramref name="destinationTag"/>.
        /// If the <paramref name="tag"/> is equal to <paramref name="destinationTag"/>, no operation will be performed.
        /// </summary>
        /// <param name="file">The file to associate with the <paramref name="destinationTag"/>.</param>
        /// <param name="tag">The tag the file is currently associated with.</param>
        /// <param name="destinationTag">The tag the file should be associated with.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="file"/> is null.</exception>
        public async Task MoveFileAsync(IFileInfo file, string tag, string destinationTag, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            tag = tag ?? string.Empty;
            destinationTag = destinationTag ?? string.Empty;

            if (tag == destinationTag)
            {
                return;
            }

            var existingFile = await GetFileAsync(file, tag, cancellationToken);
            if (existingFile == null)
            {
                return;
            }

            await CreateFileAsync(existingFile, destinationTag, cancellationToken);
            await DeleteFileAsync(existingFile, tag, cancellationToken);
        }
    }
}
