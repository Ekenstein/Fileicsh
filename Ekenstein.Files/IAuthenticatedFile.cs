using System.Threading.Tasks;

namespace Ekenstein.Files
{
    public interface IAuthenticatedFile : IFile
    {
        string HashAlgorithm { get; }
        Task<byte[]> GetHashAsync();
    }
}
