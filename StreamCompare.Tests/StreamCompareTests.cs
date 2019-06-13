using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NeoSmart.StreamCompare.Tests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class StreamCompareTests
    {
        [TestMethod]
        public void StreamCompareInit()
        {
            var scompare1 = new StreamCompare();
            Assert.IsNotNull(scompare1);

            var scompare2 = new StreamCompare(StreamCompare.DefaultBufferSize - 20);
            Assert.AreEqual(StreamCompare.DefaultBufferSize - 20, scompare2.BufferSize,
                "StreamCompare not created with specified buffer size");

            var oldDefaultBufferSize = StreamCompare.DefaultBufferSize;
            StreamCompare.DefaultBufferSize -= 200;
            Assert.AreEqual(oldDefaultBufferSize - 200, StreamCompare.DefaultBufferSize,
                "DefaultBufferSize changes did not persist");

            var scompare3 = new StreamCompare();
            Assert.AreEqual(StreamCompare.DefaultBufferSize, scompare3.BufferSize,
                "StreamCompare not using DefaultBufferSize");

            var scompare4 = new StreamCompare(1024);
            Assert.AreEqual(1024, scompare4.BufferSize,
                "StreamCompare constructor buffer size not overriding DefaultBufferSize");
        }

        [TestMethod]
        public async Task CompareSame()
        {
            var bytes = new byte[100];
            for (int i = 0; i < bytes.Length; ++i)
            {
                bytes[i] = 0;
            }

            var scompare = new StreamCompare();
            using var stream1 = new MemoryStream(bytes);

            Assert.IsTrue(await scompare.AreEqualAsync(stream1, stream1),
                "StreamCompare mismatch for stream against itself");
        }

        [TestMethod]
        public async Task CompareEqual()
        {
            var bytes1 = new byte[100];
            var bytes2 = new byte[100];
            for (int i = 0; i < bytes1.Length; ++i)
            {
                bytes1[i] = (byte) i;
                bytes2[i] = (byte) i;
            }

            var scompare = new StreamCompare();
            using var stream1 = new MemoryStream(bytes1);
            using var stream2 = new MemoryStream(bytes2);

            Assert.IsTrue(await scompare.AreEqualAsync(stream1, stream2),
                "StreamCompare mismatch for streams with identical content");
        }

        [TestMethod]
        public Task CompareDifferent()
        {
            return CompareDifferent(100);
        }

        [TestMethod]
        public Task CompareDifferentHuge()
        {
            return CompareDifferent(StreamCompare.DefaultBufferSize * 8);
        }

        public async Task CompareDifferent(int count)
        {
            var rng = new Random();
            var bytes1 = new byte[count];
            var bytes2 = new byte[count];

            rng.NextBytes(bytes1);
            rng.NextBytes(bytes2);

            var scompare = new StreamCompare();
            using var stream1 = new MemoryStream(bytes1);
            using var stream2 = new MemoryStream(bytes2);

            Assert.IsFalse(await scompare.AreEqualAsync(stream1, stream2),
                "StreamCompare match for streams with differing content");
        }

        [TestMethod]
        public async Task CompareDifferentLengths()
        {
            var rng = new Random();
            var bytes1 = new byte[StreamCompare.DefaultBufferSize];
            var bytes2 = new byte[StreamCompare.DefaultBufferSize / 2 + StreamCompare.DefaultBufferSize / 4 + 7];

            rng.NextBytes(bytes1);
            rng.NextBytes(bytes2);

            var scompare = new StreamCompare();
            using var stream1 = new MemoryStream(bytes1);
            using var stream2 = new MemoryStream(bytes2);

            // First try with explicit `Length` short-circuiting
            Assert.IsFalse(await scompare.AreEqualAsync(stream1, stream2, true),
                "StreamCompare match for streams with differing content");

            // Then try with explicit no `Length` short-circuiting
            Assert.IsFalse(await scompare.AreEqualAsync(stream1, stream2, false),
                "StreamCompare match for streams with differing content");
        }


        [TestMethod]
        public async Task CompareEmpty()
        {
            var bytes1 = new byte[0];
            var bytes2 = new byte[0];

            var scompare = new StreamCompare();
            using var stream1 = new MemoryStream(bytes1);
            using var stream2 = new MemoryStream(bytes2);

            Assert.IsTrue(await scompare.AreEqualAsync(stream1, stream2),
                "Comparison of two empty streams return false");
        }

        [TestMethod]
        public async Task CompareWithEmpty()
        {
            var nonempty = new byte[10];
            var empty = new byte[0];

            var scompare = new StreamCompare();

            using (var stream1 = new MemoryStream(nonempty))
            using (var stream2 = new MemoryStream(empty))
            {
                Assert.IsFalse(await scompare.AreEqualAsync(stream1, stream2),
                    "Comparing with an empty stream returned false match");
            }

            using (var stream1 = new MemoryStream(empty))
            using (var stream2 = new MemoryStream(nonempty))
            {
                Assert.IsFalse(await scompare.AreEqualAsync(stream1, stream2),
                    "Comparing an empty stream against non-empty returned false match");
            }
        }

        [TestMethod]
        public async Task TestCancellation()
        {
            var bytes = new byte[StreamCompare.DefaultBufferSize];

            using (var stream1 = new MemoryStream(bytes))
            using (var stream2 = new MemoryStream(bytes))
            {
                var canceled = new CancellationToken(true);

                var scompare = new StreamCompare();
                await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
                {
                    await scompare.AreEqualAsync(stream1, stream2, canceled);
                });
            }
        }

        [TestMethod]
        public async Task StreamsDifferingInLastByte()
        {
            var bytes1 = new byte[StreamCompare.DefaultBufferSize * 2 + (StreamCompare.DefaultBufferSize - 7)];
            var bytes2 = new byte[bytes1.Length];

            var rng = new Random();
            rng.NextBytes(bytes1);

            // Make bytes2 a copy of bytes1 but differing in the very last byte only
            bytes1.CopyTo(bytes2, 0);
            bytes2[bytes2.Length - 1] += 1;

            using (var stream1 = new MemoryStream(bytes1))
            using (var stream2 = new MemoryStream(bytes2))
            {
                var scompare = new StreamCompare();
                Assert.IsFalse(await scompare.AreEqualAsync(stream1, stream2));
            }
        }

        [TestMethod]
        public async Task DuplicateStreamsDifferentLengths()
        {
            var bytes1 = new byte[StreamCompare.DefaultBufferSize];
            var bytes2 = new byte[bytes1.Length];

            var rng = new Random();
            rng.NextBytes(bytes1);
            bytes1.CopyTo(bytes2, 0);

            Array.Resize(ref bytes2, bytes2.Length - 7);

            using (var stream1 = new MemoryStream(bytes1))
            using (var stream2 = new MemoryStream(bytes2))
            {
                var scompare = new StreamCompare();
                Assert.IsFalse(await scompare.AreEqualAsync(stream1, stream2, false));
            }
        }

        [TestMethod]
        public async Task DifferentStreamsDifferentLengths()
        {
            var bytes1 = new byte[StreamCompare.DefaultBufferSize * 2];
            var bytes2 = new byte[bytes1.Length];

            var rng = new Random();
            rng.NextBytes(bytes1);
            bytes1.CopyTo(bytes2, 0);

            // Make bytes2 a different length (shorter)
            Array.Resize(ref bytes2, bytes2.Length - 7);

            // Make bytes2 not match bytes1 in the second buffer
            bytes2[StreamCompare.DefaultBufferSize] += 1;

            using (var stream1 = new MemoryStream(bytes1))
            using (var stream2 = new MemoryStream(bytes2))
            {
                var scompare = new StreamCompare();
                Assert.IsFalse(await scompare.AreEqualAsync(stream1, stream2, false));
            }
        }

        [TestMethod]
        public async Task UnseekableStreamComparison()
        {
            var bytes1 = new byte[StreamCompare.DefaultBufferSize * 2];
            var bytes2 = new byte[bytes1.Length];

            var rng = new Random();
            rng.NextBytes(bytes1);

            bytes1.CopyTo(bytes2, 0);

            using (var stream1 = new MemoryStream(bytes1))
            using (var stream2 = new UnseekableMemoryStream(bytes2))
            {
                var scompare = new StreamCompare();
                Assert.IsTrue(await scompare.AreEqualAsync(stream1, stream2, false));
            }
        }

        [TestMethod]
        public async Task UnevenReadComparisonMatching()
        {
            var bytes1 = new byte[StreamCompare.DefaultBufferSize * 2];
            var bytes2 = new byte[bytes1.Length];

            var rng = new Random();
            rng.NextBytes(bytes1);

            bytes1.CopyTo(bytes2, 0);

            using (var stream1 = new MemoryStream(bytes1))
            using (var stream2 = new UnseekableMemoryStream(bytes2))
            {
                stream2.ReadModifier = 0.7;

                var scompare = new StreamCompare();
                Assert.IsTrue(await scompare.AreEqualAsync(stream1, stream2, false));
            }
        }

        [TestMethod]
        public async Task UnevenReadComparisonNotMatching()
        {
            var bytes1 = new byte[StreamCompare.DefaultBufferSize * 2];
            var bytes2 = new byte[bytes1.Length];

            var rng = new Random();
            rng.NextBytes(bytes1);

            bytes1.CopyTo(bytes2, 0);
            bytes2[bytes2.Length - 1] += 1;

            using (var stream1 = new MemoryStream(bytes1))
            using (var stream2 = new UnseekableMemoryStream(bytes2))
            {
                stream2.ReadModifier = 0.7;

                var scompare = new StreamCompare();
                // Automatic forceLengthCompare
                Assert.IsFalse(await scompare.AreEqualAsync(stream1, stream2));
            }

            // And again with stream1 reading less
            using (var stream1 = new UnseekableMemoryStream(bytes1))
            using (var stream2 = new MemoryStream(bytes2))
            {
                stream1.ReadModifier = 0.7;

                var scompare = new StreamCompare();
                // Explicit forceLengthCompare
                Assert.IsFalse(await scompare.AreEqualAsync(stream1, stream2, false));
            }
        }
    }
}
