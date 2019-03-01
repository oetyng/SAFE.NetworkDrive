using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace SAFE.NetworkDrive.Mounter.Config
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class DriveConfig
    {
        [Required]
        public string Schema { get; set; }

        [Required]
        public string Root { get; set; }

        [Required]
        public string Locator { get; set; }

        [Required]
        public string Secret { get; set; } // EncryptionKey

        public string Parameters { get; set; }

        public int Timeout { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        string DebuggerDisplay => $"{nameof(DriveConfig)} schema='{Schema}', root='{Root}', locator='{Locator}', secret='{Secret}', timeout='{Timeout}'".ToString(CultureInfo.CurrentCulture);
    }
}