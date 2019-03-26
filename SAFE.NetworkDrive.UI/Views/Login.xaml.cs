using System.Windows;
using System.Windows.Input;

namespace SAFE.NetworkDrive.UI
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        const int _minInputLength = 8;

        public string Password { get; private set; }

        public Login()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            pwdBoxPassword.Focus();
        }

        void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            Password = pwdBoxPassword.Password;
            DialogResult = true;
            Close();
        }

        void PwdBoxPassword_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                BtnLogin_Click(sender, e);
        }

        bool ValidateInput()
        {
            if (_minInputLength > pwdBoxPassword.Password.Length)
            {
                MessageBox.Show($"Password must be at least {_minInputLength} chars.", "Invalid password", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return false;
            }

            return true;
        }
    }
}