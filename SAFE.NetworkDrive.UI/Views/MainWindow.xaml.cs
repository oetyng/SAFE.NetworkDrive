﻿using System;
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
        readonly Mounter.DriveMountManager _mounter;

        public MainWindow(string username, string password)
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            LstViewDrives.ItemsSource = _drives;
            this.Closing += new CancelEventHandler(MainWindow_Closing);
            _app = new ApplicationManagement
            {
                Exit = () => Application.Current.Shutdown(-1),
                OpenDriveSettings = Show,
                ToggleMount = ToggleMountDrive,
                UnmountAll = UnmountAll
            };

            var logger = Utils.LogFactory.GetLogger("logger");

            _userConfig = new UserConfigHandler(username, password);
            var user = _userConfig.CreateOrDecrypUserConfig();
            _mounter = new Mounter.DriveMountManager(user, logger);
            user.Drives.ForEach(c => ShowDrive(c.Root[0]));
        }

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }

        void LstViewDrives_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ToggleButtons();
            if (LstViewDrives.SelectedItem == null)
                return;
        }

        void BtnToggleMountDrive_Click(object sender, RoutedEventArgs e)
            => ToggleMountDrive((LstViewDrives.SelectedItem as Drive).Letter);

        void BtnAddDrive_Click(object sender, RoutedEventArgs e)
        {
            var addDrive = new AddDrive();

            if (addDrive.ShowDialog() == true)
            {
                var driveLetter = (char)addDrive.CmbDriveLetters.SelectedItem;
                var location = addDrive.Location;
                var secret = addDrive.Secret;

                var config = _userConfig.CreateDriveConfig(driveLetter, location, secret);

                // store to config
                _userConfig.AddDrive(config);
                // add mounter
                _mounter.AddDrive(config);

                ShowDrive(driveLetter);
            }
            else
                MessageBox.Show("Unable to load data.", "Error", MessageBoxButton.OK);
        }

        void BtnRemoveDrive_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("This will remove your local drive login. " +
                "If you don't remember your network login credentials, you will never be " +
                "able to access your data on this drive again.", "Remove drive.", MessageBoxButton.OKCancel);
            if (res == MessageBoxResult.OK)
            {
                var drive = _drives.Single(c => c.Letter == (LstViewDrives.SelectedItem as Drive).Letter);
                _drives.Remove(drive);
                drive.NotifyIcon.RemoveMenuItem();
                if (drive.Mounted) _mounter.Unmount(drive.Letter);
                _userConfig.RemoveDrive(drive.Letter);
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

        void BtnUnmountAll_Click(object sender, RoutedEventArgs e)
            => UnmountAll();

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

        void UnmountAll()
        {
            _mounter.UnmountAll();
            foreach (var drive in _drives)
                drive.Mounted = false;
            _drives.ResetBindings();
            ToggleUnmountAllEnabled();
        }

        void ToggleMountDrive(char driveLetter)
        {
            var drive = _drives
                .Single(c => c.Letter == driveLetter);

            if (drive.Mounted) _mounter.Unmount(driveLetter);
            else RunInThread(() => _mounter.Mount(driveLetter));

            drive.Mounted = !drive.Mounted;
            _drives.ResetBindings();
            ToggleUnmountAllEnabled();
        }

        void ToggleButtons()
        {
            if (LstViewDrives.SelectedItem == null)
            {
                BtnMountDrive.IsEnabled = false;
                BtnRemoveDrive.IsEnabled = false;
            }
            else
            {
                BtnMountDrive.IsEnabled = true;
                BtnRemoveDrive.IsEnabled = true;
            }
        }

        void ToggleUnmountAllEnabled()
        {
            if (_drives.Any(c => c.Mounted))
                BtnUnmountAll.IsEnabled = true;
            else
                BtnUnmountAll.IsEnabled = false;
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