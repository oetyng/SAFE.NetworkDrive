/*
The MIT License(MIT)
Copyright(c) 2015 IgorSoft
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
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SAFE.NetworkDrive.IO
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class ReadWriteSegregatingStream : Stream
    {
        readonly Stream _readStream;

        readonly Stream _writeStream;

        public Stream ReadStream => _readStream;

        public Stream WriteStream => _writeStream;

        public override bool CanRead => _readStream.CanRead;

        public override bool CanSeek => _writeStream.CanSeek && _readStream.CanSeek;

        public override bool CanTimeout => _writeStream.CanTimeout && _readStream.CanTimeout;

        public override bool CanWrite => _writeStream.CanWrite;

        public override long Length => _writeStream.Length;

        public override long Position
        {
            get { return _writeStream.Position; }
            set { _readStream.Position = _writeStream.Position = value; }
        }

        public override int ReadTimeout
        {
            get { return _readStream.ReadTimeout; }
            set { _readStream.ReadTimeout = value; }
        }

        public override int WriteTimeout
        {
            get { return _writeStream.WriteTimeout; }
            set { _writeStream.WriteTimeout = value; }
        }

        public ReadWriteSegregatingStream(Stream writeStream, Stream readStream)
        {
            this._writeStream = writeStream ?? throw new ArgumentNullException(nameof(writeStream));
            this._readStream = readStream ?? throw new ArgumentNullException(nameof(readStream));
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _readStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _writeStream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void Close()
        {
            _writeStream.Close();
            _readStream.Close();

            base.Close();
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            return _readStream.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            _writeStream.Dispose();
            _readStream.Dispose();

            base.Dispose(disposing);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return _readStream.EndRead(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            _writeStream.EndWrite(asyncResult);
        }

        public override void Flush()
        {
            _writeStream.Flush();
            _readStream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return Task.WhenAll(_writeStream.FlushAsync(cancellationToken), _readStream.FlushAsync(cancellationToken));
        }

        public override object InitializeLifetimeService()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _readStream.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _readStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override int ReadByte()
        {
            return _readStream.ReadByte();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            var writePosition = _writeStream.Seek(offset, origin);
            var readPosition = _readStream.Seek(offset, origin);

            if (writePosition != readPosition)
                throw new InvalidOperationException();

            return writePosition;
        }

        public override void SetLength(long value)
        {
            _writeStream.SetLength(value);
            _readStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _writeStream.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _writeStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override void WriteByte(byte value)
        {
            _writeStream.WriteByte(value);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay => $"{nameof(ReadWriteSegregatingStream)} Read={_readStream.GetType().Name} Write={_writeStream.GetType().Name} Position=[{_writeStream.Position}/{_readStream.Position}]".ToString(CultureInfo.CurrentCulture);
    }
}