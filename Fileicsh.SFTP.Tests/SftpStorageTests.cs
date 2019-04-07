using System;
using Renci.SshNet;
using Fileicsh.Tests;

namespace Fileicsh.SFTP.Tests
{
    /// <summary>
    /// Tests for <see cref="SftpStorage"/>.
    /// Asserts that there is an SFTP server on your local machine,
    /// accessible through 127.0.0.1:22, with username `tester` and password `password`.
    /// </summary>
    public class SftpStorageTests : BaseStorageTests
    {
        private static readonly Random Randomizer = new Random(24521);

        private const string Host = "127.0.0.1";
        private const int Port = 22;
        private const string UserName = "tester";
        private const string Password = "password";

        public SftpStorageTests() : base(new SftpStorage(new SftpClient(Host, Port, UserName, Password)))
        {
        }
    }
}
