using Fileicsh.Abstraction;
using Fileicsh.Tests;
using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Fileicsh.CloudStorage.Tests
{
    public class CloudStorageTests : BaseStorageTests
    {
        public CloudStorageTests() : base(CloudStorage.DevelopmentStorage)
        {
        }

        [Theory, MemberData(nameof(InvalidContainerNames))]
        public async Task GetGetFilesWithInvalidContainerName(AlphaNumericString tag)
        {
            try 
            {
                await Storage.GetFiles(tag).FirstOrDefaultAsync();
            }
            catch
            {
                Assert.False(true, "Expected storage to pad the tag to necessary length.");
            }
        }

        public static IEnumerable<object[]> InvalidContainerNames()
        {
            for (var i = 0; i < 100; i++)
            {
                var tooLong = Random.NextBool();
                if (tooLong)
                {
                    yield return new object[] { Random.NextTag(64) };
                }
                else
                {
                    yield return new object[] { Random.NextTag(0, 2) };
                }
            }
        }
    }
}
