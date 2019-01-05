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
    }
}
