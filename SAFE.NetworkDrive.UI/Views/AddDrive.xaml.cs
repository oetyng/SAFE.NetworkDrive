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

        public string Location { get; private set; }
        public string Secret { get; private set; }

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
            if (!ValidateInput())
                return;

            Location = locationBoxPassword.Password;
            Secret = secretBoxPassword.Password;
            DialogResult = true;
            Close();
        }

        void PwdBoxPassword_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                BtnAddDrive_Click(sender, e);
        }


        bool ValidateInput()
        {
            if (_minInputLength > locationBoxPassword.Password.Length)
            {
                MessageBox.Show($"Location must be at least {_minInputLength} chars.", "Invalid location", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return false;
            }
            if (_minInputLength > secretBoxPassword.Password.Length)
            {
                MessageBox.Show($"Secret must be at least {_minInputLength} chars.", "Invalid secret", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return false;
            }

            return true;
        }
    }
}