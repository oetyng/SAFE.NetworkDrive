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
        readonly Service _service;

        public MainWindow(string password)
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            
            this.Closing += new CancelEventHandler(MainWindow_Closing);

            var app = new ApplicationManagement
            {
                Explore = Explore,
                Exit = Exit,
                OpenDriveSettings = Show,
                ToggleMount = ToggleMountDrive,
                UnmountAll = UnmountAll
            };

            _service = new Service(password, app);

            LstViewDrives.ItemsSource = _service.Drives;
        }

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }

        void LstViewDrives_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => SetMountToggleEnabled();

        void BtnAddDrive_Click(object sender, RoutedEventArgs e)
        {
            var addDrive = new AddDrive(GetAvailableLetters());

            if (addDrive.ShowDialog() == true)
            {
                var driveLetter = (char)addDrive.CmbDriveLetters.SelectedItem;
                var config = _service.UserConfig.CreateDriveConfig(driveLetter);

                // store to config
                _service.UserConfig.AddDrive(config);
                // add mounter
                if (!_service.Mounter.AddDrive(config))
                {
                    MessageBox.Show("Drive letter already exists. Choose another letter.", "Error", MessageBoxButton.OK);
                    return;
                }

                _service.ShowDrive(driveLetter, config.VolumeNr);
                ToggleMountDrive(driveLetter);
            }
        }

        List<char> GetAvailableLetters()
        {
            var reservedInConfig = _service.Drives
                .Where(c => !c.Mounted)
                .Select(c => c.Letter);
            return DriveLetterUtil.GetAvailableDriveLetters(reservedInConfig);
        }

        void BtnEditDrive_Click(object sender, RoutedEventArgs e)
        {
            var drive = _service.Drives.Single(c => c.Letter == (LstViewDrives.SelectedItem as Drive).Letter);

            var edit = new EditDrive(drive.Letter, drive.Mounted, GetAvailableLetters());
            if (edit.ShowDialog() != true)
                return;
            if (edit.NewDriveLetter.HasValue)
            {
                var editResult = _service.UserConfig.TrySetDriveLetter(drive.Letter, edit.NewDriveLetter.Value);
                if (editResult.HasValue)
                {
                    var removedLetter = drive.Letter;
                    drive.Letter = edit.NewDriveLetter.Value;
                    drive.NotifyIcon.RemoveMenuItem();
                    drive.NotifyIcon = DriveNotifyIcon.Create(drive.Letter, _service.App);
                    _service.Drives.ResetBindings();
                    _service.Mounter.RemoveDrive(removedLetter);
                    _service.Mounter.AddDrive(editResult.Value);
                }
            }
        }

        void BtnToggleMountDrive_Click(object sender, RoutedEventArgs e)
            => ToggleMountDrive((LstViewDrives.SelectedItem as Drive).Letter);

        void BtnAdvancedOptions_Click(object sender, RoutedEventArgs e)
        {
            var options = new AdvancedOptions(_service, GetAvailableLetters());
            options.ShowDialog();
        }

        void BtnExitApp_Click(object sender, RoutedEventArgs e)
            => _service.App.Exit();

        void RunInThread(Action a)
        {
            var t = new System.Threading.Thread(new System.Threading.ThreadStart(() => a()));
            t.Start();
        }

        void Exit()
        {
            UnmountAll();
            Application.Current.Shutdown(-1);
        }

        void UnmountAll()
        {
            _service.Mounter.UnmountAll();
            foreach (var drive in _service.Drives)
                drive.Mounted = false;
            _service.Drives.ResetBindings();
        }

        void ToggleMountDrive(char driveLetter)
        {
            var drive = _service.Drives
                .Single(c => c.Letter == driveLetter);

            if (drive.Mounted) _service.Mounter.Unmount(driveLetter);
            else RunInThread(() => _service.Mounter.Mount(driveLetter));

            drive.Mounted = !drive.Mounted;
            _service.Drives.ResetBindings();
            SetMountToggleEnabled();
        }

        void SetMountToggleEnabled()
        {
            if (LstViewDrives.SelectedItem == null)
            {
                BtnToggleMountDrive.IsEnabled = false;
                BtnEditDrive.IsEnabled = false;
            }
            else
            {
                BtnToggleMountDrive.IsEnabled = true;
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
        public uint VolumeNr { get; set; }
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