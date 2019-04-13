using Fileicsh.Abstraction;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace Fileicsh.Crypto.Extensions
{
    /// <summary>
    /// Provides a static set of extensions for files associated with crypto.
    /// </summary>
    public static class CryptoExtensions
    {
        /// <summary>
        /// Creates an ASCII-armored PGP encrypted file out of the given <paramref name="file"/>
        /// that will be encrypted with the given <paramref name="publicKey"/>.
        /// </summary>
        /// <param name="file">The file to create an ASCII-armored PGP encrypted file of.</param>
        /// <param name="publicKey">The public key to encrypt the file with.</param>
        /// <returns>
        /// An <see cref="IFile"/> representing the ASCII-armored PGP encrypted file.
        /// </returns>
        public static IFile ToEncrypted(this IFile file, PgpPublicKey publicKey)
        {
            return new BinaryPgpFile(file, publicKey);
        }

        /// <summary>
        /// Creates an ASCII-armored PGP signed file out of the given <paramref name="file"/>
        /// that will be signed with the given <paramref name="privateKey"/>.
        /// </summary>
        /// <param name="file">The file to sign with PGP.</param>
        /// <param name="privateKey">The private key to sign the file with.</param>
        /// <param name="password">The password that protects the private key.</param>
        /// <returns>An <see cref="IFile"/> representing the ASCII-armored PGP signed file.</returns>
        public static IFile ToSigned(this IFile file, PgpSecretKeyRing privateKey, string password)
        {
            return new BinaryPgpFile(file, privateKey, password);
        }

        /// <summary>
        /// Creates an ASCII-armored PGP signed and encrypted file of the given <paramref name="file"/>
        /// that will be signed with the <paramref name="privateKey"/> and encrypted with the
        /// <paramref name="publicKey"/>.
        /// </summary>
        /// <param name="file">The file to sign and encrypt with PGP.</param>
        /// <param name="publicKey">The public key to encrypt the file with.</param>
        /// <param name="privateKey">The private key to sign the file with.</param>
        /// <param name="password">The password that protects the private key.</param>
        /// <returns>An <see cref="IFile"/> representing the ASCII-armored PGP signed and encrypted file.</returns>
        public static IFile ToSignedAndEncrypted(this IFile file, PgpPublicKey publicKey, PgpSecretKeyRing privateKey, string password)
        {
            return new BinaryPgpFile(file, publicKey, privateKey, password);
        }

        /// <summary>
        /// Creates a file that can decrypt the PGP <paramref name="encrypted"/> file. The <paramref name="privateKey"/>
        /// is assumed to have no password.
        /// </summary>
        /// <param name="encrypted">The PGP encrypted file to decrypt.</param>
        /// <param name="privateKey">The private key containing the public key that has encrypted the file.</param>
        /// <returns>
        /// An <see cref="IFile"/> that can decrypt the PGP <paramref name="encrypted"/> file.
        /// </returns>
        public static IFile ToDecrypted(this IFile encrypted, PgpSecretKeyRing privateKey) => ToDecrypted(encrypted, privateKey, string.Empty);

        /// <summary>
        /// Creates a file that can decrypt the PGP <paramref name="encrypted"/> file. The <paramref name="privateKey"/>
        /// is protected by the given <paramref name="password"/>.
        /// </summary>
        /// <param name="encrypted">The PGP encrypted file to decrypt.</param>
        /// <param name="privateKey">The private key containing the public key that has encrypted the file.</param>
        /// <param name="password">The password that protects the private key.</param>
        /// <returns>
        /// An <see cref="IFile"/> that can decrypt the PGP <paramref name="encrypted"/> file.
        /// </returns>
        public static IFile ToDecrypted(this IFile encrypted, PgpSecretKeyRing privateKey, string password) => new ReadablePgpFile(encrypted, privateKey, password);

        /// <summary>
        /// Creates a file that can verify the PGP <paramref name="signed"/> file.
        /// </summary>
        /// <param name="signed">The PGP signed file to verify.</param>
        /// <param name="publicKey">The public key of the private key that has signed the file.</param>
        /// <returns>
        /// An <see cref="IFile"/> that can verify the PGP <paramref name="signed"/> file.
        /// </returns>
        public static IFile ToVerified(this IFile signed, PgpPublicKey publicKey) => new ReadablePgpFile(signed, publicKey);

        /// <summary>
        /// Creates a file that can both verify and decrypt the encrypted and signed PGP <paramref name="file"/>.
        /// </summary>
        /// <param name="file">The file that has been both signed and encrypted with PGP.</param>
        /// <param name="publicKey">The public key of the private key that has signed the file.</param>
        /// <param name="privateKey">The private key containing the public key that has encrypted the file.</param>
        /// <param name="password">The password that protects the private key.</param>
        /// <returns>
        /// An <see cref="IFile"/> that can decrypt and verify the PGP encrypted and signed <paramref name="file"/>.
        /// </returns>
        public static IFile ToVerifiedAndDecrypted(this IFile file, PgpPublicKey publicKey, PgpSecretKeyRing privateKey, string password)
        {
            return new ReadablePgpFile(file, publicKey, privateKey, password);
        }
    }
}
