using System.Windows;
using System.Windows.Input;

namespace SAFE.NetworkDrive.UI
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        public string Username { get; private set; }
        public string Password { get; private set; }

        public Login()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (3 > txtUsername.Text.Length)
                return;
            if (4 > pwdBoxPassword.Password.Length)
                return;

            Username = txtUsername.Text;
            Password = pwdBoxPassword.Password;
            DialogResult = true;
            Close();
        }

        private void PwdBoxPassword_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                BtnLogin_Click(sender, e);
        }
    }
}