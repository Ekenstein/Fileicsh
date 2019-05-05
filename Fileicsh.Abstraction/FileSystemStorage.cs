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

        private string GetTagPath(AlphaNumericString tag) => Path.Combine(_rootPath, tag);
        private string GetFilePath(AlphaNumericString tag, IFileInfo file) => Path.Combine(GetTagPath(tag), file.FileName);

        public async Task<bool> CreateFileAsync(IFile file, AlphaNumericString tag, CancellationToken cancellationToken = default)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            var tagPath = GetTagPath(tag);
            if (!Directory.Exists(tagPath))
            {
                Directory.CreateDirectory(tagPath);
            }

            var filePath = GetFilePath(tag, file);

            using (var fs = File.Open(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fs, cancellationToken);
                await fs.FlushAsync(cancellationToken);
            }

            return true;
        }

        public Task<bool> DeleteFileAsync(IFileInfo file, AlphaNumericString tag, CancellationToken cancellationToken = default)
        {
            var filePath = GetFilePath(tag, file);
            if (!File.Exists(filePath))
            {
                return Task.FromResult(false);
            }

            File.Delete(filePath);

            var tagPath = GetTagPath(tag);
            if (tag != AlphaNumericString.Empty && 
                !Directory.GetFiles(tagPath).Any() && 
                !Directory.GetDirectories(tagPath).Any())
            {
                Directory.Delete(tagPath);
            }

            return Task.FromResult(true);
        }

        public Task<bool> DeleteTagAsync(AlphaNumericString tag, CancellationToken cancellationToken = default)
        {
            var path = GetTagPath(tag);
            if (!Directory.Exists(path))
            {
                return Task.FromResult(false);
            }

            var filesInTag = Directory.GetFiles(path);
            foreach (var file in filesInTag)
            {
                File.Delete(file);
            }

            if (tag != AlphaNumericString.Empty)
            {
                Directory.Delete(path);
            }

            return Task.FromResult(true);
        }

        public void Dispose()
        {
        }

        public Task<IFile> GetFileAsync(IFileInfo fileInfo, AlphaNumericString tag, CancellationToken cancellationToken = default)
        {
            var filePath = GetFilePath(tag, fileInfo);
            if (!File.Exists(filePath))
            {
                return Task.FromResult<IFile>(null);
            }

            var file = new FileSystemFile(filePath);
            return Task.FromResult<IFile>(file);
        }

        public IAsyncEnumerable<IFile> GetFiles(AlphaNumericString tag)
        {
            var tagPath = GetTagPath(tag);
            if (!Directory.Exists(tagPath))
            {
                return AsyncEnumerable.Empty<IFile>();
            }

            return Directory.GetFiles(tagPath)
                .Select(path => new FileSystemFile(path))
                .Cast<IFile>()
                .ToAsyncEnumerable();
        }

        public IAsyncEnumerable<AlphaNumericString> GetTags()
        {
            var directories = Directory
                .GetDirectories(_rootPath)
                .Where(p => Directory.GetFiles(p).Any())
                .Select(Path.GetFileName)
                .Select(s => new AlphaNumericString(s));

            if (Directory.GetFiles(_rootPath).Any())
            {
                directories = directories.Concat(new[] { AlphaNumericString.Empty});
            }

            return directories.ToAsyncEnumerable();
        }

        public Task MoveFileAsync(IFileInfo file, AlphaNumericString tag, AlphaNumericString destinationTag, CancellationToken cancellationToken = default)
        {
            var destinationTagPath = GetTagPath(destinationTag);

            if (!Directory.Exists(destinationTagPath))
            {
                Directory.CreateDirectory(destinationTagPath);
            }

            var filePath = GetFilePath(tag, file);
            var destinationPath = GetFilePath(destinationTag, file);
            File.Move(filePath, destinationPath);

            if (tag != string.Empty)
            {
                var tagPath = GetTagPath(tag);
                if (!Directory.GetDirectories(tagPath).Any() &&
                    !Directory.GetFiles(tagPath).Any())
                {
                    Directory.Delete(tagPath);
                }
            }

            return Task.CompletedTask;
        }
    }
}
