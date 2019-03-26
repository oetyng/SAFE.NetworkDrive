using SAFE.NetworkDrive.Mounter.Config;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace SAFE.NetworkDrive.Mounter
{
    public class DriveManager
    {
        readonly ConcurrentDictionary<char, MountSessionManager> _drives;
        readonly Func<DriveConfig, IDriveMounter> _mounter;
        readonly UserConfig _user;
        readonly ILogger _logger;

        public DriveManager(Func<DriveConfig, IDriveMounter> mounter, UserConfig user, ILogger logger)
        {
            _user = user;
            _logger = logger;
            _mounter = mounter;
            var mounters = _user.Drives
                    .ToDictionary(c => c.Root[0],
                        c => new MountSessionManager(mounter, c, _logger));
            _drives = new ConcurrentDictionary<char, MountSessionManager>(mounters);
        }

        public bool AddDrive(DriveConfig drive)
        {
            if (!_drives.TryAdd(drive.Root[0], new MountSessionManager(_mounter, drive, _logger)))
                return false;
            return true;
        }

        public void RemoveDrive(char driveLetter)
        {
            Unmount(driveLetter);
            _drives.TryRemove(driveLetter, out _);
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
}