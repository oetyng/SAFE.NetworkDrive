using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SAFE.NetworkDrive.Mounter.Config;

namespace SAFE.NetworkDrive.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly UserConfigHandler _userConfig;
        readonly BindingList<Drive> _drives = new BindingList<Drive>();
        readonly ApplicationManagement _app;
        readonly Mounter.DriveManager _mounter;

        public MainWindow(string password)
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            LstViewDrives.ItemsSource = _drives;
            this.Closing += new CancelEventHandler(MainWindow_Closing);
            _app = new ApplicationManagement
            {
                Explore = Explore,
                Exit = Exit,
                OpenDriveSettings = Show,
                ToggleMount = ToggleMountDrive,
                UnmountAll = UnmountAll
            };

            var logger = Utils.LogFactory.GetLogger("logger");

            _userConfig = new UserConfigHandler(password);
            var user = _userConfig.CreateOrDecrypUserConfig();
            _mounter = new Mounter.DriveManager((c) => new DokanMounter(c), user, logger);
            user.Drives.ForEach(c => ShowDrive(c.Root[0]));
        }

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }

        void LstViewDrives_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => SetMountToggleEnabled();

        void BtnToggleMountDrive_Click(object sender, RoutedEventArgs e)
            => ToggleMountDrive((LstViewDrives.SelectedItem as Drive).Letter);

        void BtnAddDrive_Click(object sender, RoutedEventArgs e)
        {
            var addDrive = new AddDrive(GetAvailableLetters());

            if (addDrive.ShowDialog() == true)
            {
                var driveLetter = (char)addDrive.CmbDriveLetters.SelectedItem;
                var config = _userConfig.CreateDriveConfig(driveLetter);

                // store to config
                _userConfig.AddDrive(config);
                // add mounter
                if (!_mounter.AddDrive(config))
                {
                    MessageBox.Show("Drive letter already exists. Choose another letter.", "Error", MessageBoxButton.OK);
                    return;
                }

                ShowDrive(driveLetter);
                ToggleMountDrive(driveLetter);
            }
        }

        List<char> GetAvailableLetters()
        {
            var reservedInConfig = _drives
                .Where(c => !c.Mounted)
                .Select(c => c.Letter);
            return DriveLetterUtil.GetAvailableDriveLetters(reservedInConfig);
        }

        void BtnEditDrive_Click(object sender, RoutedEventArgs e)
        {
            var drive = _drives.Single(c => c.Letter == (LstViewDrives.SelectedItem as Drive).Letter);
            void removal()
            {
                _drives.Remove(drive);
                drive.NotifyIcon.RemoveMenuItem();
                _mounter.RemoveDrive(drive.Letter);
                _userConfig.RemoveDrive(drive.Letter);
            }
            var edit = new EditDrive(drive.Letter, drive.Mounted, removal, GetAvailableLetters());
            if (edit.ShowDialog() != true)
                return;
            if (edit.NewDriveLetter.HasValue)
            {
                var editResult = _userConfig.TrySetDriveLetter(drive.Letter, edit.NewDriveLetter.Value);
                if (editResult.HasValue)
                {
                    drive.Letter = edit.NewDriveLetter.Value;
                    drive.NotifyIcon.RemoveMenuItem();
                    drive.NotifyIcon = DriveNotifyIcon.Create(drive.Letter, _app);
                    _drives.ResetBindings();
                    _mounter.RemoveDrive(drive.Letter);
                    _mounter.AddDrive(editResult.Value);
                }
            }
        }

        void BtnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("This will remove your user and all local drive logins. " +
                "If you don't remember your network login credentials, you will never be " +
                "able to access your data on these drives again.", "Delete user.", MessageBoxButton.OKCancel);
            if (res == MessageBoxResult.OK)
            {
                _mounter.UnmountAll(); // unmount and clean up dbs
                foreach (var drive in _drives)
                    drive.NotifyIcon.RemoveMenuItem();
                _drives.Clear();
                _userConfig.DeleteUser(); // remove encrypted file
                _app.Exit(); // exit application
            }
        }

        void BtnExitApp_Click(object sender, RoutedEventArgs e)
            => _app.Exit();

        void RunInThread(Action a)
        {
            var t = new System.Threading.Thread(new System.Threading.ThreadStart(() => a()));
            t.Start();
        }

        void ShowDrive(char driveLetter)
        {
            var drive = new Drive { Letter = driveLetter };
            _drives.Add(drive);
            drive.NotifyIcon = DriveNotifyIcon.Create(driveLetter, _app);
        }

        void Exit()
        {
            UnmountAll();
            Application.Current.Shutdown(-1);
        }

        void UnmountAll()
        {
            _mounter.UnmountAll();
            foreach (var drive in _drives)
                drive.Mounted = false;
            _drives.ResetBindings();
        }

        void ToggleMountDrive(char driveLetter)
        {
            var drive = _drives
                .Single(c => c.Letter == driveLetter);

            if (drive.Mounted) _mounter.Unmount(driveLetter);
            else RunInThread(() => _mounter.Mount(driveLetter));

            drive.Mounted = !drive.Mounted;
            _drives.ResetBindings();
            SetMountToggleEnabled();
        }

        void SetMountToggleEnabled()
        {
            if (LstViewDrives.SelectedItem == null)
            {
                BtnMountDrive.IsEnabled = false;
                BtnEditDrive.IsEnabled = false;
            }
            else
            {
                BtnMountDrive.IsEnabled = true;
                BtnEditDrive.IsEnabled = (LstViewDrives.SelectedItem as Drive).Mounted == false; // temporary: as long as changing label is the only edit option, we better not enable edit at all if mounted
            }
        }

        void LstViewDrives_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (LstViewDrives.SelectedItem == null)
                return;
            var drive = LstViewDrives.SelectedItem as Drive;
            if (!drive.Mounted)
            {
                ToggleMountDrive(drive.Letter);
                var driveInfo = new System.IO.DriveInfo(drive.Letter.ToString());
                while (!driveInfo.IsReady)
                    System.Threading.Thread.Sleep(10);
            }
            Explore(drive.Letter);
        }

        void Explore(char driveLetter)
        {
            // See http://support.microsoft.com/kb/152457 for a list of command-line args
            // that are supported by Windows Explorer.
            var explorerArgs = $"/e,{driveLetter}:\\";
            System.Diagnostics.Process.Start("explorer.exe", explorerArgs);
        }
    }

    class Drive
    {
        public char Letter { get; set; }
        public bool Mounted { get; set; }
        public MountEnum MountStatus => Mounted ? MountEnum.Mounted : MountEnum.Unmounted;
        public DriveNotifyIcon NotifyIcon { get; set; }
    }

    public enum MountEnum
    {
        Unmounted = 0,
        Mounted = 1
    }
}