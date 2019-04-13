using Fileicsh.Abstraction;
using Fileicsh.Abstraction.Extensions;
using Fileicsh.Crypto.Extensions;
using Fileicsh.Tests;
using Org.BouncyCastle.Bcpg.OpenPgp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Fileicsh.Crypto.Tests
{
    public class PgpFileTests
    {
        private readonly PgpPublicKey _publicKey;
        private readonly PgpSecretKeyRing _privateKey;
        private static readonly Random Random = new Random(5843309);

        public PgpFileTests()
        {
            var keyGenerator = PgpHelper.GenerateKeyRingGenerator("test", "test");
            _publicKey = keyGenerator
                    .GeneratePublicKeyRing()
                    .GetPublicKeys()
                    .OfType<PgpPublicKey>()
                    .FirstOrDefault(p => p.IsEncryptionKey);

            _privateKey = keyGenerator.GenerateSecretKeyRing();
        }

        [Theory]
        [MemberData(nameof(Files))]
        public async Task TestEncryptAndDecrypt(IFile file)
        {
            var expectedSha256 = await file.GetSHA256Async();
            var actualSha256 = await file
                .ToEncrypted(_publicKey)
                .ToDecrypted(_privateKey, "test")
                .GetSHA256Async();

            Assert.Equal(expectedSha256, actualSha256);
        }

        [Theory, MemberData(nameof(Files))]
        public async Task TestSignAndVerify(IFile file)
        {
            var expectedSha256 = await file.GetSHA256Async();
            var actualSha256 = await file
                .ToSigned(_privateKey, "test")
                .ToVerified(_publicKey)
                .GetSHA256Async();

            Assert.Equal(expectedSha256, actualSha256);
        }

        [Theory, MemberData(nameof(Files))]
        public async Task TestSignAndEncrypt(IFile file)
        {
            var expectedSha256 = await file.GetSHA256Async();
            var actualSha256 = await file
                .ToSignedAndEncrypted(_publicKey, _privateKey, "test")
                .ToVerifiedAndDecrypted(_privateKey.GetPublicKey(), _privateKey, "test")
                .GetSHA256Async();

            Assert.Equal(expectedSha256, actualSha256);
        }

        public static IEnumerable<object[]> Files()
        {
            for (var i = 0; i < 50; i++)
            {
                yield return new object[] { Random.NextFile() };
            }
        }
    }
}
