using Microsoft.Azure.Storage.Blob;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fileicsh.CloudStorage.Extensions
{
    public static class CloudStorageExtensions
    {
        public static async Task<IEnumerable<CloudBlobContainer>> ListContainersAsync(this CloudBlobClient client)
        {
            BlobContinuationToken token = null;
            var containers = new List<CloudBlobContainer>();
            do
            {
                var result = await client.ListContainersSegmentedAsync(token);
                token = result.ContinuationToken;
                containers.AddRange(result.Results);
            } while(token != null);

            return containers;
        }
    }
}
