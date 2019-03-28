using System;
using System.Collections.Generic;
using System.Globalization;

namespace SAFE.NetworkDrive.Mounter.Config
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class UserConfig
    {
        /// <summary>
        /// Used for seeding VolumeIds.
        /// </summary>
        public uint VolumeNrCheckpoint { get; set; }
        public List<DriveConfig> Drives { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        string DebuggerDisplay => $"{nameof(DriveConfig)} drives='{Drives?.Count}'".ToString(CultureInfo.CurrentCulture);
    }
}