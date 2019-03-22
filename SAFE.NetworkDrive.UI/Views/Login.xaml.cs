using System.Windows;
using System.Windows.Input;

namespace SAFE.NetworkDrive.UI
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        const int _minInputLength = 5;

        public string Username { get; private set; }
        public string Password { get; private set; }

        public Login()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            txtUsername.Focus();
        }

        void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            Username = txtUsername.Text;
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
            if (_minInputLength > txtUsername.Text.Length)
            {
                MessageBox.Show($"Username must be at least {_minInputLength} chars.", "Invalid username", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return false;
            }
            if (_minInputLength > pwdBoxPassword.Password.Length)
            {
                MessageBox.Show($"Password must be at least {_minInputLength} chars.", "Invalid password", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return false;
            }

            return true;
        }
    }
}