using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fileicsh.Abstraction;
using Fileicsh.Abstraction.Extensions;
using Xunit;

namespace Fileicsh.Tests
{
    public abstract class BaseStorageTests : IDisposable
    {
        public static readonly Random Random = new Random(213981);
        private static readonly IFile File = Random.NextFile();
        protected IStorage Storage { get; }

        protected BaseStorageTests(IStorage storage)
        {
            Storage = storage ?? throw new ArgumentNullException(nameof(storage));
            foreach (var tag in Storage.GetAllTags())
            {
                storage.DeleteTag(tag);
            }
        }

        [Theory]
        [MemberData(nameof(FilesWithTag))]
        public virtual async Task TestGetTags(TagWithFiles testCase)
        {
            var previousTags = await Storage.GetTags().ToArrayAsync();

            var tag = testCase.Tag;
            await CreateFilesAsync(tag, testCase.Files);

            {
                var tags = await Storage.GetTags().ToArrayAsync();
                Assert.Equal(previousTags.Length + 1, tags.Length);
                Assert.Equal(tags.Length, tags.Distinct().Count());
            }

            foreach (var file in testCase.Files)
            {
                Assert.True(await Storage.DeleteFileAsync(file, tag));
            }

            {
                var tags = await Storage.GetTags().ToArrayAsync();
                Assert.Equal(previousTags.Length, tags.Length);
            }
        }

        [Theory]
        [MemberData(nameof(FilesWithTag))]
        public virtual async Task TestDeleteTagAsync(TagWithFiles testCase)
        {
            var previousTags = await Storage.GetTags().ToArrayAsync();
            await CreateFilesAsync(testCase.Tag, testCase.Files);
            var files = await Storage.GetFiles(testCase.Tag).ToArrayAsync();
            Assert.Equal(testCase.Files.Count(), files.Length);

            {
                var tags = await Storage.GetTags().ToArrayAsync();
                Assert.Equal(previousTags.Length + 1, tags.Length);
            }

            Assert.True(await Storage.DeleteTagAsync(testCase.Tag));
            var currentFiles = await Storage.GetFiles(testCase.Tag).ToArrayAsync();
            Assert.Empty(currentFiles);
            {
                var tags = await Storage.GetTags().ToArrayAsync();
                Assert.Equal(previousTags.Length, tags.Length);
            }
        }

        [Theory]
        [MemberData(nameof(FilesWithTag))]
        public virtual async Task TestCreateFileAsync(TagWithFiles testCase)
        {
            foreach (var file in testCase.Files)
            {
                var expectedSha256 = await file.GetSHA256Async();
                Assert.True(await Storage.CreateFileAsync(file, testCase.Tag));
                var actualFile = await Storage.GetFileAsync(file, testCase.Tag);
                Assert.NotNull(actualFile);

                var actualSha256 = await actualFile.GetSHA256Async();
                Assert.Equal(expectedSha256, actualSha256);
                Assert.Equal(file.FileName, actualFile.FileName);

                var fileWithSameName = Random.NextFile().Rename(file.FileName);
                Assert.Equal(file.FileName, fileWithSameName.FileName);
                expectedSha256 = await fileWithSameName.GetSHA256Async();
                Assert.True(await Storage.CreateFileAsync(fileWithSameName, testCase.Tag));
                actualFile = await Storage.GetFileAsync(fileWithSameName, testCase.Tag);
                actualSha256 = await actualFile.GetSHA256Async();
                Assert.Equal(expectedSha256, actualSha256);
                Assert.Equal(fileWithSameName.FileName, actualFile.FileName);

                var files = await Storage.GetFiles(testCase.Tag).ToArrayAsync();
                Assert.Contains(files, f => f.FileName == file.FileName);
            }
        }

        [Theory]
        [MemberData(nameof(FilesWithTag))]
        public virtual async Task TestGetFiles(TagWithFiles testCase)
        {
            Assert.Empty(await Storage.GetFiles(testCase.Tag).ToArrayAsync());

            foreach (var file in testCase.Files)
            {
                var expectedSha256 = await file.GetSHA256Async();
                var previousFiles = await Storage.GetFiles(testCase.Tag).ToArrayAsync();
                Assert.True(await Storage.CreateFileAsync(file, testCase.Tag));
                var actualFiles = await Storage.GetFiles(testCase.Tag).ToArrayAsync();
                
                Assert.Equal(previousFiles.Length + 1, actualFiles.Length);
                var actualFile = actualFiles.SingleOrDefault(f => f.FileName == file.FileName);
                Assert.NotNull(actualFile);
                var actualSha256 = await actualFile.GetSHA256Async();
                Assert.Equal(expectedSha256, actualSha256);
            }
        }

        [Theory]
        [MemberData(nameof(FilesWithTag))]
        public virtual async Task TestDeleteFileAsync(TagWithFiles testCase)
        {
            var previousTags = await Storage.GetTags().ToArrayAsync();
            foreach (var file in testCase.Files)
            {
                Assert.True(await Storage.CreateFileAsync(file, testCase.Tag));
                var actualFile = await Storage.GetFileAsync(file, testCase.Tag);
                Assert.NotNull(actualFile);

                Assert.True(await Storage.DeleteFileAsync(actualFile, testCase.Tag));
                Assert.False(await Storage.DeleteFileAsync(actualFile, testCase.Tag));

                Assert.Null(await Storage.GetFileAsync(file, testCase.Tag));

                var currentTags = await Storage.GetTags().ToArrayAsync();
                var files = await Storage.GetFiles(testCase.Tag).ToArrayAsync();

                if (files.Any())
                {
                    Assert.Equal(previousTags.Length + 1, currentTags.Length);
                }
                else
                {
                    Assert.Equal(previousTags.Length, currentTags.Length);
                }
            }
        }

        [Theory]
        [MemberData(nameof(FilesWithTag))]
        public virtual async Task TestMoveFileAsync(TagWithFiles testCase)
        {
            await CreateFilesAsync(testCase.Tag, testCase.Files);
            foreach (var file in testCase.Files)
            {
                var expectedSha256 = await file.GetSHA256Async();
                var destinationTag = Random.NextTag();
                await Storage.MoveFileAsync(file, testCase.Tag, destinationTag);
                var filesInTag = await Storage.GetFiles(testCase.Tag).ToArrayAsync();
                Assert.DoesNotContain(filesInTag, f => f.FileName == file.FileName);

                var filesInDestinationTag = await Storage.GetFiles(destinationTag).ToArrayAsync();
                var fileInDestinationTag = filesInDestinationTag.SingleOrDefault(f => f.FileName == file.FileName);
                Assert.NotNull(fileInDestinationTag);

                var actualSha256 = await fileInDestinationTag.GetSHA256Async();
                Assert.Equal(expectedSha256, actualSha256);
            }
        }

        private async Task CreateFilesAsync(AlphaNumericString tag, IEnumerable<IFile> files)
        {
            foreach (var file in files)
            {
                Assert.True(await Storage.CreateFileAsync(file, tag));
            }
        }

        public static IEnumerable<object[]> FilesWithTag()
        {
            for (var i = 0; i < 50; i++)
            {
                var tag = Random.NextTag();
                var numberOfFiles = Random.Next(1, 10);
                var files = new IFile[numberOfFiles];

                for (var j = 0; j < files.Length; j++)
                {
                    files[j] = Random.NextFile();
                }

                yield return new object[] { new TagWithFiles(tag, files) };
            }
        }

        public class TagWithFiles
        {
            public AlphaNumericString Tag { get; }
            public IReadOnlyList<IFile> Files { get; }

            public TagWithFiles(AlphaNumericString tag, IEnumerable<IFile> files)
            {
                Tag = tag;
                Files = files.ToArray();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                var tags = Storage.GetAllTags();
                foreach (var tag in tags)
                {
                    Storage.DeleteTag(tag);
                }

                Storage.Dispose();
            }
        }
    }
}
