using Fileicsh.Abstraction;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fileicsh.Crypto
{
    /// <summary>
    /// Represents a binary PGP file that
    /// </summary>
    public class BinaryPgpFile : IFile
    {
        private readonly IFile _file;
        private readonly string _password;

        /// <summary>
        /// The public key to use to encrypt the file with.
        /// </summary>
        /// <value>The public key to use for encryption. Is null if the document should only be signed.</value>
        public PgpPublicKey PublicKey { get; }

        /// <summary>
        /// The private key to use to sign the file with.
        /// </summary>
        /// <value>The private key to use for signature. Is null if the document should only be encrypted.</value>
        public PgpSecretKeyRing PrivateKey { get; }

        /// <summary>
        /// Whether the encryption/signature should be ASCII-armored. Default is true.
        /// </summary>
        public bool Armored { get; set; } = true;

        /// <summary>
        /// The hash algorithm to use for the signature. Default is SHA256.
        /// </summary>
        public HashAlgorithmTag HashAlgorithm { get; set; } = HashAlgorithmTag.Sha256;

        /// <summary>
        /// The compression algorithm to use. Default is Zip.
        /// </summary>
        public CompressionAlgorithmTag CompressionAlgorithm { get; set; } = CompressionAlgorithmTag.Zip;

        /// <summary>
        /// The symmetric key algorithm to use for encryption. Default is Cast5.
        /// </summary>
        public SymmetricKeyAlgorithmTag SymmetricKeyAlgorithm { get; set; } = SymmetricKeyAlgorithmTag.Cast5;

        /// <summary>
        /// Whether the encryption should be done with integrity packet. Default is true.
        /// </summary>
        public bool WithIntegrityPacket { get; set; } = true;

        /// <summary>
        /// The name of the underlying file that will be encrypted/signed.
        /// </summary>
        public string FileName => Path.ChangeExtension(_file.FileName, Armored ? "asc" : "gpg");

        /// <summary>
        /// The content type of the underlying file.
        /// </summary>
        public string ContentType => PublicKey != null
            ? "application/pgp-encrypted"
            : "application/pgp-signature";

        private BinaryPgpFile(IFile file)
        {
            _file = file ?? throw new ArgumentNullException(nameof(file));
        }

        /// <summary>
        /// Creates a file that contains the data of the given <paramref name="file"/> encrypted with PGP.
        /// </summary>
        /// <param name="file">The file to encrypt with PGP.</param>
        /// <param name="publicKey">The public key to use for encryption.</param>
        public BinaryPgpFile(IFile file, PgpPublicKey publicKey) : this(file)
        {
            PublicKey = publicKey ?? throw new ArgumentNullException(nameof(publicKey));
        }

        /// <summary>
        /// Creates a file that contains the data of the given <paramref name="file"/> signed with PGP.
        /// </summary>
        /// <param name="file">The file to sign.</param>
        /// <param name="privateKey">The private key to use to sign the file with.</param>
        /// <param name="password">The password that protects the private key.</param>
        public BinaryPgpFile(IFile file, PgpSecretKeyRing privateKey, string password) : this(file)
        {
            PrivateKey = privateKey ?? throw new ArgumentNullException(nameof(privateKey));
            _password = password ?? string.Empty;
        }

        /// <summary>
        /// Creates a file that is both signed with the <paramref name="privateKey"/> and encrypted with
        /// the <paramref name="publicKey"/>.
        /// </summary>
        /// <param name="file">The file to encrypt and sign.</param>
        /// <param name="publicKey">The public key to encrypt the file with.</param>
        /// <param name="privateKey">The private key to sign the file with.</param>
        public BinaryPgpFile(IFile file, PgpPublicKey publicKey, PgpSecretKeyRing privateKey, string password) : this(file, privateKey, password) 
        {
            PublicKey = publicKey ?? throw new ArgumentNullException(nameof(publicKey));
        }

        public async Task CopyToAsync(Stream outputStream, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (var armoredStream = ArmoredOutputStream(outputStream))
            using (var encryptedStream = EncryptionStream(armoredStream))
            using (var compressedStream = CompressionStream(encryptedStream))
            {
                var signatureGenerator = InitSignature(compressedStream);
                using (var inputStream = await _file.OpenReadStreamAsync(cancellationToken))
                using (var literalData = new PgpLiteralDataGenerator().Open(compressedStream, PgpLiteralData.Binary, FileName, DateTime.UtcNow, new byte[1 << 16]))
                {
                    var length = 0;
                    var buf = new byte[1 << 16];
                    while ((length = await inputStream.ReadAsync(buf, 0, buf.Length)) > 0) 
                    {
                        await literalData.WriteAsync(buf, 0, length);
                        signatureGenerator?.Update(buf, 0, length);
                    }

                    signatureGenerator?.Generate()?.Encode(compressedStream);
                }
            }
        }

        private PgpSignatureGenerator InitSignature(Stream outputStream)
        {
            if (PrivateKey == null)
            {
                return null;
            }

            var signatureGenerator = new PgpSignatureGenerator(PrivateKey.GetPublicKey().Algorithm, HashAlgorithm);
            signatureGenerator.InitSign(PgpSignature.BinaryDocument, PrivateKey.GetSecretKey().ExtractPrivateKey(_password.ToCharArray()));

            var userId = PrivateKey.GetPublicKey().GetUserIds().OfType<string>().FirstOrDefault();
            var subpacketGenerator = new PgpSignatureSubpacketGenerator();
            subpacketGenerator.SetSignerUserId(false, userId);
            signatureGenerator.SetHashedSubpackets(subpacketGenerator.Generate());
            signatureGenerator.GenerateOnePassVersion(false).Encode(outputStream);
            return signatureGenerator;
        }

        private Stream EncryptionStream(Stream outputStream)
        {
            if (PublicKey == null)
            {
                return outputStream;
            }
            
            var encryptedDataGenerator = new PgpEncryptedDataGenerator(SymmetricKeyAlgorithm, WithIntegrityPacket);
            encryptedDataGenerator.AddMethod(PublicKey);
            return encryptedDataGenerator.Open(outputStream, new byte[1 << 16]);
        }

        private Stream CompressionStream(Stream outputStream)
        {
            var compressedDataGenerator = new PgpCompressedDataGenerator(CompressionAlgorithm);
            return compressedDataGenerator.Open(outputStream);
        }

        private Stream ArmoredOutputStream(Stream outputStream) => Armored
            ? new ArmoredOutputStream(outputStream)
            : outputStream;

        public async Task<Stream> OpenReadStreamAsync(CancellationToken cancellationToken = default)
        {
            var tempFile = Path.GetTempFileName();

            using (var fs = File.Create(tempFile))
            {
                await CopyToAsync(fs);
                await fs.FlushAsync();
            }

            return new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.None, 1 << 16, FileOptions.DeleteOnClose);
        }

        public void Dispose() => _file.Dispose();
    }
}
