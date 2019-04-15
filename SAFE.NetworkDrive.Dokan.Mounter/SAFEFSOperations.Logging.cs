using System.Globalization;
using System.IO;
using DokanNet;
using SAFE.NetworkDrive.Extensions;
using FileAccess = DokanNet.FileAccess;

namespace SAFE.NetworkDrive
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal partial class SAFEFSOperations
    {
        NtStatus AsTrace(string method, string fileName, DokanFileInfo info, NtStatus result, params string[] parameters)
        {
            var extraParameters = parameters != null && parameters.Length > 0 ? ", " + string.Join(", ", parameters) : string.Empty;
            _logger?.Trace($"{System.Threading.Thread.CurrentThread.ManagedThreadId:D2} / {_drive.DisplayRoot} {method}({fileName}, {info.ToTrace()}{extraParameters}) -> {result}".ToString(CultureInfo.CurrentCulture));
            return result;
        }

        NtStatus AsTrace(string method, string fileName, DokanFileInfo info, FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, NtStatus result)
        {
            _logger?.Trace($"{System.Threading.Thread.CurrentThread.ManagedThreadId:D2} / {_drive.DisplayRoot} {method}({fileName}, {info.ToTrace()}, [{access}], [{share}], [{mode}], [{options}], [{attributes}]) -> {result}".ToString(CultureInfo.CurrentCulture));
            return result;
        }

        NtStatus AsDebug(string method, string fileName, DokanFileInfo info, NtStatus result, params string[] parameters)
        {
            var extraParameters = parameters != null && parameters.Length > 0 ? ", " + string.Join(", ", parameters) : string.Empty;
            _logger?.Debug($"{System.Threading.Thread.CurrentThread.ManagedThreadId:D2} / {_drive.DisplayRoot} {method}({fileName}, {info.ToTrace()}{extraParameters}) -> {result}".ToString(CultureInfo.CurrentCulture));
            return result;
        }

        NtStatus AsDebug(string method, string fileName, DokanFileInfo info, FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, NtStatus result)
        {
            _logger?.Debug($"{System.Threading.Thread.CurrentThread.ManagedThreadId:D2} / {_drive.DisplayRoot} {method}({fileName}, {info.ToTrace()}, [{access}], [{share}], [{mode}], [{options}], [{attributes}]) -> {result}".ToString(CultureInfo.CurrentCulture));
            return result;
        }

        NtStatus AsWarn(string method, string fileName, DokanFileInfo info, NtStatus result, params string[] parameters)
        {
            var extraParameters = parameters != null && parameters.Length > 0 ? ", " + string.Join(", ", parameters) : string.Empty;
            _logger?.Warn($"{System.Threading.Thread.CurrentThread.ManagedThreadId:D2} / {_drive.DisplayRoot} {method}({fileName}, {info.ToTrace()}{extraParameters}) -> {result}".ToString(CultureInfo.CurrentCulture));
            return result;
        }

        NtStatus AsError(string method, string fileName, DokanFileInfo info, NtStatus result, params string[] parameters)
        {
            var extraParameters = parameters != null && parameters.Length > 0 ? ", " + string.Join(", ", parameters) : string.Empty;
            _logger?.Error($"{System.Threading.Thread.CurrentThread.ManagedThreadId:D2} / {_drive.DisplayRoot} {method}({fileName}, {info.ToTrace()}{extraParameters}) -> {result}".ToString(CultureInfo.CurrentCulture));
            return result;
        }

        NtStatus AsError(string method, string fileName, DokanFileInfo info, FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, NtStatus result)
        {
            _logger?.Error($"{System.Threading.Thread.CurrentThread.ManagedThreadId:D2} / {_drive.DisplayRoot} {method}({fileName}, {info.ToTrace()}, [{access}], [{share}], [{mode}], [{options}], [{attributes}]) -> {result}".ToString(CultureInfo.CurrentCulture));
            return result;
        }
    }
}