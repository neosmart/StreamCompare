using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NeoSmart.StreamCompare.Tests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class FileCompareTests
    {
        const uint FileSize = 2048;

        [TestMethod]
        public void FileCompareInit()
        {
            // With the default buffer size
            var fcompare = new FileCompare();

            // With a custom buffer size
            fcompare = new FileCompare((uint)(StreamCompare.DefaultBufferSize / 7));
        }

        [TestMethod]
        public async Task BasicFileCompare()
        {
            var path1 = Path.GetRandomFileName();
            var path2 = Path.GetRandomFileName();

            using (var file1 = File.Create(path1))
            using (var file2 = File.Create(path2))
            {
                var bytes1 = new byte[FileSize];
                var bytes2 = new byte[FileSize];

                var rng = new Random();
                rng.NextBytes(bytes1);
                rng.NextBytes(bytes2);

                var tasks = new[]
                {
                    file1.WriteAsync(bytes1),
                    file2.WriteAsync(bytes2),
                };

                await Task.WhenAll(tasks.Select(t => t.AsTask()));
            }

            var fileInfo1 = new FileInfo(path1);
            var fileInfo2 = new FileInfo(path2);

            Assert.AreEqual(FileSize, fileInfo1.Length);
            Assert.AreEqual(FileSize, fileInfo2.Length);

            var fcompare = new FileCompare();
            Assert.IsFalse(await fcompare.AreEqualAsync(path1, path2));

            // These will throw if the handles haven't been closed, which would indicate a bug
            File.Delete(path1);
            File.Delete(path2);
        }

        [TestMethod]
        public async Task CompareSameFile()
        {
            var rng = new Random();
            var path1 = Path.GetRandomFileName();

            using (var file1 = File.Create(path1))
            {
                var bytes1 = new byte[FileSize];
                rng.NextBytes(bytes1);

                await file1.WriteAsync(bytes1);
            }

            var fileInfo1 = new FileInfo(path1);
            Assert.AreEqual(FileSize, fileInfo1.Length);

            var fcompare = new FileCompare();
            Assert.IsTrue(await fcompare.AreEqualAsync(path1, path1));

            File.Delete(path1);
        }

        [TestMethod]
        public async Task CompareIdenticalFiles()
        {
            var rng = new Random();
            var path1 = Path.GetRandomFileName();
            var path2 = Path.GetRandomFileName();

            using (var file1 = File.Create(path1))
            using (var file2 = File.Create(path2))
            {
                var bytes = new byte[FileSize];
                rng.NextBytes(bytes);

                var tasks = new[]
                {
                    file1.WriteAsync(bytes),
                    file2.WriteAsync(bytes),
                };

                await Task.WhenAll(tasks.Select(t => t.AsTask()));
            }

            var fcompare = new FileCompare();
            Assert.IsTrue(await fcompare.AreEqualAsync(path1, path2));

            File.Delete(path1);
            File.Delete(path2);
        }

        [TestMethod]
        public async Task CompareDifferentLengths()
        {
            var bytes = new byte[StreamCompare.DefaultBufferSize * 2];

            var path1 = Path.GetRandomFileName();
            var path2 = Path.GetRandomFileName();

            using (var file1 = File.Create(path1))
            using (var file2 = File.Create(path2))
            {
                var tasks = new[]
                {
                    file1.WriteAsync(bytes),
                    file2.WriteAsync(bytes.AsMemory(12))
                };

                await Task.WhenAll(tasks.Select(t => t.AsTask()));
            }

            // And use a custom buffer size
            var fcompare = new FileCompare(3907); // prime number for good measure
            Assert.IsFalse(await fcompare.AreEqualAsync(path1, path2));
        }

        [TestMethod]
        public async Task TestCancellation()
        {
            var bytes = new byte[StreamCompare.DefaultBufferSize];

            var path1 = Path.GetRandomFileName();
            var path2 = Path.GetRandomFileName();

            using (var file1 = File.Create(path1))
            using (var file2 = File.Create(path2))
            {
                var tasks = new[]
                {
                    file1.WriteAsync(bytes),
                    file2.WriteAsync(bytes),
                };

                await Task.WhenAll(tasks.Select(t => t.AsTask()));
            }

            var fcompare = new FileCompare();
            var canceled = new CancellationToken(true);
            await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
            {
                await fcompare.AreEqualAsync(path1, path2, canceled);
            });

            File.Delete(path1);
            File.Delete(path2);
        }
    }
}
