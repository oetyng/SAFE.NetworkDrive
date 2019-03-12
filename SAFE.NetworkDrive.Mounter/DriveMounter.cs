using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DokanNet;
using NLog;
using SAFE.NetworkDrive.Mounter.Config;
using SAFE.NetworkDrive.Parameters;

namespace SAFE.NetworkDrive.Mounter
{
    public class DriveMountManager
    {
        readonly ConcurrentDictionary<char, DriveMounter> _drives;
        readonly UserConfig _user;
        readonly ILogger _logger;

        public DriveMountManager(UserConfig user, ILogger logger)
        {
            _user = user;
            _logger = logger;
            var mounters = _user.Drives
                    .ToDictionary(c => c.Root[0],
                        c => new DriveMounter(_user.Username, c, _logger));
            _drives = new ConcurrentDictionary<char, DriveMounter>(mounters);
        }

        public void AddDrive(DriveConfig drive)
        {
            if (!_drives.TryAdd(drive.Root[0], new DriveMounter(_user.Username, drive, _logger)))
                throw new InvalidOperationException($"Drive already exists: {drive.Root}");
        }

        public void MountAll()
        {
            foreach (var drive in _drives.Values)
                drive.Mount();
        }

        public void UnmountAll()
        {
            foreach (var drive in _drives.Values)
                drive.Unmount();
        }

        public void Mount(char driveLetter)
        {
            if (!_drives.ContainsKey(driveLetter))
                return;
            _drives[driveLetter].Mount();
        }

        public void Unmount(char driveLetter)
        {
            if (!_drives.ContainsKey(driveLetter))
                return;
            _drives[driveLetter].Unmount();
        }
    }

    public class DriveMounter
    {
        readonly string _username;
        readonly DriveConfig _config;
        readonly ILogger _logger;
        MountSession _session;

        public bool Mounted => _session != null && _session.Mounted;

        public DriveMounter(string username, DriveConfig driveConfig, ILogger logger)
        {
            _username = username;
            _config = driveConfig;
            _logger = logger;
        }

        public bool Mount()
        {
            if (_session != null)
                return Mounted;
            try
            {
                _session = MountDrive();
                return true;
            }
            catch (Exception ex)
            {
                _session = null;
                _logger.Error($"{ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        public void Unmount()
        {
            if (_session == null)
                return;
            
            try
            {
                _session.Unmount();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"{ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                _session = null;
            }
        }

        MountSession MountDrive()
        {
            var cancellation = new CancellationTokenSource();
            var factory = new CloudDriveFactory();
            var drive = factory.CreateCloudDrive(
                _config.Schema,
                _username,
                _config.Root,
                new CloudDriveParameters()
                {
                    ApiKey = _config.Locator,
                    EncryptionKey = _config.Secret,
                    Logger = _logger,
                    Cancellation = cancellation.Token,
                    Parameters = _config.GetParameters()
                }
            );

            if (!drive.TryAuthenticate())
            {
                var displayRoot = drive.DisplayRoot;
                drive.Dispose();
                _logger.Warn($"Authentication failed for drive '{displayRoot}'");
            }

            var operations = new CloudOperations(drive, _logger);

            // HACK: handle non-unique parameter set of DokanOperations.Mount() by explicitely specifying AllocationUnitSize and SectorSize
            var runner = Task.Run(() => operations.Mount(_config.Root,
                DokanOptions.NetworkDrive | DokanOptions.MountManager | DokanOptions.CurrentSession,
                threadCount: 5,
                121,
                TimeSpan.FromSeconds(_config.Timeout != 0 ? _config.Timeout : 20),
                null, 512, 512),
                cancellation.Token);

            var session = new MountSession(_config.Root[0], runner, cancellation);

            var driveInfo = new DriveInfo(_config.Root);
            while (!driveInfo.IsReady)
                Thread.Sleep(10);
            _logger.Info($"Drive '{drive.DisplayRoot}' mounted successfully.");

            return session;
        }
    }

    public class MountSession
    {
        readonly Task _runner;
        readonly CancellationTokenSource _cancellation;

        public char DriveLetter { get; }
        public bool Mounted { get; private set; }

        public MountSession(char driveLetter, Task runner, CancellationTokenSource cancellation)
        {
            DriveLetter = driveLetter;
            _runner = runner;
            _cancellation = cancellation;
        }

        public void Unmount()
        {
            _cancellation.Cancel();
            Dokan.Unmount(DriveLetter);
            Mounted = false;
        }
    }
}