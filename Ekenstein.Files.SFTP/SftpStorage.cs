using HeyRed.Mime;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekenstein.Files.SFTP
{
    public class SftpStorage : IStorage
    {
        private readonly SftpClient _sftpClient;
        private readonly string _rootDirectory;
        private bool _disposed;

        public SftpStorage(string host, int port, string userName, string password, string rootDirectory = "/")
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                throw new ArgumentException("Root directory must not be null or white space.");
            }

            _sftpClient = new SftpClient(host, port, userName, password);
            _rootDirectory = rootDirectory;
        }

        private string GetTagPath(string tag)
        {
            return Path.Combine(_rootDirectory, Uri.EscapeDataString(tag ?? string.Empty))
                .Replace("\\", "/");
        }

        private string GetFilePath(IFileInfo fileInfo, string tag)
        {
            return Path.Combine(GetTagPath(tag), fileInfo.FileName)
                .Replace("\\", "/");
        }

        public async Task CreateFileAsync(IFile file, string tag)
        {
            ThrowIfDisposed();
            ConnectIfDisconnected();

            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            var tagPath = GetTagPath(tag);
            if (!_sftpClient.Exists(tagPath))
            {
                _sftpClient.CreateDirectory(tagPath);
            }

            var filePath = GetFilePath(file, tag);

            using (var stream = await file.OpenReadStreamAsync())
            {
                await Task.Factory.FromAsync(
                    _sftpClient.BeginUploadFile(stream, filePath),
                    result => _sftpClient.EndUploadFile(result));
            }
        }

        public Task<bool> DeleteFileAsync(IFileInfo file, string tag)
        {
            ThrowIfDisposed();
            ConnectIfDisconnected();

            var filePath = GetFilePath(file, tag);
            if (!_sftpClient.Exists(filePath))
            {
                return Task.FromResult(false);
            }

            _sftpClient.DeleteFile(filePath);
            return Task.FromResult(true);
        }

        public async Task<bool> DeleteTagAsync(string tag)
        {
            ThrowIfDisposed();
            ConnectIfDisconnected();
            
            var path = GetTagPath(tag);
            if (!_sftpClient.Exists(path))
            {
                return false;
            }

            var directory = _sftpClient.Get(path);
            await DeleteRecursiveAsync(directory);
            return true;
        }

        private async Task DeleteRecursiveAsync(SftpFile sftpFile)
        {
            if (sftpFile.Name == "." || sftpFile.Name == "..")
            {
                return;
            }
            
            if (sftpFile.IsDirectory)
            {
                var files = await ListFilesAsync(sftpFile.FullName);
                foreach (var file in files)
                {
                    await DeleteRecursiveAsync(file);
                }
            }

            _sftpClient.Delete(sftpFile.FullName);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_disposed) 
                { 
                    _sftpClient.Dispose();
                }

                _disposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        public Task<IFile> GetFileAsync(IFileInfo fileInfo, string tag)
        {
            ThrowIfDisposed();
            ConnectIfDisconnected();

            var filePath = GetFilePath(fileInfo, tag);
            if (!_sftpClient.Exists(filePath))
            {
                return Task.FromResult<IFile>(null);
            }

            var file = _sftpClient.Get(filePath);
            return Task.FromResult<IFile>(new SftpRegularFile(_sftpClient.ConnectionInfo, file));
        }

        public IAsyncEnumerable<IFile> GetFiles(string tag)
        {
            ThrowIfDisposed();
            ConnectIfDisconnected();

            var tagPath = GetTagPath(tag);
            if (!_sftpClient.Exists(tagPath))
            {
                return AsyncEnumerable.Empty<IFile>();
            }

            return new AsyncEnumerable<IFile>(async yield =>
            {
                var files = await ListFilesAsync(tagPath);
                
                foreach (var file in files.Where(f => f.IsRegularFile))
                {
                    await yield.ReturnAsync(new SftpRegularFile(_sftpClient.ConnectionInfo, file));
                }
            });
        }

        public async Task<IReadOnlyList<string>> GetTagsAsync()
        {
            ThrowIfDisposed();
            ConnectIfDisconnected();

            var files = await ListFilesAsync(_rootDirectory);
            return files
                .Where(f => f.IsDirectory)
                .Where(f => f.Name != "." && f.Name != "..")
                .Select(d => Uri.UnescapeDataString(d.Name))
                .Union(new[] { string.Empty })
                .ToArray();
        }

        private Task<IEnumerable<SftpFile>> ListFilesAsync(string directory)
        {
            return Task.Factory.FromAsync(_sftpClient.BeginListDirectory(directory, null, null), r => _sftpClient.EndListDirectory(r));
        }

        public Task MoveFileAsync(IFileInfo file, string tag, string destinationTag)
        {
            ThrowIfDisposed();
            ConnectIfDisconnected();

            var filePath = GetFilePath(file, tag);
            if (!_sftpClient.Exists(filePath))
            {
                return Task.FromResult(0);
            }

            var destinationTagPath = GetTagPath(destinationTag);
            if (!_sftpClient.Exists(destinationTagPath))
            {
                _sftpClient.CreateDirectory(destinationTagPath);
            }

            var sftpFile = _sftpClient.Get(filePath);
            sftpFile.MoveTo(GetFilePath(file, destinationTag));
            return Task.FromResult(0);
        }

        private void ConnectIfDisconnected()
        {
            if (!_sftpClient.IsConnected)
            {
                _sftpClient.Connect();
            }
        }

        private class SftpRegularFile : IFile
        {
            private bool _disposed;
            private readonly SftpClient _client;
            private readonly SftpFile _file;

            public string FileName => _file.Name;
            public string ContentType => MimeTypesMap.GetMimeType(FileName);

            public SftpRegularFile(ConnectionInfo connectionInfo, SftpFile file)
            {
                if (connectionInfo == null)
                {
                    throw new ArgumentNullException(nameof(connectionInfo));
                }

                _client = new SftpClient(connectionInfo);
                _file = file ?? throw new ArgumentNullException(nameof(file));
            }

            public Task CopyToAsync(Stream outputStream)
            {
                ThrowIfDisposed();
                ConnectIfDisconnected();
                return Task.Factory.FromAsync(_client.BeginDownloadFile(_file.FullName, outputStream), r => _client.EndDownloadFile(r));
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _client.Dispose();
                }
                _disposed = true;
            }

            private void ThrowIfDisposed()
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }
            }

            private void ConnectIfDisconnected()
            {
                if (!_client.IsConnected)
                {
                    _client.Connect();
                }
            }

            public Task<Stream> OpenReadStreamAsync()
            {
                ThrowIfDisposed();
                ConnectIfDisconnected();
                return Task.FromResult<Stream>(_client.OpenRead(_file.FullName));
            }
        }
    }
}
