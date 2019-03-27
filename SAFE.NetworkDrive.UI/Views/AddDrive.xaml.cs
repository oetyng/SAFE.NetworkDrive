using SAFE.NetworkDrive.Mounter.Config;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace SAFE.NetworkDrive.UI
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class AddDrive : Window
    {
        readonly BindingList<char> _availableLetters;

        public AddDrive(IEnumerable<char> reservedInConfig)
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            _availableLetters = new BindingList<char>(DriveLetterUtil.GetAvailableDriveLetters(reservedInConfig));
            CmbDriveLetters.ItemsSource = _availableLetters;
            CmbDriveLetters.SelectedIndex = 0;
        }

        void BtnAddDrive_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}