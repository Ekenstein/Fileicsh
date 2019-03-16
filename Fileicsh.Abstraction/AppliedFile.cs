using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Fileicsh.Abstraction
{
    /// <inheritdoc />
    /// <summary>
    /// An abstraction which encapsulates an underlying file and applies
    /// a value of type <typeparamref name="TExtra" />.
    /// </summary>
    /// <typeparam name="TExtra">The type encapsulating the extra value of the file.</typeparam>
    public class AppliedFile<TExtra> : IFile<TExtra>
    {
        private readonly IFile _file;
        private readonly Func<IFile, TExtra> _extra;

        public TExtra Extra => _extra(_file);

        public string FileName => _file.FileName;

        public string ContentType => _file.ContentType;

        /// <summary>
        /// Creates a file that wraps the given <paramref name="file"/> and
        /// which will contain the extra value produced by the given <paramref name="extra"/>
        /// function.
        /// </summary>
        /// <param name="file">The file to apply the extra value to.</param>
        /// <param name="extra">The function taking the underlying file as an argument and produces the extra value.</param>
        public AppliedFile(IFile file, Func<IFile, TExtra> extra)
        {
            _file = file ?? throw new ArgumentNullException(nameof(file));
            _extra = extra ?? throw new ArgumentNullException(nameof(extra));
        }

        /// <summary>
        /// Creates a file that wraps the given <paramref name="file"/> and
        /// which will contain the extra value produced by the given <paramref name="extra"/> function.
        /// </summary>
        /// <param name="file">The file to apply the extra value to.</param>
        /// <param name="extra">The function producing the extra value.</param>
        public AppliedFile(IFile file, Func<TExtra> extra) : this(file, _ => extra())
        {
        }

        /// <summary>
        /// Creates a file that wraps the given <paramref name="file"/> and applies the
        /// given <paramref name="extra"/> value.
        /// </summary>
        /// <param name="file">The file to apply the extra value to.</param>
        /// <param name="extra">The extra value to apply to the underlying file.</param>
        /// <exception cref="ArgumentNullException">If either file or the extra value are null.</exception>
        public AppliedFile(IFile file, TExtra extra) : this(file, () => extra)
        {
        }

        public Task CopyToAsync(Stream outputStream, CancellationToken cancellationToken = default(CancellationToken)) => _file
            .CopyToAsync(outputStream, cancellationToken);

        public void Dispose() => _file.Dispose();

        public Task<Stream> OpenReadStreamAsync(CancellationToken cancellationToken = default(CancellationToken)) => _file.OpenReadStreamAsync(cancellationToken);


    }
}
