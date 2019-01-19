using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Ekenstein.Files.CloudStorage
{
    public class CloudBlockBlobEnumerator : IEnumerable<CloudBlockBlob>
    {
        private readonly CloudBlobContainer _container;

        public CloudBlockBlobEnumerator(CloudBlobContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        public IEnumerator<CloudBlockBlob> GetEnumerator()
        {
            BlobContinuationToken continuationToken = null;
            do
            {
                var blobs = _container.ListBlobsSegmented(continuationToken);

                foreach (var blob in blobs.Results.OfType<CloudBlockBlob>())
                {
                    yield return blob;
                }

                continuationToken = blobs.ContinuationToken;
            } while (continuationToken != null);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
