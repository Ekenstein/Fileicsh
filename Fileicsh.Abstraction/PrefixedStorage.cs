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
        private readonly string _prefix;

        public PrefixedStorage(IStorage storage, string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                throw new ArgumentException("Prefix must not be null or white space.");
            }

            _prefix = prefix;
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        private string ToPrefixedTag(string tag) => _prefix + tag;
        private bool IsPrefixedTag(string tag) => tag.StartsWith(_prefix);
        private string FromPrefixedTag(string prefixedTag) => prefixedTag.Substring(_prefix.Length);

        public Task<bool> CreateFileAsync(IFile file, string tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _storage.CreateFileAsync(file, ToPrefixedTag(tag), cancellationToken);
        }

        public Task<bool> DeleteFileAsync(IFileInfo file, string tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _storage.DeleteFileAsync(file, ToPrefixedTag(tag), cancellationToken);
        }

        public Task<bool> DeleteTagAsync(string tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _storage.DeleteTagAsync(ToPrefixedTag(tag), cancellationToken);
        }

        public void Dispose() => _storage.Dispose();

        public Task<IFile> GetFileAsync(IFileInfo fileInfo, string tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _storage.GetFileAsync(fileInfo, ToPrefixedTag(tag), cancellationToken);
        }

        public IAsyncEnumerable<IFile> GetFiles(string tag) => _storage.GetFiles(ToPrefixedTag(tag));

        public async Task<IReadOnlyList<string>> GetTagsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var tags = await _storage.GetTagsAsync(cancellationToken);
            return tags
                .Where(IsPrefixedTag)
                .Select(FromPrefixedTag)
                .ToArray();
        }

        public Task MoveFileAsync(IFileInfo file, string tag, string destinationTag, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _storage.MoveFileAsync(file, ToPrefixedTag(tag), ToPrefixedTag(destinationTag), cancellationToken);
        }
    }
}
