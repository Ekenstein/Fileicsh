using Fileicsh.Abstraction;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fileicsh.Compression
{
    public class ZipArchiveEntryFile : IFile
    {
        private readonly ZipArchiveEntry _entry;

        /// <summary>
        /// Creates an <see cref="IFile"/> representation of the given <paramref name="entry"/>.
        /// </summary>
        /// <param name="entry">The zip archive entry to create an <see cref="IFile"/> representation of.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="entry"/> is null.</exception>
        public ZipArchiveEntryFile(ZipArchiveEntry entry)
        {
            _entry = entry ?? throw new ArgumentNullException(nameof(entry));
        }

        public string FileName => _entry.Name;

        public string ContentType => "application/octet-stream";

        public async Task CopyToAsync(Stream outputStream, CancellationToken cancellationToken = default)
        {
            using (var s = _entry.Open())
            {
                await s.CopyToAsync(s);
                await outputStream.FlushAsync(cancellationToken);
            }
        }

        public void Dispose()
        {
        }

        public Task<Stream> OpenReadStreamAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_entry.Open());
        }
    }
}
