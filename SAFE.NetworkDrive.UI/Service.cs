using SAFE.NetworkDrive.Mounter;
using SAFE.NetworkDrive.Mounter.Config;
using System.ComponentModel;

namespace SAFE.NetworkDrive.UI
{
    internal class Service
    {
        public UserConfigHandler UserConfig { get; }
        public BindingList<Drive> Drives { get; } = new BindingList<Drive>();
        public ApplicationManagement App { get; }
        public DriveManager Mounter { get; }

        public Service(string password, ApplicationManagement app)
        {
            App = app;

            var logger = Utils.LogFactory.GetLogger("logger");

            UserConfig = new UserConfigHandler(password);
            var user = UserConfig.CreateOrDecrypUserConfig();
            Mounter = new DriveManager((c) => new DokanMounter(c), user, logger);
            user.Drives.ForEach(c => ShowDrive(c.Root[0], c.VolumeNr));
        }

        internal void ShowDrive(char driveLetter, uint volumeNr)
        {
            var drive = new Drive { Letter = driveLetter, VolumeNr = volumeNr };
            Drives.Add(drive);
            drive.NotifyIcon = DriveNotifyIcon.Create(driveLetter, App);
        }
    }
}