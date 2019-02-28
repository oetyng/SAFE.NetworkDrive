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
using NLog;

namespace SAFE.NetworkDrive.IO
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class TraceStream : Stream
    {
        readonly string _name;

        readonly string _fileName;

        readonly Stream _baseStream;

        bool _disposed;

        readonly object _lockObject = new object();

#pragma warning disable CS3003
        public ILogger Logger { get; set; }
#pragma warning restore CS3003

        public TraceStream(string name, string fileName, Stream baseStream)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));
            if (baseStream == null)
                throw new ArgumentNullException(nameof(baseStream));

            _name = name;
            _fileName = fileName;
            _baseStream = baseStream;
        }

        private void Trace(string message)
        {
            Logger.Trace($"[{Thread.CurrentThread.ManagedThreadId}] {_name}: '{_fileName}' {message}".ToString(CultureInfo.CurrentCulture));
        }

        private T Trace<T>(string message, T result)
        {
            Logger.Trace($"[{Thread.CurrentThread.ManagedThreadId}] {_name}: '{_fileName}' {message} => {result}".ToString(CultureInfo.CurrentCulture));
            return result;
        }

        private void Trace<T>(T value, string message)
        {
            Logger.Trace($"[{Thread.CurrentThread.ManagedThreadId}] {_name}: '{_fileName}' {message}={value}".ToString(CultureInfo.CurrentCulture));
        }

        public override long Length
        {
            get
            {
                lock (_lockObject)
                {
                    return Trace($"get_{nameof(Length)}".ToString(CultureInfo.InvariantCulture), _baseStream.Length);
                }
            }
        }

        public override long Position
        {
            get
            {
                lock (_lockObject)
                {
                    return Trace($"get_{nameof(Position)}".ToString(CultureInfo.InvariantCulture), _baseStream.Position);
                }
            }

            set
            {
                lock (_lockObject)
                {
                    Trace(value, $"set_{nameof(Position)}".ToString(CultureInfo.InvariantCulture));
                    _baseStream.Position = value;
                }
            }
        }

        public override int ReadTimeout
        {
            get
            {
                lock (_lockObject)
                {
                    return Trace($"get_{nameof(ReadTimeout)}".ToString(CultureInfo.InvariantCulture), _baseStream.ReadTimeout);
                }
            }

            set
            {
                lock (_lockObject)
                {
                    Trace(value, $"set_{nameof(ReadTimeout)}".ToString(CultureInfo.InvariantCulture));
                    _baseStream.ReadTimeout = value;
                }
            }
        }

        public override int WriteTimeout
        {
            get
            {
                lock (_lockObject)
                {
                    return Trace($"get_{nameof(WriteTimeout)}".ToString(CultureInfo.InvariantCulture), _baseStream.WriteTimeout);
                }
            }

            set
            {
                lock (_lockObject)
                {
                    Trace(value, $"set_{nameof(WriteTimeout)}".ToString(CultureInfo.InvariantCulture));
                    _baseStream.WriteTimeout = value;
                }
            }
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            lock (_lockObject)
            {
                Trace($"{nameof(BeginRead)}(buffer=[{buffer?.Length ?? -1}], offset={offset}, count={count})".ToString(CultureInfo.CurrentCulture));
                return _baseStream.BeginRead(buffer, offset, count, callback, state);
            }
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            lock (_lockObject)
            {
                Trace($"{nameof(BeginWrite)}(buffer=[{buffer?.Length ?? -1}], offset={offset}, count={count})".ToString(CultureInfo.CurrentCulture));
                return _baseStream.BeginWrite(buffer, offset, count, callback, state);
            }
        }

        public override bool CanRead
        {
            get
            {
                lock (_lockObject)
                {
                    return Trace($"get_{nameof(CanRead)}".ToString(CultureInfo.InvariantCulture), _baseStream.CanRead);
                }
            }
        }

        public override bool CanSeek
        {
            get
            {
                lock (_lockObject)
                {
                    return Trace($"get_{nameof(CanSeek)}".ToString(CultureInfo.InvariantCulture), _baseStream.CanSeek);
                }
            }
        }

        public override bool CanTimeout
        {
            get
            {
                lock (_lockObject)
                {
                    return Trace($"get_{nameof(CanTimeout)}".ToString(CultureInfo.InvariantCulture), _baseStream.CanTimeout);
                }
            }
        }

        public override bool CanWrite
        {
            get
            {
                lock (_lockObject)
                {
                    return Trace($"get_{nameof(CanWrite)}".ToString(CultureInfo.InvariantCulture), _baseStream.CanWrite);
                }
            }
        }

        public override void Close()
        {
            lock (_lockObject)
            {
                Trace($"{nameof(Close)}()".ToString(CultureInfo.InvariantCulture));
                _baseStream.Close();
            }

            base.Close();
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            lock (_lockObject)
            {
                Trace($"{nameof(CopyToAsync)}(destination={destination?.GetType()?.Name}, bufferSize={bufferSize})".ToString(CultureInfo.CurrentCulture));
                return _baseStream.CopyToAsync(destination, bufferSize, cancellationToken);
            }
        }

        protected override void Dispose(bool disposing)
        {
            lock (_lockObject)
            {
                if (disposing && !_disposed)
                {
                    Trace($"{nameof(Dispose)}(disposing={disposing})".ToString(CultureInfo.CurrentCulture));

                    _baseStream.Dispose();
                    _disposed = true;
                }
            }

            base.Dispose(disposing);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            lock (_lockObject)
            {
                return Trace($"{nameof(EndRead)}(asyncResult.IsCompleted={asyncResult?.IsCompleted})".ToString(CultureInfo.CurrentCulture), _baseStream.EndRead(asyncResult));
            }
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            lock (_lockObject)
            {
                _baseStream.EndWrite(asyncResult);
                Trace($"{nameof(EndWrite)}(asyncResult.IsCompleted={asyncResult?.IsCompleted})".ToString(CultureInfo.CurrentCulture));
            }
        }

        public override void Flush()
        {
            lock (_lockObject)
            {
                Trace($"{nameof(Flush)}()".ToString(CultureInfo.InvariantCulture));
                _baseStream.Flush();
            }
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            lock (_lockObject)
            {
                Trace($"{nameof(FlushAsync)}()".ToString(CultureInfo.InvariantCulture));
                return _baseStream.FlushAsync(cancellationToken);
            }
        }

        public override object InitializeLifetimeService()
        {
            lock (_lockObject)
            {
                Trace($"{nameof(InitializeLifetimeService)}()".ToString(CultureInfo.InvariantCulture));
                return _baseStream.InitializeLifetimeService();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (_lockObject)
            {
                return Trace($"{nameof(Read)}(buffer=[{buffer?.Length ?? -1}], offset={offset}, count={count}) <Position={_baseStream.Position}>".ToString(CultureInfo.CurrentCulture), _baseStream.Read(buffer, offset, count));
            }
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            lock (_lockObject)
            {
                Trace($"{nameof(ReadAsync)}(buffer=[{buffer?.Length ?? -1}], offset={offset}, count={count}) <Position={_baseStream.Position}>".ToString(CultureInfo.CurrentCulture));
                return _baseStream.ReadAsync(buffer, offset, count, cancellationToken);
            }
        }

        public override int ReadByte()
        {
            lock (_lockObject)
            {
                return Trace($"{nameof(ReadByte)}()".ToString(CultureInfo.InvariantCulture), _baseStream.ReadByte());
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            lock (_lockObject)
            {
                return Trace($"{nameof(Seek)}(offset={offset}, origin={origin})".ToString(CultureInfo.CurrentCulture), _baseStream.Seek(offset, origin));
            }
        }

        public override void SetLength(long value)
        {
            lock (_lockObject)
            {
                Trace($"{nameof(SetLength)}(value={value})".ToString(CultureInfo.CurrentCulture));
                _baseStream.SetLength(value);
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (_lockObject)
            {
                Trace($"{nameof(Write)}(buffer=[{buffer?.Length ?? -1}], offset={offset}, count={count}) <Position={_baseStream.Position}>".ToString(CultureInfo.CurrentCulture));
                _baseStream.Write(buffer, offset, count);
            }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            lock (_lockObject)
            {
                Trace($"{nameof(WriteAsync)}(buffer=[{buffer?.Length ?? -1}], offset={offset}, count={count}) <Position={_baseStream.Position}>".ToString(CultureInfo.CurrentCulture));
                return _baseStream.WriteAsync(buffer, offset, count, cancellationToken);
            }
        }

        public override void WriteByte(byte value)
        {
            lock (_lockObject)
            {
                Trace($"{nameof(WriteByte)}(value={value})".ToString(CultureInfo.CurrentCulture));
                _baseStream.WriteByte(value);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay => $"{nameof(TraceStream)}[{_baseStream.GetType().Name}]".ToString(CultureInfo.CurrentCulture);
    }
}