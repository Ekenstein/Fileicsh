using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Ekenstein.Files.Extensions
{
    public static class FileExtensions
    {
        public static IFile<TExtra> Apply<TExtra>(this IFile file, Func<TExtra> extra)
        {
            return new AppliedFile<TExtra>(file, extra);
        }

        public static IFile<TExtra> Apply<TExtra>(this IFile file, TExtra extra) => file.Apply(() => extra);

        /// <summary>
        /// Renames the given <paramref name="file"/> file to the given <paramref name="fileName"/>.
        /// If <paramref name="keepExtension"/> is true, the original file name's extension will
        /// be used on the new file name.
        /// </summary>
        /// <param name="file">The file to rename.</param>
        /// <param name="fileName">The new file name of the file.</param>
        /// <param name="keepExtension">Whether the old file name's extension should be kept in the new file name or not.</param>
        /// <returns>A <see cref="IFile"/> containing the renamed file.</returns>
        public static IFile Rename(this IFile file, string fileName, bool keepExtension = false)
        {
            return new RenamedFile(file, fileName, keepExtension);
        }

        /// <summary>
        /// Renames the given <paramref name="file"/> file to the given <paramref name="fileName"/>.
        /// If <paramref name="keepExtension"/> is true, the original file name's extension will
        /// be used on the new file name.
        /// </summary>
        /// <param name="file">The file to rename.</param>
        /// <param name="fileName">The new file name of the file.</param>
        /// <param name="keepExtension">Whether the old file name's extension should be kept in the new file name or not.</param>
        /// <returns>A <see cref="IFile"/> containing the renamed file.</returns>
        public static IFile<TExtra> Rename<TExtra>(this IFile<TExtra> file, string fileName, bool keepExtension = false)
        {
            return new RenamedFile<TExtra>(file, fileName, keepExtension);
        }

        /// <summary>
        /// Returns the string representation of the given <paramref name="file"/>
        /// encoded in UTF8.
        /// </summary>
        /// <param name="file">The file to retrieve the string representation of.</param>
        /// <returns>The string representation of the given <paramref name="file"/> encoded in UTF8.</returns>
        public static Task<string> GetStringContentAsync(this IFile file) => file.GetStringContentAsync(Encoding.UTF8);

        /// <summary>
        /// Returns the string representation of the given <paramref name="file"/> encoded with the given
        /// <paramref name="encoding"/>.
        /// </summary>
        /// <param name="file">The file to get the string representation of.</param>
        /// <param name="encoding">The encoding the string is encoded with.</param>
        /// <returns>The string representation of the given <paramref name="file"/>.</returns>
        public static async Task<string> GetStringContentAsync(this IFile file, Encoding encoding)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            using (var stream = await file.OpenReadStreamAsync())
            using (var streamReader = new StreamReader(stream, encoding))
            {
                return await streamReader.ReadToEndAsync();
            }
        }

        /// <summary>
        /// Returns the byte representation of the given <paramref name="file"/>.
        /// </summary>
        /// <param name="file">The file to retrieve the byte representation of.</param>
        /// <returns>An array of bytes of the given <paramref name="file"/>.</returns>
        public static async Task<byte[]> GetBytesAsync(this IFile file)
        {
            using (var stream = await file.OpenReadStreamAsync())
            using (var ms = new MemoryStream())
            {
                await stream.CopyToAsync(ms);
                await stream.FlushAsync();
                return ms.ToArray();
            }
        }

        public static IAuthenticatedFile ToAuth(this IFile file, HashAlgorithm hashAlgorithm, string hashName)
        {
            if (file is IAuthenticatedFile authFile && authFile.HashAlgorithm == hashName)
            {
                return authFile;
            }

            return new AuthenticatedFile(file, hashAlgorithm, hashName);
        }

        public static IAuthenticatedFile<TExtra> ToAuth<TExtra>(this IFile<TExtra> file, HashAlgorithm hashAlgorithm, string hashName)
        {
            if (file is IAuthenticatedFile<TExtra> authFile && authFile.HashAlgorithm == hashName)
            {
                return authFile;
            }

            return new AuthenticatedFile<TExtra>(file, hashAlgorithm, hashName);
        }

        public static IAuthenticatedFile<TExtra> ToSHA256<TExtra>(this IFile<TExtra> file) => ToAuth(file, SHA256.Create(), "SHA256");

        public static IAuthenticatedFile ToSHA256(this IFile file) => file.ToAuth(SHA256.Create(), "SHA256");

        public static IAuthenticatedFile<TExtra> ToMD5<TExtra>(this IFile<TExtra> file) => file.ToAuth(MD5.Create(), "MD5");

        public static IAuthenticatedFile ToMD5(this IFile file) => file.ToAuth(MD5.Create(), "MD5");

        public static Task<byte[]> GetMD5(this IFile file)
        {
            return file.ToMD5().GetHashAsync();
        }

        public static Task<byte[]> GetSHA256(this IFile file)
        {
            return file.ToSHA256().GetHashAsync();
        }
    }
}
