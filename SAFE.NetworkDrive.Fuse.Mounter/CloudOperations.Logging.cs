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

using Mono.Unix.Native;
using System;
using System.Globalization;
using System.IO;
//using SAFE.NetworkDrive.Extensions;
//using FileAccess = DokanNet.FileAccess;

namespace SAFE.NetworkDrive
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal partial class CloudOperations
    {
        private Errno AsTrace(string method, string fileName, FuseFileInfo info, FuseResult result, params string[] parameters)
        {
            var extraParameters = parameters != null && parameters.Length > 0 ? ", " + string.Join(", ", parameters) : string.Empty;

            _logger?.Trace($"{System.Threading.Thread.CurrentThread.ManagedThreadId:D2} / {_drive.DisplayRoot} {method}({fileName}, {info.ToTrace()}{extraParameters}) -> {result}".ToString(CultureInfo.CurrentCulture));

            return (Errno)result;
        }

        private Errno AsTrace(string method, string fileName, FuseFileInfo info, FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, FuseResult result)
        {
            _logger?.Trace($"{System.Threading.Thread.CurrentThread.ManagedThreadId:D2} / {_drive.DisplayRoot} {method}({fileName}, {info.ToTrace()}, [{access}], [{share}], [{mode}], [{options}], [{attributes}]) -> {result}".ToString(CultureInfo.CurrentCulture));

            return (Errno)result;
        }

        private Errno AsDebug(string method, string fileName, FuseFileInfo info, FuseResult result, params string[] parameters)
        {
            var extraParameters = parameters != null && parameters.Length > 0 ? ", " + string.Join(", ", parameters) : string.Empty;

            _logger?.Debug($"{System.Threading.Thread.CurrentThread.ManagedThreadId:D2} / {_drive.DisplayRoot} {method}({fileName}, {info.ToTrace()}{extraParameters}) -> {result}".ToString(CultureInfo.CurrentCulture));

            return (Errno)result;
        }

        private Errno AsDebug(string method, string fileName, FuseFileInfo info, FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, FuseResult result)
        {
            _logger?.Debug($"{System.Threading.Thread.CurrentThread.ManagedThreadId:D2} / {_drive.DisplayRoot} {method}({fileName}, {info.ToTrace()}, [{access}], [{share}], [{mode}], [{options}], [{attributes}]) -> {result}".ToString(CultureInfo.CurrentCulture));

            return (Errno)result;
        }

        private Errno AsWarn(string method, string fileName, FuseFileInfo info, FuseResult result, params string[] parameters)
        {
            var extraParameters = parameters != null && parameters.Length > 0 ? ", " + string.Join(", ", parameters) : string.Empty;

            _logger?.Warn($"{System.Threading.Thread.CurrentThread.ManagedThreadId:D2} / {_drive.DisplayRoot} {method}({fileName}, {info.ToTrace()}{extraParameters}) -> {result}".ToString(CultureInfo.CurrentCulture));

            return (Errno)result;
        }

        private Errno AsError(string method, string fileName, FuseFileInfo info, FuseResult result, params string[] parameters)
        {
            var extraParameters = parameters != null && parameters.Length > 0 ? ", " + string.Join(", ", parameters) : string.Empty;

            _logger?.Error($"{System.Threading.Thread.CurrentThread.ManagedThreadId:D2} / {_drive.DisplayRoot} {method}({fileName}, {info.ToTrace()}{extraParameters}) -> {result}".ToString(CultureInfo.CurrentCulture));

            return (Errno)result;
        }

        private Errno AsError(string method, string fileName, FuseFileInfo info, FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, FuseResult result)
        {
            _logger?.Error($"{System.Threading.Thread.CurrentThread.ManagedThreadId:D2} / {_drive.DisplayRoot} {method}({fileName}, {info.ToTrace()}, [{access}], [{share}], [{mode}], [{options}], [{attributes}]) -> {result}".ToString(CultureInfo.CurrentCulture));

            return (Errno)result;
        }
    }

    class FuseFileInfo
    {
        public bool IsDirectory { get; internal set; }
        public object Context { get; internal set; }
        public bool DeleteOnClose { get; internal set; }

        internal object ToTrace()
            => "FuseFileInfo ToTrace not implemented";
    }

    enum FuseResult
    {
        Success,
        Error,
        NotImplemented,
        FileNotFound,
        PathNotFound,
        DirectoryNotEmpty,
        DiskFull,
        AccessDenied,
        AlreadyExists,
        FileExists
    }

    enum FileAccess
    {
        ReadData,
        WriteData,
        GenericRead,
        ReadAttributes,
        Delete,
        ReadPermissions
    }

    public struct FileInformation
    {
        public FileInformation(string filename, Stat stat)
        {
            FileName = filename;
            CreationTime = new LocalTimeSpec(stat.st_ctim).ToDateTime();
            LastAccessTime = new LocalTimeSpec(stat.st_atim).ToDateTime();
            LastWriteTime = new LocalTimeSpec(stat.st_mtim).ToDateTime();
            Length = stat.st_size;
            Attributes = FileAttributes.NotContentIndexed;
        }
        public string FileName { get; set; }
        public FileAttributes Attributes { get; set; }
        public DateTime? CreationTime { get; set; }
        public DateTime? LastAccessTime { get; set; }
        public DateTime? LastWriteTime { get; set; }
        public long Length { get; set; }

        internal Stat GetStat()
        {
            return new Stat
            {
                st_atime = LastAccessTime.HasValue ? new DateTimeOffset(LastAccessTime.Value).ToUnixTimeSeconds() : 0,
                st_ctime = CreationTime.HasValue ? new DateTimeOffset(CreationTime.Value).ToUnixTimeSeconds() : 0,
                st_mtime = LastWriteTime.HasValue ? new DateTimeOffset(LastWriteTime.Value).ToUnixTimeSeconds() : 0,
                st_size = Length,
                st_ino = (ulong)FileName.GetHashCode()
            };
        }
    }

    public class LocalTimeSpec
    {
        const long NanosPerSecond = 1000 * 1000 * 1000;
        const long NanosPerTick = 100;
        const long TicksPerSecond = NanosPerSecond / NanosPerTick;
        static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        public LocalTimeSpec(long tv_sec, long tv_nsec) 
            //: this(tv_sec, tv_nsec, ClockType.Realtime)
        { }

        public LocalTimeSpec(Timespec timespec)
        {
            _tv_sec = timespec.tv_sec;
            _tv_nsec = timespec.tv_nsec;
            //this.clock_type = clock_type;
        }

        long _tv_sec;
        long _tv_nsec;
        //ClockType clock_type;

        /// <summary>
        /// Timespec a long time in the future.
        /// </summary>
        public static LocalTimeSpec InfFuture
            => new LocalTimeSpec(long.MaxValue, 0);

        /// <summary>
        /// Timespec a long time in the past.
        /// </summary>
        public static LocalTimeSpec InfPast
            => new LocalTimeSpec(long.MinValue, 0);

        public DateTime ToDateTime()
        {
            //GrpcPreconditions.CheckState(tv_nsec >= 0 && tv_nsec < NanosPerSecond);
            //GrpcPreconditions.CheckState(clock_type == ClockType.Realtime);

            // fast path for InfFuture
            if (this.Equals(InfFuture))
                return DateTime.MaxValue;

            // fast path for InfPast
            if (this.Equals(InfPast))
                return DateTime.MinValue;

            try
            {
                // convert nanos to ticks, round up to the nearest tick
                long ticksFromNanos = _tv_nsec / NanosPerTick + ((_tv_nsec % NanosPerTick != 0) ? 1 : 0);
                long ticksTotal = checked(_tv_sec * TicksPerSecond + ticksFromNanos);
                return UnixEpoch.AddTicks(ticksTotal);
            }
            catch (OverflowException)
            {
                // ticks out of long range
                return _tv_sec > 0 ? DateTime.MaxValue : DateTime.MinValue;
            }
            catch (ArgumentOutOfRangeException)
            {
                // resulting date time would be larger than MaxValue
                return _tv_sec > 0 ? DateTime.MaxValue : DateTime.MinValue;
            }
        }
    }
}