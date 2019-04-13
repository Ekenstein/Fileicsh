using Fileicsh.Abstraction;
using Org.BouncyCastle.Bcpg.OpenPgp;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fileicsh.Crypto
{
    /// <summary>
    /// Represents a file that can read PGP encrypted and/or PGP signed files.
    /// </summary>
    public class ReadablePgpFile : IFile
    {
        private readonly IFile _file;
        private readonly string _password;

        /// <summary>
        /// The public key of the party that signed this file.
        /// </summary>
        public PgpPublicKey PublicKey { get; }

        /// <summary>
        /// The private key for decrypting this encrypted file.
        /// </summary>
        public PgpSecretKeyRing PrivateKey { get; }

        /// <summary>
        /// Whether the PGP-file can be decrypted.
        /// </summary>
        public bool CanDecrypt => PrivateKey != null;

        /// <summary>
        /// Whether the PGP-file can be verified.
        /// </summary>
        public bool CanVerify => PublicKey != null;

        private ReadablePgpFile(IFile file)
        {
            _file = file ?? throw new ArgumentNullException(nameof(file));
        }

        /// <summary>
        /// Creates a file that can verify the PGP signed <paramref name="file"/> with the given <paramref name="publicKey"/>.
        /// </summary>
        /// <param name="file">The signed file to verify.</param>
        /// <param name="publicKey">The public key for the party that signed the file.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="publicKey"/> is null.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="file"/> is null.</exception>
        public ReadablePgpFile(IFile file, PgpPublicKey publicKey) : this(file)
        {
            PublicKey = publicKey ?? throw new ArgumentNullException(nameof(publicKey));
        }

        /// <summary>
        /// Creates a file that decrypts the PGP encrypted <paramref name="file"/> with the given <paramref name="privateKey"/>.
        /// </summary>
        /// <param name="file">The file that has been encrypted with the public key of the <paramref name="privateKey"/>.</param>
        /// <param name="privateKey">The private key to decrypt the file with.</param>
        /// <param name="password">The password for the private key. Can be null.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="file"/> is null.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="privateKey"/> is null.</exception>
        public ReadablePgpFile(IFile file, PgpSecretKeyRing privateKey, string password) : this(file)
        {
            PrivateKey = privateKey ?? throw new ArgumentNullException(nameof(privateKey));
            _password = password ?? string.Empty;
        }

        /// <summary>
        /// Creates a file that can decrypt and verify the PGP signed and encrypted <paramref name="file"/>.
        /// </summary>
        /// <param name="file">The file that has been both signed and encrypted with PGP.</param>
        /// <param name="publicKey">The public key of the private key that has signed the file.</param>
        /// <param name="privateKey">The private key containing the public key that has encrypted the file.</param>
        /// <param name="password">The password of the private key. Can be null.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="file"/> is null.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="privateKey"/> is null.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="publicKey"/> is null.</exception>
        public ReadablePgpFile(IFile file, PgpPublicKey publicKey, PgpSecretKeyRing privateKey, string password) : this(file, privateKey, password)
        {
            PublicKey = publicKey ?? throw new ArgumentNullException(nameof(publicKey));
        }

        /// <summary>
        /// The name of the underlying file.
        /// </summary>
        public string FileName => _file.FileName;

        /// <summary>
        /// The content type of the underlying file.
        /// </summary>
        public string ContentType => _file.ContentType;

        public async Task CopyToAsync(Stream outputStream, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var inputStream = await _file.OpenReadStreamAsync(cancellationToken))
            using (var decoderStream = PgpUtilities.GetDecoderStream(inputStream))
            {
                var objectFactory = new PgpObjectFactory(decoderStream);
                PgpObject nextObject = objectFactory.NextPgpObject();
                await ProcessNextAsync(outputStream, nextObject, objectFactory, null);
            }
        }

        private async Task ProcessNextAsync(Stream outputStream, PgpObject pgpObject, PgpObjectFactory factory, Action<byte[], int, int> update)
        {
            switch (pgpObject)
            {
                case PgpEncryptedDataList encryptedDataList:
                    var encryptedData = encryptedDataList
                        .GetEncryptedDataObjects()
                        .OfType<PgpPublicKeyEncryptedData>()
                        .FirstOrDefault();

                    var keyId = encryptedData.KeyId;
                    var privateKey = PrivateKey
                        .GetSecretKey(keyId)
                        .ExtractPrivateKey(_password.ToCharArray());

                    using (var decryptedDataStream = encryptedData.GetDataStream(privateKey))
                    {
                        var decryptedFactory = new PgpObjectFactory(decryptedDataStream);
                        await ProcessNextAsync(outputStream, decryptedFactory.NextPgpObject(), decryptedFactory, update);
                        VerifyEncryptedData(encryptedData);
                    }
                    break;
                case PgpCompressedData compressedData:
                    using (var data = compressedData.GetDataStream())
                    {
                        var nextFactory = new PgpObjectFactory(data);
                        await ProcessNextAsync(outputStream, nextFactory.NextPgpObject(), nextFactory, update);
                    }
                    break;
                case PgpOnePassSignatureList onePassSigList:
                    var onePassSignature = onePassSigList[0];
                    InitSignatureVerification(onePassSignature);
                    await ProcessNextAsync(outputStream, factory.NextPgpObject(), factory, (buf, offset, length) => onePassSignature.Update(buf, offset, length));
                    break;
                case PgpSignatureList signatureList:
                    var signature = signatureList[0];
                    InitSignatureVerification(signature);
                    await ProcessNextAsync(outputStream, factory.NextPgpObject(), factory, (buf, offset, length) => signature.Update(buf, offset, length));
                    VerifyMessageSignature(signature);
                    break;
                case PgpLiteralData literalData:
                    using (var inputStream = literalData.GetInputStream())
                    {
                        var length = 0;
                        var buf = new byte[1 << 16];
                        while ((length = await inputStream.ReadAsync(buf, 0, buf.Length)) > 0)
                        {
                            update?.Invoke(buf, 0, length);
                            await outputStream.WriteAsync(buf, 0, length);
                        }

                        await inputStream.FlushAsync();
                        inputStream.Close();
                    }
                    break;
                case PgpMarker _:
                    await ProcessNextAsync(outputStream, factory.NextPgpObject(), factory, update);
                    break;
                default:
                    throw new PgpException("Couldn't recognize the pgp object.");
            }
        }

        /// <summary>
        /// Disposes the underlying file.
        /// </summary>
        public void Dispose() => _file.Dispose();

        public async Task<Stream> OpenReadStreamAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var ms = new MemoryStream();
            await CopyToAsync(ms, cancellationToken);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        private PgpPublicKey GetPublicKey(long keyId) => new PgpPublicKeyRing(PublicKey.GetEncoded()).GetPublicKey(keyId);

        private void InitSignatureVerification(PgpOnePassSignature sig) => sig.InitVerify(GetPublicKey(sig.KeyId));
        private void InitSignatureVerification(PgpSignature sig) => sig.InitVerify(GetPublicKey(sig.KeyId));

        private void VerifyEncryptedData(PgpPublicKeyEncryptedData encryptedData)
        {
            if (!encryptedData.IsIntegrityProtected()) return;
            if (!encryptedData.Verify())
            {
                throw new PgpException("Message integrity check failed.");
            }
        }

        private void VerifyMessageSignature(PgpSignature sig)
        {
            if (!sig.Verify())
            {
                throw new PgpException("The signature of the file couldn't be verified.");
            }
        }

        private void VerifyMessageSignature(PgpOnePassSignature onePassSignature, PgpSignature sig)
        {
            if (!onePassSignature.Verify(sig))
            {
                throw new PgpException("The signature of the file couldn't be verified.");
            }
        }
    }
}
