using SAFE.NetworkDrive.Mounter.Config;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace SAFE.NetworkDrive.UI
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class AddDrive : Window
    {
        const int _minInputLength = 5;

        readonly BindingList<char> _availableLetters;

        public AddDrive()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            _availableLetters = new BindingList<char>(DriveLetterUtil.GetAvailableDriveLetters());
            CmbDriveLetters.ItemsSource = _availableLetters;
            CmbDriveLetters.SelectedIndex = 0;
        }

        void BtnAddDrive_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        void PwdBoxPassword_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                BtnAddDrive_Click(sender, e);
        }
    }
}