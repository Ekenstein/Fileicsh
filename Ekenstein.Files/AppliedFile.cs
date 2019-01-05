using System;
using System.IO;
using System.Threading.Tasks;

namespace Ekenstein.Files
{
    public class AppliedFile<TExtra> : IFile<TExtra>
    {
        private readonly IFile _file;
        private readonly Func<TExtra> _extra;

        public TExtra Extra => _extra();

        public string FileName => _file.FileName;

        public string ContentType => _file.ContentType;

        public AppliedFile(IFile file, Func<TExtra> extra)
        {
            _file = file ?? throw new ArgumentNullException(nameof(file));
            _extra = extra ?? throw new ArgumentNullException(nameof(extra));
        }

        public AppliedFile(IFile file, TExtra extra) : this(file, () => extra)
        {
        }

        public Task CopyToAsync(Stream outputStream) => _file.CopyToAsync(outputStream);

        public void Dispose() => _file.Dispose();

        public Task<Stream> OpenReadStreamAsync() => _file.OpenReadStreamAsync();
    }
}
