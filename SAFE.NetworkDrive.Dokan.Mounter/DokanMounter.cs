using DokanNet;
using SAFE.NetworkDrive.Mounter;
using SAFE.NetworkDrive.Mounter.Config;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SAFE.NetworkDrive
{
    public class DokanMounter : IDriveMounter
    {
        readonly DriveConfig _config;
        Action _driveDisposal = () => { };

        public DokanMounter(DriveConfig config)
            => _config = config;

        public Task Mount(ISAFEDrive drive, ILogger logger, CancellationTokenSource cancellation)
        {
            var operations = new SAFEFSOperations(drive, logger);
            _driveDisposal = drive.Dispose;

            // HACK: handle non-unique parameter set of DokanOperations.Mount() by explicitely specifying AllocationUnitSize and SectorSize
            var runner = Task.Run(() => operations.Mount(_config.Root,
                DokanOptions.NetworkDrive | DokanOptions.MountManager | DokanOptions.CurrentSession,
                threadCount: 5,
                121,
                TimeSpan.FromSeconds(20),
                null, 512, 512),
                cancellation.Token);

            return runner;
        }

        public bool Unmount()
        {
            if (Dokan.Unmount(_config.Root[0]))
            {
                _driveDisposal();
                return true;
            }
            else return false;
        }
    }
}