using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace NeoSmart.StreamCompare.Tests
{
    /// <summary>
    /// A mock <c>MemoryStream</c> used for testing certain <c>StreamCompare</c>
    /// cases.
    /// </summary>
    [ExcludeFromCodeCoverage]
    class UnseekableMemoryStream : MemoryStream
    {
        private MemoryStream _mstream;
        public double ReadModifier { get; set; } =  1.0d;

        public UnseekableMemoryStream(byte[] backingBytes)
        {
            _mstream = new MemoryStream(backingBytes);
        }

        public override bool CanRead => _mstream.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => _mstream.CanWrite;
        public override long Length => throw new NotImplementedException();
        public override long Position
        {
            get => _mstream.Position;
            set => throw new NotImplementedException();
        }

        public override void Flush()
        {
            _mstream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _mstream.Read(buffer, offset, (int)(Math.Min(count, ReadModifier * count + 1)));
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _mstream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _mstream.Write(buffer, offset, count);
        }
    }
}
