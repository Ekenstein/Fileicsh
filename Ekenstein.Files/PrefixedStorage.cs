using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ekenstein.Files
{
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

        public Task CreateFileAsync(IFile file, string tag) => _storage.CreateFileAsync(file, ToPrefixedTag(tag));

        public Task DeleteFileAsync(IFileInfo file, string tag) => _storage.DeleteFileAsync(file, ToPrefixedTag(tag));

        public Task DeleteTagAsync(string tag) => _storage.DeleteTagAsync(ToPrefixedTag(tag));

        public void Dispose() => _storage.Dispose();

        public Task<IFile> GetFileAsync(IFileInfo fileInfo, string tag) => _storage.GetFileAsync(fileInfo, ToPrefixedTag(tag));

        public Task<IEnumerable<IFile>> GetFilesAsync(string tag) => _storage.GetFilesAsync(ToPrefixedTag(tag));

        public async Task<IReadOnlyList<string>> GetTagsAsync()
        {
            var tags = await _storage.GetTagsAsync();
            return tags
                .Where(IsPrefixedTag)
                .Select(FromPrefixedTag)
                .ToArray();
        }
    }
}
