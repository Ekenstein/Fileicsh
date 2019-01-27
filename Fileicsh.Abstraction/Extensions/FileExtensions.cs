using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Fileicsh.Abstraction.Extensions
{
    public static class FileExtensions
    {
        /// <summary>
        /// Applies the extra value produced by the given <paramref name="extra"/> function
        /// which takes the underlying file as an argument and applies it to the given <paramref name="file"/>.
        /// </summary>
        /// <typeparam name="TExtra">The type of the extra value for the file.</typeparam>
        /// <param name="file">The file to apply the extra value to.</param>
        /// <param name="extra">The extra value to be applied.</param>
        /// <returns>An <see cref="IFile{TExtra}"/> containing the value produced by the given <paramref name="extra"/> function.</returns>
        public static IFile<TExtra> Apply<TExtra>(this IFile file, Func<IFile, TExtra> extra) => new AppliedFile<TExtra>(file, extra);

        /// <summary>
        /// Applies the extra value produced by the given <paramref name="extra"/> function 
        /// to the given <paramref name="file"/>.
        /// </summary>
        /// <typeparam name="TExtra">The type of the extra value for the file.</typeparam>
        /// <param name="file">The file to apply the extra value to.</param>
        /// <param name="extra">The extra value to be applied.</param>
        /// <returns>An <see cref="IFile{TExtra}"/> containing the value produced by the given <paramref name="extra"/> function.</returns>
        public static IFile<TExtra> Apply<TExtra>(this IFile file, Func<TExtra> extra) => new AppliedFile<TExtra>(file, extra);

        /// <summary>
        /// Applies the given <paramref name="extra"/> value to the given <paramref name="file"/>.
        /// </summary>
        /// <typeparam name="TExtra">The type of the extra value for the file.</typeparam>
        /// <param name="file">The file to apply the extra value to.</param>
        /// <param name="extra">The extra value to be applied.</param>
        /// <returns>An <see cref="IFile{TExtra}"/> containing the given <paramref name="extra"/>.</returns>
        public static IFile<TExtra> Apply<TExtra>(this IFile file, TExtra extra) => new AppliedFile<TExtra>(file, extra);

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

        /// <summary>
        /// Returns the MD5 hash of the given <paramref name="file"/>.
        /// </summary>
        /// <param name="file">The file to produce an MD5 hash of.</param>
        /// <returns>A byte array representing the MD5 hash of the file.</returns>
        public static Task<byte[]> GetMD5(this IFile file)
        {
            return file.ToMD5().GetHashAsync();
        }

        /// <summary>
        /// Returns the SHA256 hash of the given <paramref name="file"/>.
        /// </summary>
        /// <param name="file">The file to produce an SHA256 hash of.</param>
        /// <returns>A byte array representing the SHA256 hash of the file.</returns>
        public static Task<byte[]> GetSHA256(this IFile file)
        {
            return file.ToSHA256().GetHashAsync();
        }
    }
}
