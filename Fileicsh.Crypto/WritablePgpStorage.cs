using Fileicsh.Abstraction;
using Fileicsh.Crypto.Extensions;
using Org.BouncyCastle.Bcpg.OpenPgp;
using System;
using System.Collections.Async;
using System.Threading;
using System.Threading.Tasks;

namespace Fileicsh.Crypto
{
    /// <summary>
    /// Represents an <see cref="IStorage"/> that encrypts and/or signs
    /// files that are created through this storage.
    /// </summary>
    public class WritablePgpStorage : IStorage
    {
        /// <summary>
        /// The password that protects the private key.
        /// </summary>
        protected internal string Password { get; }

        /// <summary>
        /// The underlying storage to create the signed/encrypted files against.
        /// </summary>
        protected internal IStorage Storage { get; }

        /// <summary>
        /// The private key to use to sign the files created.
        /// This is null if the storage doesn't support signing files.
        /// </summary>
        public PgpSecretKeyRing PrivateKey { get; }

        /// <summary>
        /// The public key to use to encrypt the files created.
        /// This is null if the storage doesn't support encryption of files.
        /// </summary>
        public PgpPublicKey PublicKey { get; }

        /// <summary>
        /// Returns a flag indicating whether this storage supports encryption of files.
        /// </summary>
        /// <value>True if the storage supports encryption, otherwise false.</value>
        public bool SupportsEncryption => PublicKey != null;

        /// <summary>
        /// Returns a flag indicating whether this storage supports signing of files.
        /// </summary>
        /// <value>True if the storage supports signing, otherwise false.</value>
        public bool SupportsSigning => PrivateKey != null;

        private WritablePgpStorage(IStorage storage)
        {
            Storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        /// <summary>
        /// Creates a writable PGP storage that will encrypt the files that is
        /// created through this storage with the given <paramref name="publicKey"/>.
        /// </summary>
        /// <param name="storage">The underlying storage to create the file against.</param>
        /// <param name="publicKey">The public key used to encrypt the files with PGP.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="storage"/> is null.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="publicKey"/> is null.</exception>
        public WritablePgpStorage(IStorage storage, PgpPublicKey publicKey) : this(storage)
        {
            Storage = storage ?? throw new ArgumentNullException(nameof(storage));
            PublicKey = publicKey ?? throw new ArgumentNullException(nameof(publicKey));
        }

        /// <summary>
        /// Creates a writable PGP storage that will sign the files that
        /// is created through this storage with the given <paramref name="privateKey"/>.
        /// </summary>
        /// <param name="storage">The storage to create the signed files against.</param>
        /// <param name="privateKey">The private key to use to sign the files with.</param>
        /// <param name="password">The password that protects the private key. The password can be null or empty.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="storage"/> is null.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="privateKey"/> is null.</exception>
        public WritablePgpStorage(IStorage storage, PgpSecretKeyRing privateKey, string password) : this(storage)
        {
            PrivateKey = privateKey ?? throw new ArgumentNullException(nameof(privateKey));
            Password = password ?? string.Empty;
        }

        /// <summary>
        /// Creates a writable PGP storage that will both encrypt and sign the files
        /// created through this storage with PGP.
        /// </summary>
        /// <param name="storage">The storage to create the signed and encrypted files against.</param>
        /// <param name="publicKey">The public key used to encrypt the files with PGP.</param>
        /// <param name="privateKey">The private key used to sign the files with PGP.</param>
        /// <param name="password">The password that protects the private key.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="storage"/> is null.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="publicKey"/> is null.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="privateKey"/> is null.</exception>
        public WritablePgpStorage(IStorage storage, PgpPublicKey publicKey, PgpSecretKeyRing privateKey, string password) : this(storage, privateKey, password)
        {
            PublicKey = publicKey ?? throw new ArgumentNullException(nameof(publicKey));
        }

        public virtual IAsyncEnumerable<AlphaNumericString> GetTags() => Storage.GetTags();

        public virtual Task<IFile> GetFileAsync(IFileInfo file, AlphaNumericString tag, CancellationToken cancellationToken = default(CancellationToken)) => Storage
            .GetFileAsync(file, tag, cancellationToken);

        public virtual Task<bool> CreateFileAsync(IFile file, AlphaNumericString tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            IFile pgpFile;
            if (SupportsSigning && SupportsEncryption)
            {
                pgpFile = file.ToSignedAndEncrypted(PublicKey, PrivateKey, Password);
            }
            else if (SupportsSigning)
            {
                pgpFile = file.ToSigned(PrivateKey, Password);
            }
            else
            {
                pgpFile = file.ToEncrypted(PublicKey);
            }

            return Storage.CreateFileAsync(pgpFile, tag, cancellationToken);
        }

        public virtual Task<bool> DeleteFileAsync(IFileInfo file, AlphaNumericString tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Storage.DeleteFileAsync(file, tag, cancellationToken);
        }

        public virtual Task<bool> DeleteTagAsync(AlphaNumericString tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Storage.DeleteTagAsync(tag, cancellationToken);
        }

        public virtual IAsyncEnumerable<IFile> GetFiles(AlphaNumericString tag) => Storage.GetFiles(tag);

        public virtual Task MoveFileAsync(IFileInfo file, AlphaNumericString tag, AlphaNumericString destinationTag, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Storage.MoveFileAsync(file, tag, destinationTag, cancellationToken);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Storage.Dispose();
            }
        }
    }
}
