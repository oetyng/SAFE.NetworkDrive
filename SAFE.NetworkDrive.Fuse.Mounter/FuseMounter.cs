using SAFE.NetworkDrive.Mounter;
using SAFE.NetworkDrive.Mounter.Config;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SAFE.NetworkDrive.Fuse
{
    public class FuseMounter : IDriveMounter
    {
        readonly DriveConfig _config;
        ILogger _logger;
        FuseOperations _operations;

        public FuseMounter(DriveConfig config)
            => _config = config;

        public Task Mount(ICloudDrive drive, ILogger logger, CancellationTokenSource cancellation)
        {
            _logger = logger;
            _operations = new FuseOperations(drive, logger);

            var c = new Action(() =>
            {
                try
                {
                    _operations.Start();
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error mounting {_config.Root[0]}: {ex.Message + ex.StackTrace}");
                }
                finally { Unmount(); }
            });

            var runner = Task.Run(c, cancellation.Token);

            return runner;
        }

        public bool Unmount()
        {
            try
            {
                _operations.Stop();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error unmounting {_config.Root[0]}: {ex.Message + ex.StackTrace}");
                return false;
            }
        }
    }
}