using System;
using System.Collections;
using System.Collections.Generic;
using Renci.SshNet;
using Xunit;

namespace Fileicsh.SFTP.Tests
{
    /// <summary>
    /// Tests for <see cref="SftpStorage"/>.
    /// Asserts that there is an SFTP server on your local machine,
    /// accessible through 127.0.0.1:22, with username `tester` and password `password`.
    /// </summary>
    public class SftpStorageTests : IDisposable
    {
        private static readonly Random Randomizer = new Random();

        private const string Host = "127.0.0.1";
        private const int Port = 22;
        private const string UserName = "tester";
        private const string Password = "password";

        private readonly SftpClient _sftpClient;

        public SftpStorageTests()
        {
            _sftpClient = new SftpClient(Host, Port, UserName, Password);
        }

        public void Dispose()
        {
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

        [Fact]
        public void TestCreateFileAsync()
        {
            var instance = new SftpStorage(_sftpClient);
        }

        public static IEnumerable<object[]> FilesAndTags()
        {
            for (var i = 0; i < 1000; i++)
            {
                var length = Randomizer.Next();
                var data = new byte[length];
                Randomizer.NextBytes(data);
            }
        }
    }
}
