﻿/*
The MIT License(MIT)
Copyright(c) 2017 IgorSoft
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.IO;
using Moq;
using System.Threading;
using System.Threading.Tasks;

namespace SAFE.NetworkDrive.Tests
{
    public partial class ReadWriteSegregatingStreamTest
    {
        public interface IStream
        {
            bool CanRead { get; }

            bool CanSeek { get; }

            bool CanTimeout { get; }

            bool CanWrite { get; }

            long Length { get; }

            long Position { get; set; }

            int ReadTimeout { get; set; }

            int WriteTimeout { get; set; }

            IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state);

            IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state);

            Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken);

            void Close();

            int EndRead(IAsyncResult asyncResult);

            int EndWrite(IAsyncResult asyncResult);

            void Flush();

            Task FlushAsync(CancellationToken cancellationToken);

            long Seek(long offset, SeekOrigin origin);

            void SetLength(long value);

            int Read(byte[] buffer, int offset, int count);

            Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);

            int ReadByte();

            void Write(byte[] buffer, int offset, int count);

            Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);

            void WriteByte(byte value);
        }

        internal class Fixture
        {
            private class StreamFake : Stream
            {
                readonly IStream _stream;

                public override bool CanRead => _stream.CanRead;

                public override bool CanSeek => _stream.CanSeek;

                public override bool CanWrite => _stream.CanWrite;

                public override bool CanTimeout => _stream.CanTimeout;

                public override long Length => _stream.Length;

                public override long Position
                {
                    get { return _stream.Position; }
                    set { _stream.Position = value; }
                }

                public override int ReadTimeout
                {
                    get { return _stream.ReadTimeout; }
                    set { _stream.ReadTimeout = value; }
                }

                public override int WriteTimeout
                {
                    get { return _stream.WriteTimeout; }
                    set { _stream.WriteTimeout = value; }
                }

                public StreamFake(IStream stream)
                {
                    _stream = stream;
                }

                public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
                {
                    return _stream.BeginRead(buffer, offset, count, callback, state);
                }

                public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
                {
                    return _stream.BeginWrite(buffer, offset, count, callback, state);
                }

                public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
                {
                    return _stream.CopyToAsync(destination, bufferSize, cancellationToken);
                }

                public override void Close()
                {
                    _stream.Close();
                    base.Close();
                }

                public override int EndRead(IAsyncResult asyncResult)
                {
                    return _stream.EndRead(asyncResult);
                }

                public override void EndWrite(IAsyncResult asyncResult)
                {
                    _stream.EndWrite(asyncResult);
                }

                public override void Flush()
                {
                    _stream.Flush();
                }

                public override Task FlushAsync(CancellationToken cancellationToken)
                {
                    return _stream.FlushAsync(cancellationToken);
                }

                public override int Read(byte[] buffer, int offset, int count)
                {
                    return _stream.Read(buffer, offset, count);
                }

                public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
                {
                    return _stream.ReadAsync(buffer, offset, count, cancellationToken);
                }

                public override int ReadByte()
                {
                    return _stream.ReadByte();
                }

                public override long Seek(long offset, SeekOrigin origin)
                {
                    return _stream.Seek(offset, origin);
                }

                public override void SetLength(long value)
                {
                    _stream.SetLength(value);
                }

                public override void Write(byte[] buffer, int offset, int count)
                {
                    _stream.Write(buffer, offset, count);
                }

                public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
                {
                    return _stream.WriteAsync(buffer, offset, count, cancellationToken);
                }

                public override void WriteByte(byte value)
                {
                    _stream.WriteByte(value);
                }
            }

            MockRepository _mockRepository = new MockRepository(MockBehavior.Strict);

            Stream CreateStream(out Mock<IStream> streamMock)
            {
                streamMock = _mockRepository.Create<IStream>();
                return new StreamFake(streamMock.Object);
            }

            public Stream CreateStream()
            {
                return CreateStream(out _);
            }

            public Stream CreateStream_ForCanRead(bool canRead = true)
            {
                var stream = CreateStream(out Mock<IStream> streamMock);

                streamMock
                    .Setup(s => s.CanRead)
                    .Returns(canRead)
                    .Verifiable();

                return stream;
            }

            public Stream CreateStream_ForCanWrite(bool canWrite = true)
            {
                var stream = CreateStream(out Mock<IStream> streamMock);

                streamMock
                    .Setup(s => s.CanWrite)
                    .Returns(canWrite)
                    .Verifiable();

                return stream;
            }

            public Stream CreateStream_ForCanTimeout(bool canTimeout = true)
            {
                var stream = CreateStream(out Mock<IStream> streamMock);

                streamMock
                    .Setup(s => s.CanTimeout)
                    .Returns(canTimeout)
                    .Verifiable();

                return stream;
            }

            public Stream CreateStream_ForGetReadTimeout(int readTimeout)
            {
                var stream = CreateStream(out Mock<IStream> streamMock);

                streamMock
                    .SetupGet(s => s.ReadTimeout)
                    .Returns(readTimeout)
                    .Verifiable();

                return stream;
            }

            public Stream CreateStream_ForSetReadTimeout(int readTimeout)
            {
                var stream = CreateStream(out Mock<IStream> streamMock);

                streamMock
                    .SetupSet(s => s.ReadTimeout = readTimeout)
                    .Verifiable();

                return stream;
            }

            public Stream CreateStream_ForGetWriteTimeout(int writeTimeout)
            {
                var stream = CreateStream(out Mock<IStream> streamMock);

                streamMock
                    .SetupGet(s => s.WriteTimeout)
                    .Returns(writeTimeout)
                    .Verifiable();

                return stream;
            }

            public Stream CreateStream_ForSetWriteTimeout(int writeTimeout)
            {
                var stream = CreateStream(out Mock<IStream> streamMock);

                streamMock
                    .SetupSet(s => s.WriteTimeout = writeTimeout)
                    .Verifiable();

                return stream;
            }

            public Stream CreateStream_ForBeginRead(byte[] buffer, AsyncCallback callback, object state)
            {
                var stream = CreateStream(out Mock<IStream> streamMock);

                streamMock
                    .Setup(s => s.BeginRead(buffer, 0, buffer.Length, callback, state))
                    .Returns(Task.FromResult(buffer.Length))
                    .Verifiable();

                return stream;
            }

            public Stream CreateStream_ForBeginWrite(byte[] buffer, AsyncCallback callback, object state)
            {
                var stream = CreateStream(out Mock<IStream> streamMock);

                streamMock
                    .Setup(s => s.BeginWrite(buffer, 0, buffer.Length, callback, state))
                    .Returns(Task.FromResult(buffer.Length))
                    .Verifiable();

                return stream;
            }

            public Stream CreateStream_ForCopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                var stream = CreateStream(out Mock<IStream> streamMock);

                streamMock
                    .Setup(s => s.CopyToAsync(destination, bufferSize, cancellationToken))
                    .Returns(Task.CompletedTask)
                    .Verifiable();

                return stream;
            }

            public Stream CreateStream_ForEndRead(Task<int> asyncResult)
            {
                var stream = CreateStream(out Mock<IStream> streamMock);

                streamMock
                    .Setup(s => s.EndRead(asyncResult))
                    .Returns(asyncResult.Result)
                    .Verifiable();

                return stream;
            }

            public Stream CreateStream_ForEndWrite(Task<int> asyncResult)
            {
                var stream = CreateStream(out Mock<IStream> streamMock);

                streamMock
                    .Setup(s => s.EndWrite(asyncResult))
                    .Returns(asyncResult.Result)
                    .Verifiable();

                return stream;
            }

            public Stream CreateStream_ForFlush()
            {
                var stream = CreateStream(out Mock<IStream> streamMock);

                streamMock
                    .Setup(s => s.Flush())
                    .Verifiable();

                return stream;
            }

            public Stream CreateStream_ForFlushAsync(CancellationToken cancellationToken)
            {
                var stream = CreateStream(out Mock<IStream> streamMock);

                streamMock
                    .Setup(s => s.FlushAsync(cancellationToken))
                    .Returns(Task.CompletedTask)
                    .Verifiable();

                return stream;
            }

            public Stream CreateStream_ForRead(byte[] buffer, int offset, int count)
            {
                var stream = CreateStream(out Mock<IStream> streamMock);

                streamMock
                    .Setup(s => s.Read(buffer, offset, count))
                    .Returns(count)
                    .Verifiable();

                return stream;
            }

            public Stream CreateStream_ForReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                var stream = CreateStream(out Mock<IStream> streamMock);

                streamMock
                    .Setup(s => s.ReadAsync(buffer, offset, count, cancellationToken))
                    .Returns(Task.FromResult(count))
                    .Verifiable();

                return stream;
            }

            public Stream CreateStream_ForReadByte(int result)
            {
                var stream = CreateStream(out Mock<IStream> streamMock);

                streamMock
                    .Setup(s => s.ReadByte())
                    .Returns(result)
                    .Verifiable();

                return stream;
            }

            public Stream CreateStream_ForSeek(long offset, SeekOrigin origin, long position)
            {
                var stream = CreateStream(out Mock<IStream> streamMock);

                streamMock
                    .Setup(s => s.Seek(offset, origin))
                    .Returns(position)
                    .Verifiable();

                return stream;
            }

            public Stream CreateStream_ForSetLength(long value)
            {
                var stream = CreateStream(out Mock<IStream> streamMock);

                streamMock
                    .Setup(s => s.SetLength(value))
                    .Verifiable();

                return stream;
            }

            public Stream CreateStream_ForWrite(byte[] buffer, int offset, int count)
            {
                var stream = CreateStream(out Mock<IStream> streamMock);

                streamMock
                    .Setup(s => s.Write(buffer, offset, count))
                    .Verifiable();

                return stream;
            }

            public Stream CreateStream_ForWriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                var stream = CreateStream(out Mock<IStream> streamMock);

                streamMock
                    .Setup(s => s.WriteAsync(buffer, offset, count, cancellationToken))
                    .Returns(Task.CompletedTask)
                    .Verifiable();

                return stream;
            }

            public Stream CreateStream_ForWriteByte(byte value)
            {
                var stream = CreateStream(out Mock<IStream> streamMock);

                streamMock
                    .Setup(s => s.WriteByte(value))
                    .Verifiable();

                return stream;
            }

            public void Verify()
            {
                _mockRepository.Verify();
            }
        }
    }
}