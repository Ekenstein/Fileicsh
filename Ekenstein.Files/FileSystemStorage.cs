using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ekenstein.Files
{
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

        private string GetTagPath(string tag) => Path.Combine(_rootPath, tag);
        private string GetFilePath(string tag, IFileInfo file) => Path.Combine(GetTagPath(tag), file.FileName);

        public async Task CreateFileAsync(IFile file, string tag)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            using (var fs = File.OpenWrite(GetFilePath(tag, file))
            {
                await file.CopyToAsync(fs);
                await fs.FlushAsync();
            }
        }

        public Task DeleteFileAsync(IFileInfo file, string tag)
        {
            var filePath = GetFilePath(tag, file);
            if (!File.Exists(filePath))
            {
                return Task.FromResult(0);
            }

            File.Delete(filePath);
            return Task.FromResult(0);
        }

        public Task DeleteTagAsync(string tag)
        {
            var path = GetTagPath(tag);
            if (!Directory.Exists(path))
            {
                return Task.FromResult(0);
            }

            Directory.Delete(path, true);
            return Task.FromResult(0);
        }

        public void Dispose()
        {
        }

        public Task<IFile> GetFileAsync(IFileInfo fileInfo, string tag)
        {
            var filePath = GetFilePath(tag, fileInfo);
            if (!File.Exists(filePath))
            {
                return Task.FromResult<IFile>(null);
            }

            var file = new FileSystemFile(filePath);
            return Task.FromResult<IFile>(file);
        }

        public Task<IEnumerable<IFile>> GetFilesAsync(string tag)
        {
            var tagPath = GetTagPath(tag);
            if (!Directory.Exists(tagPath))
            {
                return Task.FromResult(Enumerable.Empty<IFile>());
            }

            var files = Directory.GetFiles(tagPath)
                .Select(path => new FileSystemFile(path));

            return Task.FromResult<IEnumerable<IFile>>(files);
        }

        public Task<IReadOnlyList<string>> GetTagsAsync()
        {
            var directories = Directory.GetDirectories(_rootPath).ToArray();
            return Task.FromResult<IReadOnlyList<string>>(directories);
        }
    }
}
