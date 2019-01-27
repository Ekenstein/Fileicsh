using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ekenstein.Files
{
    public class RenamedFile<TExtra> : RenamedFile, IFile<TExtra>
    {
        private readonly IFileInfo<TExtra> _fileInfo;

        public RenamedFile(IFile<TExtra> file, string fileName, bool keepExtension) : base(file, fileName, keepExtension)
        {
            _fileInfo = file ?? throw new ArgumentNullException(nameof(file));
        }

        public TExtra Extra => _fileInfo.Extra;
    }

    public class RenamedFile : IFile
    {
        private readonly Lazy<string> _fileName;

        private readonly IFile _file;

        public string FileName => _fileName.Value;

        public string ContentType => _file.ContentType;

        public RenamedFile(IFile file, string fileName, bool keepExtension)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name must not be null or white space.");
            }

            _file = file ?? throw new ArgumentNullException(nameof(file));
            _fileName = new Lazy<string>(() => RenameFile(fileName, keepExtension));
        }

        public Task CopyToAsync(Stream outputStream, CancellationToken cancellationToken = default(CancellationToken)) => _file
            .CopyToAsync(outputStream);

        public Task<Stream> OpenReadStreamAsync(CancellationToken cancellationToken = default(CancellationToken)) => _file
            .OpenReadStreamAsync(cancellationToken);

        private string RenameFile(string fileName, bool keepExtension) => keepExtension
            ? Path.GetFileNameWithoutExtension(fileName) + Path.GetExtension(_file.FileName)
            : fileName;

        public void Dispose() => _file.Dispose();
    }
}
