using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Ekenstein.Files
{
    public class AuthenticatedFile<TExtra> : AuthenticatedFile, IAuthenticatedFile<TExtra>
    {
        private readonly IFileInfo<TExtra> _fileInfo;

        public AuthenticatedFile(IFile<TExtra> file, HashAlgorithm hashAlgorithm, string hashAlgorithmName) : base(file, hashAlgorithm, hashAlgorithmName)
        {
            _fileInfo = file ?? throw new ArgumentNullException(nameof(file));
        }

        public TExtra Extra => _fileInfo.Extra;
    }

    public class AuthenticatedFile : IAuthenticatedFile
    {
        private readonly HashAlgorithm _hashAlgorithm;
        private readonly IFile _file;
        private byte[] _hash;

        public string HashAlgorithm { get; }

        public string FileName => _file.FileName;

        public string ContentType => _file.ContentType;

        public AuthenticatedFile(IFile file, HashAlgorithm hashAlgorithm, string hashAlgorithmName)
        {
            if (string.IsNullOrWhiteSpace(hashAlgorithmName))
            {
                throw new ArgumentException("Hash algorithm name must not be null or white space.");
            }

            _file = file ?? throw new ArgumentNullException(nameof(file));
            _hashAlgorithm = hashAlgorithm ?? throw new ArgumentNullException(nameof(hashAlgorithm));
            HashAlgorithm = hashAlgorithmName;
        }

        public async Task CopyToAsync(Stream outputStream)
        {
            var cryptoStream = new CryptoStream(outputStream, _hashAlgorithm, CryptoStreamMode.Write);
            _hashAlgorithm.Initialize();
            await _file.CopyToAsync(cryptoStream);
            await cryptoStream.FlushAsync();
            _hash = _hashAlgorithm.Hash;
        }

        public void Dispose() => _file.Dispose();

        public async Task<byte[]> GetHashAsync()
        {
            if (_hash == null)
            {
                await CopyToAsync(Stream.Null);
            }

            return _hash;
        }

        public Task<Stream> OpenReadStreamAsync() => _file.OpenReadStreamAsync();
    }
}
