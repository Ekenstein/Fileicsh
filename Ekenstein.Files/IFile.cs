using System;
using System.IO;
using System.Threading.Tasks;

namespace Ekenstein.Files
{
    public interface IFile : IFileInfo, IDisposable
    {
        Task CopyToAsync(Stream outputStream);
        Task<Stream> OpenReadStreamAsync();
    }
}
