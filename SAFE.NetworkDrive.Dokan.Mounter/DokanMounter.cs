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

        public DokanMounter(DriveConfig config)
            => _config = config;

        public Task Mount(ICloudDrive drive, ILogger logger, CancellationTokenSource cancellation)
        {
            var operations = new CloudOperations(drive, logger);

            // HACK: handle non-unique parameter set of DokanOperations.Mount() by explicitely specifying AllocationUnitSize and SectorSize
            var runner = Task.Run(() => operations.Mount(_config.Root,
                DokanOptions.NetworkDrive | DokanOptions.MountManager | DokanOptions.CurrentSession,
                threadCount: 5,
                121,
                TimeSpan.FromSeconds(_config.Timeout != 0 ? _config.Timeout : 20),
                null, 512, 512),
                cancellation.Token);

            return runner;
        }

        public bool Unmount()
            => Dokan.Unmount(_config.Root[0]);
    }
}