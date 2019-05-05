using System;
using System.Collections.Async;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fileicsh.Abstraction.Helpers;

namespace Fileicsh.Abstraction
{
    /// <summary>
    /// A storage that consists of two storages, one main storage and
    /// one shadow storage. All operations will be performed on both
    /// storages.
    /// </summary>
    public class ShadowedStorage : IStorage
    {
        /// <summary>
        /// All operations should be shadowed.
        /// </summary>
        public const ShadowedOperation AllOperations = ShadowedOperation.CreateFile |
                                                       ShadowedOperation.DeleteFile |
                                                       ShadowedOperation.DeleteTag |
                                                       ShadowedOperation.GetFile |
                                                       ShadowedOperation.GetFiles |
                                                       ShadowedOperation.GetTags |
                                                       ShadowedOperation.MoveFile;

        /// <summary>
        /// All read operations should be shadowed.
        /// </summary>
        public const ShadowedOperation ReadOnlyOperations = ShadowedOperation.GetFile |
                                                            ShadowedOperation.GetFiles |
                                                            ShadowedOperation.GetTags;

        /// <summary>
        /// All delete operations should be shadowed.
        /// </summary>
        public const ShadowedOperation DeleteOnlyOperations = ShadowedOperation.DeleteFile |
                                                              ShadowedOperation.DeleteTag;

        private bool _disposed;
        private readonly IStorage _main;
        private readonly IStorage _shadow;
        private readonly Action<ShadowedOperation, Exception> _errorHandler;

        /// <summary>
        /// The operations that will be shadowed.
        /// </summary>
        public ShadowedOperation Operations { get; }

        /// <summary>
        /// Operations that can be shadowed.
        /// </summary>
        [Flags]
        public enum ShadowedOperation
        {
            /// <summary>
            /// The operation <see cref="IStorage.CreateFileAsync"/>.
            /// </summary>
            CreateFile = 1 << 0,

            /// <summary>
            /// The operation <see cref="IStorage.GetTags"/>.
            /// </summary>
            GetTags = 1 << 1,

            /// <summary>
            /// The operation <see cref="IStorage.GetFiles"/>.
            /// </summary>
            GetFiles = 1 << 2,

            /// <summary>
            /// The operation <see cref="IStorage.GetFileAsync"/>.
            /// </summary>
            GetFile = 1 << 3,

            /// <summary>
            /// The delete operation <see cref="IStorage.DeleteFileAsync"/>.
            /// </summary>
            DeleteFile = 1 << 4,

            /// <summary>
            /// The delete operation <see cref="IStorage.DeleteTagAsync"/>.
            /// </summary>
            DeleteTag = 1 << 5,

            /// <summary>
            /// The move operation <see cref="IStorage.MoveFileAsync"/>.
            /// </summary>
            MoveFile = 1 << 6
        }

        /// <summary>
        /// Creates a shadowed storage for the given <paramref name="main"/> which will
        /// ignore all exceptions produced by the <paramref name="shadow"/>.
        /// </summary>
        /// <param name="main">The main storage to create a shadow for.</param>
        /// <param name="shadow">The storage that will shadow the main storage.</param>
        /// <param name="operations">The operations that should be shadowed.</param>
        /// <returns>
        /// A shadowed storage for the given <paramref name="main"/> which will ignore all exceptions
        /// thrown by the <paramref name="shadow"/>.
        /// </returns>
        public static IStorage LenientErrorHandling(IStorage main, IStorage shadow, ShadowedOperation operations) => new ShadowedStorage(main, shadow, (_, e) => {}, operations);

        /// <summary>
        /// Creates a shadowed storage for the given <paramref name="main"/> which will
        /// throw all the exceptions produced by the <paramref name="shadow"/>.
        /// </summary>
        /// <param name="main">The main storage to create a shadow for.</param>
        /// <param name="shadow">The storage that will shadow the main storage.</param>
        /// <param name="operations">The operations that should be shadowed.</param>
        /// <returns>
        /// A shadowed storage for the given <paramref name="main"/> storage which will throw all
        /// exceptions thrown by the <paramref name="shadow"/>.
        /// </returns>
        public static IStorage StrictErrorHandling(IStorage main, IStorage shadow, ShadowedOperation operations) => new ShadowedStorage(main, shadow, (_, e) => throw e, operations);

        /// <summary>
        /// Creates a shadowed storage for the given <paramref name="main"/> storage
        /// where the given <paramref name="shadow"/> storage will copy all the operations made.
        /// </summary>
        /// <param name="main">The main storage.</param>
        /// <param name="shadow">The shadow storage that will perform the same operations as the main.</param>
        /// <param name="errorHandler">An error handler on how to handle exceptions thrown by the shadow storage.</param>
        /// <param name="operations">The operations that should be shadowed. Default is all operations.</param>
        public ShadowedStorage(IStorage main, IStorage shadow, Action<ShadowedOperation, Exception> errorHandler, ShadowedOperation operations = AllOperations)
        {
            _main = main ?? throw new ArgumentNullException(nameof(main));
            _shadow = shadow ?? throw new ArgumentNullException(nameof(shadow));
            _errorHandler = errorHandler;
            Operations = operations;
        }

        /// <summary>
        /// Returns a flag indicating whether the given <paramref name="operation"/>
        /// should be shadowed or not.
        /// </summary>
        /// <param name="operation">The operation to check whether it should be shadowed or not.</param>
        /// <returns>True if the <paramref name="operation"/> should be shadowed, otherwise false.</returns>
        protected virtual bool ShouldShadow(ShadowedOperation operation) => Operations.HasFlag(operation);

        /// <summary>
        /// Called whenever an exception was thrown from the shadow storage.
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="ex"></param>
        protected virtual void OnShadowError(ShadowedOperation operation, Exception ex) =>
            _errorHandler?.Invoke(operation, ex);

        private Task ShadowAsync(Func<IStorage, Task> shadowOperation, ShadowedOperation operation) =>
            ShadowAsync(
                shadowOperation: s => shadowOperation(s).ContinueWith(_ => true, TaskContinuationOptions.OnlyOnRanToCompletion),
                operation: operation, 
                defaultValue: () => false);

        private async Task<T> ShadowAsync<T>(Func<IStorage, Task<T>> shadowOperation, ShadowedOperation operation, Func<T> defaultValue)
        {
            if (ShouldShadow(operation))
            {
                try
                {
                    return await shadowOperation(_shadow);
                }
                catch (Exception ex)
                {
                    _errorHandler?.Invoke(operation, ex);
                }
            }

            return defaultValue();
        }

        /// <summary>
        /// Returns the tags from the main storage unioned with
        /// the tags from the shadow storage iff <see cref="Operations"/> is flagged with <see cref="ShadowedOperation.GetTags"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> containing unique tags from both the main storage and the shadowed storage,
        /// iff <see cref="ShadowedOperation.GetTags"/> is flagged.
        /// </returns>
        public virtual IAsyncEnumerable<AlphaNumericString> GetTags()
        {
            ThrowIfDisposed();

            var mainTags = _main.GetTags();
            if (ShouldShadow(ShadowedOperation.GetTags))
            {
                return mainTags.Concat(_shadow.GetTags()).Distinct();
            }

            return mainTags;
        }

        /// <summary>
        /// Returns the file corresponding to the given <paramref name="file"/> that is associated with the given <paramref name="tag"/>
        /// from the main storage. If the main storage doesn't have such a file, the shadow storage will be queried with the
        /// same operation, iff <see cref="Operations"/> is is flagged with <see cref="ShadowedOperation.GetFile"/>
        /// </summary>
        /// <param name="file">The file to find.</param>
        /// <param name="tag">The tag the file is associated with.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The file corresponding to the given <paramref name="file"/> from the main storage. If the file doesn't
        /// exist in the main storage, the file will be queried at the shadow storage, iff <see cref="Operations"/>
        /// is flagged with <see cref="ShadowedOperation.GetFile"/>.
        /// </returns>
        public async Task<IFile> GetFileAsync(IFileInfo file, AlphaNumericString tag, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            var mainFile = await _main.GetFileAsync(file, tag, cancellationToken);
            if (mainFile != null)
            {
                return mainFile;
            }

            return await ShadowAsync(s => s.GetFileAsync(file, tag, cancellationToken), ShadowedOperation.GetFile, () => null);
        }

        /// <summary>
        /// Creates the given <paramref name="file"/> that should be associated with the given <paramref name="tag"/>
        /// at the main storage. If <see cref="Operations"/> is flagged with <see cref="ShadowedOperation.CreateFile"/> the
        /// file will also be created at the shadow storage.
        /// If the file wasn't successfully created at the main storage, the file won't be created in the shadow storage
        /// either.
        /// </summary>
        /// <param name="file">The file to create.</param>
        /// <param name="tag">The tag the file should be associated with.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// A flag indicating whether the file was successfully created at the main storage.
        /// </returns>
        public async Task<bool> CreateFileAsync(IFile file, AlphaNumericString tag, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            var result = await _main.CreateFileAsync(file, tag, cancellationToken);
            if (!result)
            {
                return result;
            }

            await ShadowAsync(s => s.CreateFileAsync(file, tag, cancellationToken), ShadowedOperation.CreateFile);
            return true;
        }

        /// <summary>
        /// Deletes the given <paramref name="file"/> associated with the given <paramref name="tag"/>
        /// at the main storage. If <see cref="Operations"/> is flagged with <see cref="ShadowedOperation.DeleteFile"/>,
        /// the file will also be deleted at the shadow storage.
        /// If the file wasn't successfully deleted at the main storage, the file won't be deleted at the shadow storage
        /// either.
        /// </summary>
        /// <param name="file">The file to delete.</param>
        /// <param name="tag">The tag the file is associated with.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// A flag indicating whether the file was successfully deleted or not at the main storage.
        /// </returns>
        public async Task<bool> DeleteFileAsync(IFileInfo file, AlphaNumericString tag, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            var result = await _main.DeleteFileAsync(file, tag, cancellationToken);
            if (!result)
            {
                return result;
            }

            await ShadowAsync(s => s.DeleteFileAsync(file, tag, cancellationToken), ShadowedOperation.DeleteFile);
            return result;
        }

        /// <summary>
        /// Deletes the given <paramref name="tag"/> at the main storage. If <see cref="Operations"/> is
        /// flagged with <see cref="ShadowedOperation.DeleteFile"/>, the tag will be deleted
        /// at the shadow storage aswell.
        /// If the tag wasn't deleted successfully at the main storage, the tag won't be deleted
        /// at the shadow storage either.
        /// </summary>
        /// <param name="tag">The tag to delete.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// A flag indicating whether the tag was successfully deleted at the main storage or not.
        /// </returns>
        public async Task<bool> DeleteTagAsync(AlphaNumericString tag, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            var result = await _main.DeleteTagAsync(tag, cancellationToken);
            if (!result)
            {
                return result;
            }

            await ShadowAsync(s => s.DeleteTagAsync(tag, cancellationToken), ShadowedOperation.DeleteFile);
            return true;
        }

        /// <summary>
        /// Returns all the files associated with the given <paramref name="tag"/> from the main storage.
        /// If <see cref="Operations"/> is flagged with <see cref="ShadowedOperation.GetFiles"/>, the main storage
        /// tags will be unioned with the shadow storage's files.
        /// </summary>
        /// <param name="tag">The tag the files should be associated with.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> containing zero or more files from the main storage.
        /// If <see cref="Operations"/> is flagged with <see cref="ShadowedOperation.GetFiles"/>, the collection
        /// will also contain files from the shadow storage.
        /// </returns>
        public IAsyncEnumerable<IFile> GetFiles(AlphaNumericString tag)
        {
            ThrowIfDisposed();

            var mainFiles = _main.GetFiles(tag);
            IAsyncEnumerable<IFile> shadowFiles = AsyncEnumerable.Empty<IFile>();

            if (ShouldShadow(ShadowedOperation.GetFiles))
            {
                try
                {
                    shadowFiles = _shadow.GetFiles(tag);
                }
                catch (Exception ex)
                {
                    OnShadowError(ShadowedOperation.GetFiles, ex);
                }
            }

            return mainFiles.UnionAll(shadowFiles).Distinct(FileHelpers.FileComparer);
        }

        /// <summary>
        /// Moves the <paramref name="file"/> associated with the given <paramref name="tag"/> to the <paramref name="destinationTag"/>
        /// at the main storage. If <see cref="Operations"/> is flagged with <see cref="ShadowedOperation.MoveFile"/>, the file
        /// will also be moved at the shadow storage.
        /// </summary>
        /// <param name="file">The file to associate with a new tag.</param>
        /// <param name="tag">The tag the file is currently associated with.</param>
        /// <param name="destinationTag">The tag the file should be associated with.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task MoveFileAsync(IFileInfo file, AlphaNumericString tag, AlphaNumericString destinationTag, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            if (tag == destinationTag)
            {
                return;
            }

            await _main.MoveFileAsync(file, tag, destinationTag, cancellationToken);
            await ShadowAsync(s => s.MoveFileAsync(file, tag, destinationTag, cancellationToken),
                ShadowedOperation.MoveFile);
        }

        /// <summary>
        /// Throws <see cref="ObjectDisposedException"/> if the storage has been disposed.
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        /// <summary>
        /// Disposes the shadowed storage.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes both the main storage and the shadow storage.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_disposed)
                {
                    _main.Dispose();
                    _shadow.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
