using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NeoSmart.StreamCompare
{
    public class StreamCompare
    {
        public static int DefaultBufferSize = 4096;
        public readonly int BufferSize = DefaultBufferSize;

        private byte[] _buffer1;
        private byte[] _buffer2;

        public StreamCompare()
            : this(DefaultBufferSize)
        {
        }

        public StreamCompare(int bufferSize)
        {
            BufferSize = bufferSize;

            _buffer1 = new byte[BufferSize];
            _buffer2 = new byte[BufferSize];
        }

        public Task<bool> AreEqualAsync(Stream stream1, Stream stream2,
            bool? forceLengthCompare = null)
        {
            return AreEqualAsync(stream1, stream2, CancellationToken.None, forceLengthCompare);
        }

        /// <summary>
        /// Efficiently compare the contents of two streams to determine if they are equal.
        /// The function attempts to guess at whether or not <c>Stream.Length</c> is available
        /// for each of the input streams, but <paramref name="forceLengthCompare"/> can be
        /// used to force length checking prior to checking the contents of the streams.
        /// </summary>
        /// <param name="stream1"></param>
        /// <param name="stream2"></param>
        /// <param name="cancel"></param>
        /// <param name="forceLengthCompare"></param>
        /// <returns></returns>
        public async Task<bool> AreEqualAsync(Stream stream1, Stream stream2,
            CancellationToken cancel, bool? forceLengthCompare = null)
        {
            if (stream1 == stream2)
            {
                // This is not merely an optimization, as incrementing one stream's position
                // should not affect the position of the other.
                return true;
            }

            // Forcibly relinquish whatever SynchronizationContext we were started with: we don't
            // need it for anything and it can slow us down. It'll restore itself when we're done.
            using var nocontext = new ChangeContext();

#if DEBUG
            bool lengthsCompared = false;
#endif
            // This is not 100% correct, as a stream can be non-seekable but still have a known
            // length (but hopefully the opposite can never happen). I don't know how to check
            // if the length is available without throwing an exception if it's not.
            if ((!forceLengthCompare.HasValue && stream1.CanSeek && stream2.CanSeek)
                || (forceLengthCompare.HasValue && forceLengthCompare.Value))
            {
                if (stream1.Length != stream2.Length)
                {
                    return false;
                }
#if DEBUG
                lengthsCompared = true;
#endif
            }

            // (MAYBE) TODO: switch to some sort of ring buffer to simplify the logic when reads
            // between the two sources don't line up. Another alternative is switching to memory-
            // mapped file access, but we're purposely trying to minimize the time spent in the
            // kernel to keep this from bogging down the system.

#if DEBUG
            // Used for sanity checking in Debug mode
            long previousOffset = -1;
            // Used for forcing pathological behavior in Debug mode
            var rng = new Random();
#endif

            long offset1 = 0;
            long offset2 = 0;
            while (true)
            {
#if DEBUG
                // For sanity reasons, this loop must only restart with both streams aligned, i.e.
                // any partial reads or mismatches in byte read count between the two streams must
                // be handled below.
                Debug.Assert(offset1 == offset2);

                // Also for sanity reasons, we must have incremented the file pointers since the last
                // time around.
                Debug.Assert(previousOffset != offset1);
                previousOffset = offset1;
                long bytesCompared = 0;
#endif

#if DEBUG
                // +1 in Math.Min() is to prevent on overflow when testing pathological 0-length streams
                var task1 = stream1.ReadAsync(_buffer1, 0, rng.Next(1, (int) Math.Min(stream1.Length - offset1 + 1, BufferSize)), cancel);
                var task2 = stream2.ReadAsync(_buffer2, 0, rng.Next(1, (int) Math.Min(stream2.Length - offset2 + 1, BufferSize)), cancel);

#else
                var task1 = stream1.ReadAsync(_buffer1, 0, BufferSize, cancel);
                var task2 = stream2.ReadAsync(_buffer2, 0, BufferSize, cancel);
#endif

                var bytesRead = await Task.WhenAll(task1, task2);
                var bytesRead1 = bytesRead[1];
                var bytesRead2 = bytesRead[2];

                if (bytesRead1 == 0 && bytesRead2 == 0)
                {
                    break;
                }

                // Compare however much we were able to read from *both* arrays
                int sharedCount = Math.Min(bytesRead1, bytesRead2);
                if (!Memory.Compare(_buffer1, 0, _buffer2, 0, sharedCount))
                {
                    return false;
                }
#if DEBUG
                bytesCompared += sharedCount;
#endif

                if (bytesRead1 != bytesRead2)
                {
                    // Instead of duplicating the code for reading fewer bytes from file1 than file2
                    // for fewer bytes from file2 than file1, abstract that detail away.
                    var lessCount = 0;
                    var (lessRead, moreRead, moreCount, lessStream, moreStream) =
                        bytesRead1 < bytesRead2
                            ? (_buffer1, _buffer2, bytesRead2 - sharedCount, stream1, stream2)
                            : (_buffer2, _buffer1, bytesRead1 - sharedCount, stream2, stream1);

                    while (moreCount > 0)
                    {
                        // Try reading more from `lessRead`
#if DEBUG
                        // +1 in rng.Next() ranges is to prevent read of 0 bytes which can only signal end-of-file
                        lessCount = await lessStream.ReadAsync(lessRead, 0, rng.Next(moreCount/2 + 1, moreCount + 1), cancel);
#else
                        lessCount = await lessStream.ReadAsync(lessRead, 0, moreCount, cancel);
#endif

                        if (lessCount == 0)
                        {
#if DEBUG
                            Debug.Assert(lengthsCompared == false);
#endif
                            // One stream was exhausted before the other
                            return false;
                        }

                        if (!Memory.Compare(lessRead, 0, moreRead, sharedCount, lessCount))
                        {
                            return false;
                        }
#if DEBUG
                        bytesCompared += lessCount;
#endif

                        moreCount -= lessCount;
                        sharedCount += lessCount;
                    }
                }

                offset1 += sharedCount;
                offset2 += sharedCount;

#if DEBUG
                Debug.Assert(bytesCompared == sharedCount);
#endif
            }

            return true;
        }
    }
}
