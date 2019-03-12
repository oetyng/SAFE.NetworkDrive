using System.Collections.Generic;
using System.Globalization;

namespace SAFE.NetworkDrive.Mounter.Config
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class UserConfig
    {
        public string Username { get; set; }

        public List<DriveConfig> Drives { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        string DebuggerDisplay => $"{nameof(DriveConfig)} username='{Username}', drives='{Drives?.Count}'".ToString(CultureInfo.CurrentCulture);
    }
}