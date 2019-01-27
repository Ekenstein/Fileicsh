using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fileicsh.Abstraction
{
    /// <inheritdoc />
    /// <summary>
    /// A storage for handling moving/creating/deleting files
    /// on a file system.
    /// </summary>
    public class FileSystemStorage : IStorage
    {
        private readonly string _rootPath;

        public FileSystemStorage(string rootPath, bool createIfNotExist = false)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException("Root path must not be null or white space.");
            }

            if (createIfNotExist && !Directory.Exists(rootPath))
            {
                Directory.CreateDirectory(rootPath);
            }

            _rootPath = rootPath;
        }

        private string GetTagPath(string tag) => Path.Combine(_rootPath, Uri.EscapeDataString(tag));
        private string GetFilePath(string tag, IFileInfo file) => Path.Combine(GetTagPath(tag), file.FileName);

        public async Task CreateFileAsync(IFile file, string tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            using (var fs = File.OpenWrite(GetFilePath(tag, file)))
            {
                await file.CopyToAsync(fs, cancellationToken);
                await fs.FlushAsync(cancellationToken);
            }
        }

        public Task<bool> DeleteFileAsync(IFileInfo file, string tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            var filePath = GetFilePath(tag, file);
            if (!File.Exists(filePath))
            {
                return Task.FromResult(false);
            }

            File.Delete(filePath);
            return Task.FromResult(true);
        }

        public Task<bool> DeleteTagAsync(string tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            var path = GetTagPath(tag);
            if (!Directory.Exists(path))
            {
                return Task.FromResult(false);
            }

            Directory.Delete(path, true);
            return Task.FromResult(true);
        }

        public void Dispose()
        {
        }

        public Task<IFile> GetFileAsync(IFileInfo fileInfo, string tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            var filePath = GetFilePath(tag, fileInfo);
            if (!File.Exists(filePath))
            {
                return Task.FromResult<IFile>(null);
            }

            var file = new FileSystemFile(filePath);
            return Task.FromResult<IFile>(file);
        }

        public IAsyncEnumerable<IFile> GetFiles(string tag)
        {
            var tagPath = GetTagPath(tag);
            if (!Directory.Exists(tagPath))
            {
                return AsyncEnumerable.Empty<IFile>();
            }

            var files = Directory.GetFiles(tagPath)
                .Select(path => new FileSystemFile(path))
                .Cast<IFile>();

            return files.ToAsyncEnumerable();
        }

        public Task<IReadOnlyList<string>> GetTagsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var directories = Directory
                .GetDirectories(_rootPath)
                .Select(Uri.UnescapeDataString)
                .Union(new [] { string.Empty })
                .ToArray();
            return Task.FromResult<IReadOnlyList<string>>(directories);
        }

        public Task MoveFileAsync(IFileInfo file, string tag, string destinationTag, CancellationToken cancellationToken = default(CancellationToken))
        {
            var filePath = GetFilePath(tag, file);
            var destinationPath = GetFilePath(destinationTag, file);

            File.Move(filePath, destinationPath);
            return Task.FromResult(0);
        }
    }
}
