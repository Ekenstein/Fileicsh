using Fileicsh.Abstraction;

namespace Fileicsh.Tests
{
    public class FileSystemStorageTests : BaseStorageTests
    {
        private const string _rootPath = @"C:\temp\test";

        public FileSystemStorageTests() : base(new FileSystemStorage(_rootPath, true))
        {
        }
    }
}
