using System.Threading;
using System.Threading.Tasks;

namespace SAFE.NetworkDrive.Mounter
{
    public interface IDriveMounter
    {
        Task Mount(ISAFEDrive drive, ILogger logger, CancellationTokenSource cancellation);
        bool Unmount();
    }
}