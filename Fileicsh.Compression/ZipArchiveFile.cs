using Fileicsh.Abstraction;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fileicsh.Compression
{
    /// <summary>
    /// Represents a zip archive containing zero or more entries.
    /// </summary>
    public class ZipArchiveFile : IFile<IReadOnlyList<IFile>>
    {
        /// <summary>
        /// The compression level each entry should be compressed with.
        /// Default is no compression.
        /// </summary>
        public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.NoCompression;

        /// <summary>
        /// Creates a zip archive containing the given entries.
        /// </summary>
        /// <param name="entries">The entries to add to a zip archive.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="entries"/> is null.</exception>
        public ZipArchiveFile(string fileName, IEnumerable<IFile> entries)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name must not be null or white space.");
            }

            FileName = fileName;
            Extra = entries?.ToArray() ?? throw new ArgumentNullException(nameof(entries));
        }

        public IReadOnlyList<IFile> Extra { get; }

        public string FileName { get; }

        public string ContentType => "application/zip";

        public async Task CopyToAsync(Stream outputStream, CancellationToken cancellationToken = default)
        {
            using (var zipArchive = new ZipArchive(outputStream, ZipArchiveMode.Create, true, Encoding.UTF8))
            {
                foreach (var file in Extra)
                {
                    var entry = zipArchive.CreateEntry(file.FileName, CompressionLevel);
                    using (var readableStream = await file.OpenReadStreamAsync(cancellationToken))
                    using (var writableStream = entry.Open())
                    {
                        await readableStream.CopyToAsync(writableStream);
                        await writableStream.FlushAsync(cancellationToken);
                    }
                }
            }
        }

        public void Dispose()
        {
        }

        public async Task<Stream> OpenReadStreamAsync(CancellationToken cancellationToken = default)
        {
            var tempFile = Path.GetTempFileName();
            using (var fs = File.Create(tempFile))
            {
                await CopyToAsync(fs, cancellationToken);
                await fs.FlushAsync(cancellationToken);
            }

            return new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.None, 1 << 16, FileOptions.DeleteOnClose);
        }
    }
}
