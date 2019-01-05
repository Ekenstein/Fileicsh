using System;

namespace Ekenstein.Files
{
    public class FileInfo<TExtra> : FileInfo, IFileInfo<TExtra>
    {
        private readonly Func<TExtra> _extra;

        public FileInfo(string fileName, Func<TExtra> extra) : base(fileName)
        {
            _extra = extra ?? throw new ArgumentNullException(nameof(extra));
        }

        public FileInfo(string fileName, string contentType, Func<TExtra> extra) : base(fileName, contentType)
        {
            _extra = extra ?? throw new ArgumentNullException(nameof(extra));
        }

        public FileInfo(string fileName, TExtra extra) : base(fileName)
        {
            _extra = () => extra;
        }

        public FileInfo(string fileName, string contentType, TExtra extra) : base(fileName, contentType)
        {
            _extra = () => extra;
        }

        public TExtra Extra => _extra();
    }

    public class FileInfo : IFileInfo
    {
        public string FileName { get; }

        public string ContentType { get; }

        public FileInfo(string fileName) : this(fileName, "application/octet-stream")
        {
        }

        public FileInfo(string fileName, string contentType)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name must not be null or white space.");
            }

            if (string.IsNullOrWhiteSpace(contentType))
            {
                throw new ArgumentException("Content type must not be null or white space.");
            }

            FileName = fileName;
            ContentType = contentType;
        }
    }
}
