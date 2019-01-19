using System.Threading.Tasks;

namespace Ekenstein.Files.Extensions
{
    public static class StorageExtensions
    {
        /// <summary>
        /// Creates a prefixed storage of the given <paramref name="storage"/> which
        /// will prefix all the tags.
        /// </summary>
        /// <param name="storage">The storage to prefix.</param>
        /// <param name="prefix">The prefix.</param>
        /// <returns>A prefixed storage which will add the given <paramref name="prefix"/> to the tags.</returns>
        public static IStorage Prefix(this IStorage storage, string prefix)
        {
            return new PrefixedStorage(storage, prefix);
        }

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
    }
}
