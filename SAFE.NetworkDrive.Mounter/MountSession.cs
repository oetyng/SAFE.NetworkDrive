using System;
using System.Threading;
using System.Threading.Tasks;

namespace SAFE.NetworkDrive.Mounter
{
    public class MountSession
    {
        readonly Task _runner;
        readonly Action<char> _unmount;
        readonly CancellationTokenSource _cancellation;

        public char DriveLetter { get; }
        public bool Mounted { get; private set; }

        public MountSession(char driveLetter, Task runner, Action<char> unmount, CancellationTokenSource cancellation)
        {
            DriveLetter = driveLetter;
            _runner = runner;
            _unmount = unmount;
            _cancellation = cancellation;
        }

        public void Unmount()
        {
            _cancellation.Cancel();
            _unmount(DriveLetter);
            Mounted = false;
        }
    }
}