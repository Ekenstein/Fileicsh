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
        public string RootDirectory { get; }
        private bool _disposed;

        /// <summary>
        /// Wraps the given <paramref name="sftpClient"/> in an <see cref="IStorage"/>
        /// which will operate from the given <paramref name="rootDirectory"/>.
        /// </summary>
        /// <param name="sftpClient">The sftp client to wrap into an <see cref="IStorage"/>.</param>
        /// <param name="rootDirectory">The root directory the storage will operate from.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="sftpClient"/> is null.</exception>
        /// <exception cref="ArgumentException">If <paramref name="rootDirectory"/> is null or white space.</exception>
        public SftpStorage(SftpClient sftpClient, string rootDirectory = "/")
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                throw new ArgumentException("Root directory must not be null or white space.");
            }

            _sftpClient = sftpClient ?? throw new ArgumentNullException(nameof(sftpClient));
            RootDirectory = rootDirectory;
        }

        private string GetTagPath(AlphaNumericString tag)
        {
            return Path.Combine(RootDirectory, tag)
                .Replace("\\", "/");
        }

        private string GetFilePath(IFileInfo fileInfo, AlphaNumericString tag)
        {
            return Path.Combine(GetTagPath(tag), fileInfo.FileName)
                .Replace("\\", "/");
        }

        /// <summary>
        /// Creates the <paramref name="file"/> at the SFTP backend. The file we be
        /// located in the path `<paramref name="tag"/>` relative to the <see cref="RootDirectory"/>.
        /// If a file with the same name as the given <paramref name="file"/> already exist,
        /// that file will be deleted.
        /// </summary>
        /// <param name="file">The file to create at the SFTP backend.</param>
        /// <param name="tag">The name of the folder relative to the <see cref="RootDirectory"/> that the file will be uploaded to. The tag will be escaped.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// A flag indicating whether the file was successfully uploaded or not.
        /// </returns>
        public async Task<bool> CreateFileAsync(IFile file, AlphaNumericString tag, CancellationToken cancellationToken = default)
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

        public async Task<bool> DeleteFileAsync(IFileInfo file, AlphaNumericString tag, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ConnectIfDisconnected();

            var filePath = GetFilePath(file, tag);

            cancellationToken.ThrowIfCancellationRequested();
            if (!_sftpClient.Exists(filePath))
            {
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();
            _sftpClient.DeleteFile(filePath);

            if (tag != string.Empty)
            {
                var tagPath = GetTagPath(tag);
                var filesInTag = await _sftpClient.ListDirectoryAsync(GetTagPath(tag), cancellationToken);
                if (!filesInTag.Any(f => f.Name != ".." && f.Name != "."))
                {
                    _sftpClient.DeleteDirectory(tagPath);
                }
            }

            return true;
        }

        public async Task<bool> DeleteTagAsync(AlphaNumericString tag, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ConnectIfDisconnected();
            
            var path = GetTagPath(tag);
            if (!_sftpClient.Exists(path))
            {
                return false;
            }

            var files = await _sftpClient.ListDirectoryAsync(path, cancellationToken);
            foreach (var file in files.Where(f => f.IsRegularFile))
            {
                file.Delete();
            }

            if (!files.Any(f => f.IsDirectory()) && 
                tag != string.Empty)
            {
                _sftpClient.DeleteDirectory(path);
            }

            return true;
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

        public Task<IFile> GetFileAsync(IFileInfo fileInfo, AlphaNumericString tag, CancellationToken cancellationToken = default)
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

        public IAsyncEnumerable<IFile> GetFiles(AlphaNumericString tag)
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
                var files = await _sftpClient.ListDirectoryAsync(tagPath, yield.CancellationToken);
                
                foreach (var file in files.Where(f => f.IsRegularFile))
                {
                    await yield.ReturnAsync(new SftpRegularFile(_sftpClient.ConnectionInfo, file));
                }
            });
        }

        public IAsyncEnumerable<AlphaNumericString> GetTags()
        {
            ThrowIfDisposed();
            ConnectIfDisconnected();

            return new AsyncEnumerable<AlphaNumericString>(async yield =>
            {
                ThrowIfDisposed();
                ConnectIfDisconnected();

                var rootFiles = await _sftpClient.ListDirectoryAsync(RootDirectory, yield.CancellationToken);
                var directories = rootFiles
                    .Where(f => f.IsDirectory());

                foreach (var directory in directories)
                {
                    ThrowIfDisposed();
                    ConnectIfDisconnected();
                    var files = await _sftpClient.ListDirectoryAsync(directory.FullName, yield.CancellationToken);
                    if (files.Any(f => f.IsRegularFile))
                    {
                        await yield.ReturnAsync(new AlphaNumericString(directory.Name));
                    }
                }

                if (rootFiles.Any(f => f.IsRegularFile))
                {
                    await yield.ReturnAsync(AlphaNumericString.Empty);
                }
            });
        }

        public Task MoveFileAsync(IFileInfo file, AlphaNumericString tag, AlphaNumericString destinationTag, CancellationToken cancellationToken = default)
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

            public Task CopyToAsync(Stream outputStream, CancellationToken cancellationToken = default)
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

            public Task<Stream> OpenReadStreamAsync(CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ThrowIfDisposed();
                ConnectIfDisconnected();
                return Task.FromResult<Stream>(_client.OpenRead(_file.FullName));
            }
        }
    }
}
