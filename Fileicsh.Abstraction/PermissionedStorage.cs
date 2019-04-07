using System;
using System.Collections.Async;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Fileicsh.Abstraction
{
    /// <summary>
    /// Represents a permissioned <see cref="IStorage"/>
    /// where a storage is limited in its use depending
    /// on which permissions the storage has been given.
    /// If an operation is accessed without permissions, <see cref="SecurityException"/>
    /// will be thrown.
    /// </summary>
    public class PermissionedStorage : IStorage
    {
        /// <summary>
        /// Represents a permission for a storage.
        /// </summary>
        [Flags]
        public enum Permission
        {
            /// <summary>
            /// A permission representing operations like retrieving
            /// files and tags.
            /// </summary>
            Read = 1 << 0,

            /// <summary>
            /// A permission representing operations like creating files.
            /// </summary>
            Write = 1 << 1,

            /// <summary>
            /// A permission representing operations like deleting files
            /// and tags.
            /// </summary>
            Delete = 1 << 2,

            /// <summary>
            /// A permission representing operations like moving files.
            /// </summary>
            Move = 1 << 3
        }

        /// <summary>
        /// Permission flag representing all permissions.
        /// </summary>
        public const Permission All = Permission.Delete | Permission.Move | Permission.Read | Permission.Write;

        private readonly IStorage _storage;

        /// <summary>
        /// The current permissions the storage has.
        /// </summary>
        public Permission Permissions { get; }

        /// <summary>
        /// Returns a flag indicating whether an operation
        /// has permissions enough to be performed.
        /// </summary>
        /// <param name="permission">The permission to check.</param>
        /// <returns>
        /// True if the operation has enough permissions to be performed, otherwise false.
        /// </returns>
        public virtual bool HasPermission(Permission permission) => Permissions.HasFlag(permission);

        private void ThrowIfNoPermission(Permission permission)
        {
            if (!HasPermission(permission))
            {
                throw new SecurityException($"The operation was not performed due to insufficient permissions. The operation was {permission}.");
            }
        }

        /// <summary>
        /// Returns all the tags of the underlying storage, iff the storage has the permission <see cref="Permission.Read"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> containing
        /// the tags of the underlying storage, iff the storage has the permission <see cref="Permission.Read"/>.
        /// </returns>
        /// <exception cref="SecurityException">If the storage doesn't have the permission <see cref="Permission.Read"/>.</exception>
        public IAsyncEnumerable<AlphaNumericString> GetTags()
        {
            ThrowIfNoPermission(Permission.Read);
            return _storage.GetTags();
        }

        /// <summary>
        /// Returns the file corresponding to the given <paramref name="file"/> that is associated with the given <paramref name="tag"/>
        /// from the underlying storage, iff the storage has the permission <see cref="Permission.Read"/>.
        /// </summary>
        /// <param name="file">The file to retrieve from the underlying storage if the storage has sufficient permissions.</param>
        /// <param name="tag">The tag the file is associated with.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation, containing
        /// the file corresponding to the given <paramref name="file"/>.
        /// </returns>
        /// <exception cref="SecurityException">If the storage doesn't have the permission <see cref="Permission.Read"/>.</exception>
        public Task<IFile> GetFileAsync(IFileInfo file, AlphaNumericString tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfNoPermission(Permission.Read);
            return _storage.GetFileAsync(file, tag, cancellationToken);
        }

        /// <summary>
        /// Creates the given <paramref name="file"/> at the underlying storage and associates the file
        /// with the given <paramref name="tag"/>, iff the storage has the permission <see cref="Permission.Write"/>.
        /// </summary>
        /// <param name="file">The file to create at the underlying storage if the storage has sufficient permissions.</param>
        /// <param name="tag">The tag to associate the file with.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation, containing
        /// a flag indicating whether the file was successfully created or not at the underlying storage.
        /// </returns>
        /// <exception cref="SecurityException">If the storage doesn't have the permission <see cref="Permission.Write"/>.</exception>
        public Task<bool> CreateFileAsync(IFile file, AlphaNumericString tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfNoPermission(Permission.Write);
            return _storage.CreateFileAsync(file, tag, cancellationToken);
        }

        /// <summary>
        /// Deletes the given <paramref name="file"/>, associated with the given <paramref name="tag"/>, at the underlying storage,
        /// iff the storage has the permission <see cref="Permission.Delete"/>.
        /// </summary>
        /// <param name="file">The file to delete if the storage has sufficient permissions.</param>
        /// <param name="tag">The tag the file is associated with.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation, containing
        ///  a flag indicating whether the file was successfully deleted or not.
        /// </returns>
        /// <exception cref="SecurityException">If the storage doesn't have the permission <see cref="Permission.Delete"/>.</exception>
        public Task<bool> DeleteFileAsync(IFileInfo file, AlphaNumericString tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfNoPermission(Permission.Delete);
            return _storage.DeleteFileAsync(file, tag, cancellationToken);
        }

        /// <summary>
        /// Deletes the given <paramref name="tag"/> from the underlying storage, iff the storage
        /// has the permission <see cref="Permission.Delete"/>.
        /// </summary>
        /// <param name="tag">The tag to delete.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation, containing
        /// a flag indicating whether the tag was successfully deleted or not.
        /// </returns>
        /// <exception cref="SecurityException">If the storage doesn't have the permission <see cref="Permission.Delete"/>.</exception>
        public Task<bool> DeleteTagAsync(AlphaNumericString tag, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfNoPermission(Permission.Delete);
            return _storage.DeleteTagAsync(tag, cancellationToken);
        }

        /// <summary>
        /// Returns all the files associated with the given <paramref name="tag"/> from the underlying storage,
        /// iff the storage has the permission <see cref="Permission.Read"/>.
        /// </summary>
        /// <param name="tag">The tag that the files should be associated with.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> containing all the files associated with the given <paramref name="tag"/>.
        /// </returns>
        /// <exception cref="SecurityException">If the storage doesn't have the permission <see cref="Permission.Read"/>.</exception>
        public IAsyncEnumerable<IFile> GetFiles(AlphaNumericString tag)
        {
            ThrowIfNoPermission(Permission.Read);
            return _storage.GetFiles(tag);
        }

        /// <summary>
        /// Re-associates the given <paramref name="file"/>, currently associated with the given <paramref name="tag"/>,
        /// with the <paramref name="destinationTag"/> at the underlying storage, iff the storage has the permission
        /// <see cref="Permission.Move"/>.
        /// </summary>
        /// <param name="file">The file to re-associate with a new tag.</param>
        /// <param name="tag">The tag the file is currently associated with.</param>
        /// <param name="destinationTag">The new tag the file should be associated with.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public Task MoveFileAsync(IFileInfo file, AlphaNumericString tag, AlphaNumericString destinationTag, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfNoPermission(Permission.Move);
            return _storage.MoveFileAsync(file, tag, destinationTag, cancellationToken);
        }

        /// <summary>
        /// Disposes the underlying storage.
        /// </summary>
        public void Dispose() => _storage.Dispose();

        /// <summary>
        /// Creates a permissioned storage for the given underlying <paramref name="storage"/>
        /// where the <paramref name="storage"/> will have the given <paramref name="permissions"/>.
        /// </summary>
        /// <param name="storage">The underlying storage to limit with permissions.</param>
        /// <param name="permissions">The permissions the storage will have.</param>
        public PermissionedStorage(IStorage storage, Permission permissions)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            Permissions = permissions;
        }
    }
}
