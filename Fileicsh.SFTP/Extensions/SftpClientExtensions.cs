using System;
using System.Collections.Generic;
using System.IO;
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

        public static bool IsDirectory(this SftpFile file) => file.IsDirectory && file.Name != ".." && file.Name != ".";
    }
}
