using System.IO;

namespace Tests
{
    class DelegatingStream : Stream
    {
        Stream stream;

        public DelegatingStream(Stream s) {
            stream = s;
        }

        public override bool CanRead => stream.CanRead;
        public override bool CanSeek => stream.CanSeek;
        public override bool CanTimeout => stream.CanTimeout;
        public override bool CanWrite => stream.CanWrite;
        public override long Length => stream.Length;

        public override long Position {
            get { return stream.Position; }
            set { stream.Position = value; }
        }

        public override int ReadTimeout {
            get { return stream.ReadTimeout; }
            set { stream.ReadTimeout = value; }
        }

        public override int WriteTimeout {
            get { return stream.WriteTimeout; }
            set { stream.WriteTimeout = value; }
        }

        public override void Close() => stream.Close();

        public override void Flush() => stream.Flush();

        public override int Read(byte[] buffer, int offset, int count) => stream.Read(buffer, offset, count);

        public override int ReadByte() => stream.ReadByte();

        public override long Seek(long offset, SeekOrigin origin) => stream.Seek(offset, origin);

        public override void SetLength(long value) => stream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => stream.Write(buffer, offset, count);

        public override void WriteByte(byte value) => stream.WriteByte(value);
    }
}