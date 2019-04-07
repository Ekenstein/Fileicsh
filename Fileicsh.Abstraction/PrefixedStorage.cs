using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fileicsh.Abstraction
{
    /// <summary>
    /// A storage which prefixes all the tags with a given prefix.
    /// </summary>
    public class PrefixedStorage : IStorage
    {
        private readonly IStorage _storage;

        /// <summary>
        /// The prefix that will prefix all the tags.
        /// </summary>
        public AlphaNumericString Prefix { get; }

        /// <summary>
        /// Creates a prefixed storage of the given <paramref name="storage"/> where
        /// all tags will be prefixed with the given <paramref name="prefix"/>.
        /// </summary>
        /// <param name="storage">The storage to prefix.</param>
        /// <param name="prefix">The non-null or empty tag that should prefix all the tags.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="storage"/> is null.</exception>
        /// <exception cref="ArgumentException">if <paramref name="prefix"/> is null or white space.</exception>
        public PrefixedStorage(IStorage storage, AlphaNumericString prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                throw new ArgumentException("Prefix must not be null or white space.");
            }

            Prefix = prefix;
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        private AlphaNumericString ToPrefixedTag(AlphaNumericString tag) => Prefix + tag;
        private bool IsPrefixedTag(AlphaNumericString tag) => tag.StartsWith(Prefix);

        private AlphaNumericString FromPrefixedTag(AlphaNumericString prefixedTag) => prefixedTag.Substring(Prefix.Length);

        /// <summary>
        /// Creates the given <paramref name="file"/> at the underlying storage
        /// and associates the file with the given <paramref name="tag"/> that will
        /// prefixed with <see cref="Prefix"/>.
        /// </summary>
        /// <param name="file">The file to create at the underlying storage.</param>
        /// <param name="tag">The tag that will be prefixed and associated with the file at the underlying storage.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// A flag indicating whether the file was successfully created at the underlying storage or not.
        /// </returns>
        public Task<bool> CreateFileAsync(IFile file, AlphaNumericString tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _storage.CreateFileAsync(file, ToPrefixedTag(tag), cancellationToken);
        }

        /// <summary>
        /// Deletes the given <paramref name="file"/>, associated with the given tag, from the underlying storage.
        /// The tag will prefixed with <see cref="Prefix"/> before being queried by the underlying storage.
        /// </summary>
        /// <param name="file">The file to delete from the underlying storage.</param>
        /// <param name="tag">The tag the file is associated with. The tag will be prefixed.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// A flag indicating whether the file was successfully deleted from the underlying storage or not.
        /// </returns>
        public Task<bool> DeleteFileAsync(IFileInfo file, AlphaNumericString tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _storage.DeleteFileAsync(file, ToPrefixedTag(tag), cancellationToken);
        }

        /// <summary>
        /// Deletes the given <paramref name="tag"/> from the underlying storage. The <paramref name="tag"/> will
        /// be prefixed with <see cref="Prefix"/> before queried at the underlying storage.
        /// </summary>
        /// <param name="tag">The tag that should be deleted. Will be prefixed.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// A flag indicating whether the tag was successfully deleted or not.
        /// </returns>
        public Task<bool> DeleteTagAsync(AlphaNumericString tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _storage.DeleteTagAsync(ToPrefixedTag(tag), cancellationToken);
        }

        /// <summary>
        /// Disposes the underlying storage.
        /// </summary>
        public void Dispose() => _storage.Dispose();

        /// <summary>
        /// Returns the file corresponding to the given <paramref name="file"/>, associated with the given <paramref name="tag"/>,
        /// from the underlying storage. The tag will be prefixed before queried at the underlying storage.
        /// </summary>
        /// <param name="file">The file to locate at the underlying storage.</param>
        /// <param name="tag">The tag the file is associated with. The tag will be prefixed.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The <see cref="IFile"/> from the underlying storage associated with the prefixed <paramref name="tag"/> or null,
        /// if there was no such file.
        /// </returns>
        public Task<IFile> GetFileAsync(IFileInfo file, AlphaNumericString tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _storage.GetFileAsync(file, ToPrefixedTag(tag), cancellationToken);
        }

        /// <summary>
        /// Returns all files associated with the tag prefixed with <see cref="Prefix"/>.
        /// </summary>
        /// <param name="tag">The tag the files should be associated when it is prefixed with <see cref="Prefix"/>.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> containing zero or more files associated with the <paramref name="tag"/>
        /// prefixed with <see cref="Prefix"/>.
        /// </returns>
        public IAsyncEnumerable<IFile> GetFiles(AlphaNumericString tag) => _storage.GetFiles(ToPrefixedTag(tag));

        /// <summary>
        /// Returns all the tags from the underlying storage that are prefixed with <see cref="Prefix"/>
        /// and removes the prefixes from the tags.
        /// </summary>
        /// <returns>
        /// An <see cref="IReadOnlyList{T}"/> collection of zero or more tags that are prefixed with <see cref="Prefix"/>.
        /// </returns>
        public IAsyncEnumerable<AlphaNumericString> GetTags()
        {
            return _storage.GetTags()
                .Where(IsPrefixedTag)
                .Select(FromPrefixedTag);
        }

        /// <summary>
        /// Moves the <paramref name="file"/> associated with the <paramref name="tag"/> when it is prefixed with <see cref="Prefix"/>
        /// to the <paramref name="destinationTag"/> that will be prefixed with <see cref="Prefix"/>.
        /// </summary>
        /// <param name="file">The file to reassociate with a new tag.</param>
        /// <param name="tag">The tag it is currently associated with.</param>
        /// <param name="destinationTag">The new tag the file should be associated with.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public Task MoveFileAsync(IFileInfo file, AlphaNumericString tag, AlphaNumericString destinationTag, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _storage.MoveFileAsync(file, ToPrefixedTag(tag), ToPrefixedTag(destinationTag), cancellationToken);
        }
    }
}
