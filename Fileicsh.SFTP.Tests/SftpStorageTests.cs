using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fileicsh.Abstraction;
using HeyRed.Mime;
using Renci.SshNet;
using Rngers;
using Xunit;
using Fileicsh.Extensions;

namespace Fileicsh.SFTP.Tests
{
    /// <summary>
    /// Tests for <see cref="SftpStorage"/>.
    /// Asserts that there is an SFTP server on your local machine,
    /// accessible through 127.0.0.1:22, with username `tester` and password `password`.
    /// </summary>
    public class SftpStorageTests : IDisposable
    {
        private sealed class FileEqualityComparer : IEqualityComparer<IFileInfo>
        {
            public bool Equals(IFileInfo x, IFileInfo y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                return string.Equals(x.FileName, y.FileName);
            }

            public int GetHashCode(IFileInfo obj)
            {
                unchecked
                {
                    return (obj.FileName.GetHashCode() * 397);
                }
            }
        }

        private static readonly IEqualityComparer<IFileInfo> FileComparer = new FileEqualityComparer();

        private static readonly Random Randomizer = new Random(24521);

        private static readonly string[] ContentTypes = new[]
        {
            "application/octet-stream",
            "text/plain",
            "image/jpeg",
            "application/javascript",
            "application/json",
            "application/xml"
        };

        private const string Host = "127.0.0.1";
        private const int Port = 22;
        private const string UserName = "tester";
        private const string Password = "password";

        private readonly SftpClient _sftpClient;

        private string[] _tags = new string[0];

        public SftpStorageTests()
        {
            _sftpClient = new SftpClient(Host, Port, UserName, Password);
        }

        public void Dispose()
        {
            using (var storage = new SftpStorage(_sftpClient))
            {
                var tags = storage.GetTagsAsync().Result;
                foreach (var tag in tags)
                {
                    var result = storage.DeleteTagAsync(tag).Result;
                }
            }
        }

        [Fact]
        public void TestConstructor()
        {
            {
                var instance = new SftpStorage(_sftpClient);
                Assert.Equal("/", instance.RootDirectory);
            }
            {
                const string rootDirectory = "/home";
                var instance = new SftpStorage(_sftpClient, rootDirectory);
                Assert.Equal(rootDirectory, instance.RootDirectory);
            }
            {
                Assert.Throws<ArgumentException>(() => new SftpStorage(_sftpClient, string.Empty));
                Assert.Throws<ArgumentNullException>(() => new SftpStorage(null));
            }
        }

        [Theory, MemberData(nameof(FilesAndTags))]
        public async Task TestCreateFileAsync(IFile file, string tag)
        {
            var instance = new SftpStorage(_sftpClient);
            var result = await instance.CreateFileAsync(file, tag);
            Assert.True(result);
            var files = await instance.GetFiles(tag).ToListAsync();
            Assert.Single(files);
            Assert.Contains(file, files, FileComparer);
        }

        public static IEnumerable<object[]> FilesAndTags()
        {
            for (var i = 0; i < 100; i++)
            {
                var length = Randomizer.Next(1, 1000);
                var data = new byte[length];
                Randomizer.NextBytes(data);

                var contentType = Randomizer.Next(ContentTypes);
                var extension = MimeTypesMap.GetExtension(contentType);
                var fileName = $"{Randomizer.NextString()}.{extension}";


                var tag = Randomizer.NextString(min: 1, alphabet: RandomExtensions.DefaultAlphabet + @"/\.");
                var file = new MemoryFile(new FileInfo(fileName, contentType), data);

                yield return new object[] {file, tag};
            }
        }
    }
}
