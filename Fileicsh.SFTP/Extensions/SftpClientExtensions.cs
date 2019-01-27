using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace Fileicsh.Extensions
{
    public static class SftpClientExtensions
    {
        /// <summary>
        /// List files in the given <paramref name="path"/> asynchronously.
        /// </summary>
        /// <param name="client">The SFTP client to use to list files.</param>
        /// <param name="path">The path to list files from.</param>
        /// <param name="cancellationToken">Cancellation token </param>
        /// <returns></returns>
        public static Task<IEnumerable<SftpFile>> ListDirectoryAsync(this SftpClient client, string path,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path must not be null or white space.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            return Task.Factory.FromAsync(
                client.BeginListDirectory(path, null, null),
                result => client.EndListDirectory(result));
        }

        public static Task DownloadToStreamAsync(this SftpClient client, string filePath, Stream outputStream, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path must not be null or white space.");
            }

            if (outputStream == null)
            {
                throw new ArgumentNullException(nameof(outputStream));
            }

            return Task.Factory.FromAsync(
                client.BeginDownloadFile(filePath, outputStream, null),
                result => client.EndDownloadFile(result));
        }

        /// <summary>
        /// Uploads the given <paramref name="inputStream"/> to the given <paramref name="path"/> asynchronously.
        /// </summary>
        /// <param name="client">The SFTP client to perform the upload action with.</param>
        /// <param name="path">The path to where the file should be uploaded to.</param>
        /// <param name="inputStream">The input stream containing the data to upload.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="client"/> or <paramref name="inputStream"/> is null.</exception>
        /// <exception cref="ArgumentException">If <paramref name="path"/> is null or white space.</exception>
        public static Task UploadFileAsync(this SftpClient client, string path, Stream inputStream, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (inputStream == null)
            {
                throw new ArgumentNullException(nameof(inputStream));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path must not be null or white space.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            return Task.Factory.FromAsync(client.BeginUploadFile(inputStream, path),
                result => client.EndUploadFile(result));
        }

        /// <summary>
        /// Deletes the directory at the given <paramref name="path"/> asynchronously.
        /// </summary>
        /// <param name="client">The SFTP client to perform the delete action with.</param>
        /// <param name="path">The path to the directory to delete.</param>
        /// <param name="recursive">Whether all files in the directory should be deleted or not.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> containing a flag indicating whether the directory was successfully deleted or not.</returns>
        public static async Task<bool> DeleteDirectoryAsync(this SftpClient client, string path, bool recursive, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path must not be null or white space.");
            }

            if (!client.Exists(path))
            {
                return false;
            }

            if (recursive)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var file = client.Get(path);

                cancellationToken.ThrowIfCancellationRequested();
                await DeleteDirectoryRecursiveAsync(client, file);
            }
            else
            {
                cancellationToken.ThrowIfCancellationRequested();
                var files = await client.ListDirectoryAsync(path);
                if (files.Any(f => f.Name != "." || f.Name != ".."))
                {
                    return false;
                }

                cancellationToken.ThrowIfCancellationRequested();
                client.DeleteDirectory(path);
            }

            return true;
        }

        private static async Task DeleteDirectoryRecursiveAsync(SftpClient client, SftpFile file)
        {
            if (file.Name == "." || file.Name == "..")
            {
                return;
            }

            if (file.IsDirectory)
            {
                var directoryFiles = await client.ListDirectoryAsync(file.FullName);
                foreach (var directoryFile in directoryFiles)
                {
                    await DeleteDirectoryRecursiveAsync(client, directoryFile);
                }
            }

            client.Delete(file.FullName);
        }
    }
}
