using System.Threading;

namespace SAFE.NetworkDrive.Parameters
{
    public class SAFEDriveParameters
    {
        public string Locator { get; set; }
        public string Secret { get; set; }
        public ILogger Logger { get; set; }
        public CancellationToken Cancellation { get; set; }
        public bool Live { get; set; }
    }
}
