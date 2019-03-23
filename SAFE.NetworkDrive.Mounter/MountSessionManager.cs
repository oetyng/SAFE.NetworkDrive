﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SAFE.NetworkDrive.Mounter.Config;
using SAFE.NetworkDrive.Parameters;

namespace SAFE.NetworkDrive.Mounter
{
    public class MountSessionManager
    {
        readonly Func<DriveConfig, IDriveMounter> _mounter;
        readonly string _username;
        readonly DriveConfig _config;
        readonly ILogger _logger;
        MountSession _session;

        public bool Mounted => _session != null && _session.Mounted;

        public MountSessionManager(Func<DriveConfig, IDriveMounter> mounter, string username, DriveConfig driveConfig, ILogger logger)
        {
            _mounter = mounter;
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
                _logger.Error($"{ex.GetType().Name}: {ex.Message}");
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

            var runner = Task.Run(() => _mounter(_config).Mount(drive, _logger, cancellation));

            var session = new MountSession(_config.Root[0], runner, () => _mounter(_config).Unmount(), cancellation);

            var driveInfo = new DriveInfo(_config.Root);
            while (!driveInfo.IsReady)
                Thread.Sleep(10);
            _logger.Info($"Drive '{drive.DisplayRoot}' mounted successfully.");

            return session;
        }
    }
}