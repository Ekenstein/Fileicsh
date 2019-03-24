using System.Threading.Tasks;
using Fileicsh.Abstraction.Helpers;

namespace Fileicsh.Abstraction.Extensions
{
    /// <summary>
    /// Provides a static set of functions that extends <see cref="IStorage"/>.
    /// </summary>
    public static class StorageExtensions
    {
        /// <summary>
        /// Creates a prefixed storage of the given <paramref name="storage"/> which
        /// will prefix all the tags.
        /// </summary>
        /// <param name="storage">The storage to prefix.</param>
        /// <param name="prefix">The prefix.</param>
        /// <returns>A prefixed storage which will add the given <paramref name="prefix"/> to the tags.</returns>
        public static IStorage PrefixWith(this IStorage storage, string prefix)
        {
            return new PrefixedStorage(storage, prefix);
        }

        /// <summary>
        /// Returns a new <see cref="IStorage"/> where the <paramref name="main"/> storage
        /// will be shadowed by the <paramref name="shadow"/> storage.
        /// This means that all the operations performed on the <paramref name="main"/> storage
        /// also will be performed on the <paramref name="shadow"/> storage. Useful for backing up
        /// data.
        /// </summary>
        /// <param name="main">The main storage to shadow.</param>
        /// <param name="shadow">The storage that will shadow the main storage.</param>
        /// <returns>
        /// An <see cref="IStorage"/> which makes sure that all the operations performed on the <paramref name="main"/> storage
        /// also is performed on the <paramref name="shadow"/> storage.
        /// </returns>
        public static IStorage ShadowedBy(this IStorage main, IStorage shadow) => new ShadowedStorage(main, shadow, (operation, exception) => {});

        /// <summary>
        /// Moves the given <paramref name="fileInfo"/> located at the given <paramref name="storage"/> 
        /// to the <paramref name="destination"/> storage
        /// where it will be associated with the given <paramref name="destinationTag"/>.
        /// This will delete the file from the <paramref name="storage"/> the file is currently located at.
        /// </summary>
        /// <param name="storage">The storage the file is currently located at.</param>
        /// <param name="fileInfo">The file that should be moved.</param>
        /// <param name="tag">The tag the file is currently associated with.</param>
        /// <param name="destination">The storage the file will be moved to.</param>
        /// <param name="destinationTag">The tag the file should be associated with at the destination storage.</param>
        public static async Task MoveFileAsync(this IStorage storage, IFileInfo fileInfo, string tag, IStorage destination, string destinationTag)
        {
            var file = await storage.GetFileAsync(fileInfo, tag);
            if (file == null)
            {
                return;
            }

            await destination.CreateFileAsync(file, destinationTag);
            await storage.DeleteFileAsync(file, tag);
        }

        /// <summary>
        /// Moves the given <paramref name="fileInfo"/> located at the given <paramref name="storage"/> 
        /// to the <paramref name="destination"/> storage where it will be associated with the same tag.
        /// This will delete the file from the storage the file is currently located at.
        /// </summary>
        /// <param name="storage">The storage the file is currently located at.</param>
        /// <param name="fileInfo">The file that should be moved.</param>
        /// <param name="tag">The tag the file is currently associated with and should be associated with at the destination storage.</param>
        /// <param name="destination">The storage the file will be moved to.</param>
        public static Task MoveFileAsync(this IStorage storage, IFileInfo fileInfo, string tag, IStorage destination)
        {
            return storage.MoveFileAsync(fileInfo, tag, destination, tag);
        }

        /// <summary>
        /// Copies the given <paramref name="fileInfo"/> located at the given <paramref name="storage"/> 
        /// to the given <paramref name="destination"/> storage where it will be associated with the given <paramref name="destinationTag"/>.
        /// </summary>
        /// <param name="storage">The storage the file is currently located at.</param>
        /// <param name="fileInfo">The file that should be moved.</param>
        /// <param name="tag">The tag the file is currently associated with.</param>
        /// <param name="destination">The storage the file will be copied to.</param>
        /// <param name="destinationTag">The tag the file will be associated with.</param>
        public static async Task CopyFileToAsync(this IStorage storage, IFileInfo fileInfo, string tag, IStorage destination, string destinationTag)
        {
            var file = await storage.GetFileAsync(fileInfo, tag);
            if (file == null)
            {
                return;
            }

            await destination.CreateFileAsync(file, destinationTag);
        }

        /// <summary>
        /// Copies the given <paramref name="fileInfo"/> located at the given <paramref name="storage"/> 
        /// to the given <paramref name="destination"/> storage where it will be associated with the same tag.
        /// </summary>
        /// <param name="storage">The storage the file is currently located at.</param>
        /// <param name="fileInfo">The file that should be moved.</param>
        /// <param name="tag">The tag the file is currently associated with and will be associated with at the destination storage.</param>
        /// <param name="destination">The storage the file will be copied to.</param>
        public static Task CopyFileToAsync(this IStorage storage, IFileInfo fileInfo, string tag, IStorage destination)
        {
            return storage.CopyFileToAsync(fileInfo, tag, destination, tag);
        }

        /// <summary>
        /// Creates the given <paramref name="file"/> and associates
        /// the file with the given <paramref name="tag"/> synchronously.
        /// </summary>
        /// <param name="storage">The storage to create the file at.</param>
        /// <param name="file">The file to create.</param>
        /// <param name="tag">The tag to associate the file with.</param>
        /// <returns>
        /// A flag indicating whether the file was successfully created or not.
        /// </returns>
        public static bool CreateFile(this IStorage storage, IFile file, string tag) =>
            AsyncHelpers.RunSync(() => storage.CreateFileAsync(file, tag));

        /// <summary>
        /// Deletes the file corresponding to the given <paramref name="file"/> associated
        /// with the given <paramref name="tag"/> synchronously.
        /// </summary>
        /// <param name="storage">The storage to delete the file from.</param>
        /// <param name="file">The file to delete.</param>
        /// <param name="tag">The tag the file is currently associated with.</param>
        /// <returns>
        /// A flag indicating whether the file was successfully deleted or not.
        /// </returns>
        public static bool DeleteFile(this IStorage storage, IFileInfo file, string tag) => AsyncHelpers
            .RunSync(() => storage.DeleteFileAsync(file, tag));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="storage"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static bool DeleteTag(this IStorage storage, string tag) => AsyncHelpers
            .RunSync(() => storage.DeleteTagAsync(tag));
    }
}
