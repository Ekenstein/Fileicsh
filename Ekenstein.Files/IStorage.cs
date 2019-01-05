using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ekenstein.Files
{
    public interface IStorage : IDisposable
    {
        Task<IReadOnlyList<string>> GetTagsAsync();
        Task<IFile> GetFileAsync(IFileInfo fileInfo, string tag);
        Task CreateFileAsync(IFile file, string tag);
        Task DeleteFileAsync(IFileInfo file, string tag);
        Task DeleteTagAsync(string tag);
        Task<IEnumerable<IFile>> GetFilesAsync(string tag);
    }
}
