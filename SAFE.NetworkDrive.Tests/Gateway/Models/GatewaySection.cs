
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using SAFE.NetworkDrive.Gateways;
using SAFE.NetworkDrive.Interface;

namespace SAFE.NetworkDrive.Tests.Gateway.Config
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public class GatewaySection
    {
        public static string Name => "gateways";

        public string Schema { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        
        public GatewayType Type { get; set; }

        public string VolumeId { get; set; }

        [Required]
        public string Mount { get; set; }

        public string ApiKey { get; set; }

        public string Parameters { get; set; }

        public GatewayCapabilities Exclusions { get; set; }

        public int MaxFileSize { get; set; }

        public string TestDirectory { get; set; } = "FileSystemTests";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        string DebuggerDisplay() => $"{nameof(GatewaySection)} schema='{Schema}', userName='{VolumeId}', mount='{Mount}', apiKey='{ApiKey}', parameters=[{Parameters?.Length ?? 0}], exclusions='{Exclusions}', testDirectory='{TestDirectory}'".ToString(CultureInfo.CurrentCulture);
    }
}
