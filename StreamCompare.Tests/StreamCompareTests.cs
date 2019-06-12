using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace NeoSmart.StreamCompare.Tests
{
    [TestClass]
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

            Assert.IsTrue(await scompare.CompareAsync(stream1, stream1),
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

            Assert.IsTrue(await scompare.CompareAsync(stream1, stream2),
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

            Assert.IsFalse(await scompare.CompareAsync(stream1, stream2),
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

            try
            {
                Assert.IsTrue(await scompare.CompareAsync(stream1, stream2),
                    "Comparison of two empty streams return false");
            }
            catch
            {
                Assert.Fail("Comparing empty streams threw an exception");
            }
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
                try
                {
                    Assert.IsFalse(await scompare.CompareAsync(stream1, stream2),
                        "Comparing with an empty stream returned false match");
                }
                catch
                {
                    Assert.Fail("Comparing against an empty stream threw an exception");
                }
            }

            using (var stream1 = new MemoryStream(empty))
            using (var stream2 = new MemoryStream(nonempty))
            {
                try
                {
                    Assert.IsFalse(await scompare.CompareAsync(stream1, stream2),
                        "Comparing an empty stream against non-empty returned false match");
                }
                catch
                {
                    Assert.Fail("Comparing empty stream against non-empty threw an exception");
                }
            }
        }
    }
}
