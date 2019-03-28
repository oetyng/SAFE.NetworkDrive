using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace SAFE.NetworkDrive.Mounter.Config
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class DriveConfig
    {
        [Required]
        public string Schema { get; set; } // evaluate necessity of this one

        [Required]
        public string Root { get; set; }

        /// <summary>
        /// 0-uint.MaxValue
        /// </summary>
        public uint VolumeNr { get; set; }

        [Required]
        public string VolumeId { get; set; }

        [Required]
        public string Locator { get; set; }

        [Required]
        public string Secret { get; set; } // Used as encryptionKey

        public string Parameters { get; set; } // todo: deprecate
        

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        string DebuggerDisplay => $"{nameof(DriveConfig)} schema='{Schema}', root='{Root}',, volumeNr='{VolumeNr}, volumeId='{VolumeId} locator='{Locator}', secret='{Secret}''".ToString(CultureInfo.CurrentCulture);
    }
}