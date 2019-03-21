using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fileicsh.Abstraction;
using Fileicsh.Extensions;
using HeyRed.Mime;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace Fileicsh.SFTP
{
    /// <inheritdoc />
    /// <summary>
    /// A storage targeting an SFTP backend.
    /// </summary>
    public class SftpStorage : IStorage
    {
        private readonly SftpClient _sftpClient;
        private readonly string _rootDirectory;
        private bool _disposed;

        public SftpStorage(SftpClient sftpClient, string rootDirectory = "/")
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                throw new ArgumentException("Root directory must not be null or white space.");
            }

            _sftpClient = sftpClient ?? throw new ArgumentNullException(nameof(sftpClient));
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

        public async Task<bool> CreateFileAsync(IFile file, string tag, CancellationToken cancellationToken = default(CancellationToken))
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

            using (var stream = await file.OpenReadStreamAsync(cancellationToken))
            {
                await _sftpClient.UploadFileAsync(filePath, stream, cancellationToken);
            }

            return true;
        }

        public Task<bool> DeleteFileAsync(IFileInfo file, string tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ConnectIfDisconnected();

            var filePath = GetFilePath(file, tag);

            cancellationToken.ThrowIfCancellationRequested();
            if (!_sftpClient.Exists(filePath))
            {
                return Task.FromResult(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            _sftpClient.DeleteFile(filePath);
            return Task.FromResult(true);
        }

        public Task<bool> DeleteTagAsync(string tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ConnectIfDisconnected();
            
            var path = GetTagPath(tag);
            return _sftpClient.DeleteDirectoryAsync(path, true, cancellationToken);
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

        public Task<IFile> GetFileAsync(IFileInfo fileInfo, string tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ConnectIfDisconnected();

            var filePath = GetFilePath(fileInfo, tag);

            cancellationToken.ThrowIfCancellationRequested();
            if (!_sftpClient.Exists(filePath))
            {
                return Task.FromResult<IFile>(null);
            }

            cancellationToken.ThrowIfCancellationRequested();
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
                var files = await _sftpClient.ListDirectoryAsync(tagPath);
                
                foreach (var file in files.Where(f => f.IsRegularFile))
                {
                    await yield.ReturnAsync(new SftpRegularFile(_sftpClient.ConnectionInfo, file));
                }
            });
        }

        public async Task<IReadOnlyList<string>> GetTagsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ConnectIfDisconnected();

            var files = await _sftpClient.ListDirectoryAsync(_rootDirectory, cancellationToken);
            return files
                .Where(f => f.IsDirectory)
                .Where(f => f.Name != "." && f.Name != "..")
                .Select(d => Uri.UnescapeDataString(d.Name))
                .Union(new[] { string.Empty })
                .ToArray();
        }

        public Task MoveFileAsync(IFileInfo file, string tag, string destinationTag, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ConnectIfDisconnected();

            var filePath = GetFilePath(file, tag);

            cancellationToken.ThrowIfCancellationRequested();
            if (!_sftpClient.Exists(filePath))
            {
                return Task.FromResult(0);
            }

            var destinationTagPath = GetTagPath(destinationTag);

            cancellationToken.ThrowIfCancellationRequested();
            if (!_sftpClient.Exists(destinationTagPath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                _sftpClient.CreateDirectory(destinationTagPath);
            }

            cancellationToken.ThrowIfCancellationRequested();
            var sftpFile = _sftpClient.Get(filePath);

            cancellationToken.ThrowIfCancellationRequested();
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

            public Task CopyToAsync(Stream outputStream, CancellationToken cancellationToken = default(CancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                ThrowIfDisposed();
                ConnectIfDisconnected();
                return _client.DownloadToStreamAsync(_file.FullName, outputStream, cancellationToken);
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

            public Task<Stream> OpenReadStreamAsync(CancellationToken cancellationToken = default(CancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                ThrowIfDisposed();
                ConnectIfDisconnected();
                return Task.FromResult<Stream>(_client.OpenRead(_file.FullName));
            }
        }
    }
}
