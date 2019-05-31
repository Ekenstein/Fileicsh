using Fileicsh.Abstraction;

namespace Fileicsh.Tests
{
    public class MemoryStorageTests : BaseStorageTests
    {
        public MemoryStorageTests() : base(new MemoryStorage())
        {
        }
    }
}
