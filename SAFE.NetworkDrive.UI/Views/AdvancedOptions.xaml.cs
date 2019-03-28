using SAFE.NetworkDrive.Mounter.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SAFE.NetworkDrive.UI
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class AdvancedOptions : Window
    {
        readonly Service _service;
        readonly List<char> _availableLetters;

        internal AdvancedOptions(Service service, List<char> availableLetters)
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            _service = service;
            _availableLetters = availableLetters;
            CmbDriveLetters.ItemsSource = _service.Drives.Select(c => c.Letter).ToList();
            CmbDriveLetters.SelectedIndex = 0;
            BtnDeleteDrive.IsEnabled = _service.Drives.Count > 0;
            BtnRestoreDrive.IsEnabled = _service.UserConfig.CreateOrDecrypUserConfig().VolumeNrCheckpoint > _service.Drives.Count;
            NUDTextBox.Text = _startValue.ToString();
        }

        void BtnDeleteDrive_Click(object sender, RoutedEventArgs e)
        {
            var drive = _service.Drives.Single(c => c.Letter == ((char)CmbDriveLetters.SelectedItem));

            var res = MessageBox.Show("This will remove your local drive login. " +
                "You can add it again later.", $"Remove drive {drive.Letter}", MessageBoxButton.OKCancel);
            if (res == MessageBoxResult.OK)
            {
                _service.Drives.Remove(drive);
                drive.NotifyIcon.RemoveMenuItem();
                _service.Mounter.RemoveDrive(drive.Letter);
                _service.UserConfig.RemoveDrive(drive.Letter);
                Close();
            }
        }

        void BtnEnterPassword_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("This will remove your user and all local drive logins. " +
                   "If you don't remember your password, you will never be " +
                   "able to access your data on these drives again.", "Delete user.", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (res != MessageBoxResult.OK)
                return;

            BtnEnterPassword.Visibility = Visibility.Hidden;
            GridPassword.Visibility = Visibility.Visible;
        }

        void BtnDeleteAll_Click(object sender, RoutedEventArgs e)
        {
            var user = new UserConfigHandler(PasswordBox.Password);
            if (!user.Equals(_service.UserConfig))
            {
                MessageBox.Show("Could not delete user: Wrong password.", "Wrong password.", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _service.Mounter.UnmountAll(); // unmount and clean up dbs
            foreach (var drive in _service.Drives)
                drive.NotifyIcon.RemoveMenuItem();
            _service.Drives.Clear();
            _service.UserConfig.DeleteUser(); // remove encrypted file
            _service.App.Exit(); // exit application
        }

        void BtnRestoreDrive_Click(object sender, RoutedEventArgs e)
        {

        }
    }

    public partial class AdvancedOptions
    {
        readonly int _minValue = 0;
        readonly int _maxValue = 100;
        readonly int _startValue = 10;

        void NUDButtonUP_Click(object sender, RoutedEventArgs e)
        {
            int number;
            if (NUDTextBox.Text != "") number = Convert.ToInt32(NUDTextBox.Text);
            else number = 0;
            if (number < _maxValue)
                NUDTextBox.Text = Convert.ToString(number + 1);
        }

        void NUDButtonDown_Click(object sender, RoutedEventArgs e)
        {
            int number;
            if (NUDTextBox.Text != "") number = Convert.ToInt32(NUDTextBox.Text);
            else number = 0;
            if (number > _minValue)
                NUDTextBox.Text = Convert.ToString(number - 1);
        }

        void NUDTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                NUDButtonUP.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(NUDButtonUP, new object[] { true });
            }

            if (e.Key == Key.Down)
            {
                NUDButtonDown.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(NUDButtonDown, new object[] { true });
            }
        }

        void NUDTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(NUDButtonUP, new object[] { false });

            if (e.Key == Key.Down)
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(NUDButtonDown, new object[] { false });
        }

        void NUDTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int number = 0;
            if (NUDTextBox.Text != "")
                if (!int.TryParse(NUDTextBox.Text, out number)) NUDTextBox.Text = _startValue.ToString();
            if (number > _maxValue) NUDTextBox.Text = _maxValue.ToString();
            if (number < _minValue) NUDTextBox.Text = _minValue.ToString();
            NUDTextBox.SelectionStart = NUDTextBox.Text.Length;
        }
    }
}