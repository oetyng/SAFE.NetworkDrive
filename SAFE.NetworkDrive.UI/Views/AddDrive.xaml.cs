using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SAFE.NetworkDrive.UI
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class AddDrive : Window
    {
        public string Location { get; private set; }
        public string Secret { get; private set; }

        readonly BindingList<char> _availableLetters;

        public AddDrive()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            _availableLetters = new BindingList<char>(DriveLetterUtility.GetAvailableDriveLetters());
            CmbDriveLetters.ItemsSource = _availableLetters;
            CmbDriveLetters.SelectedIndex = 0;
        }

        void BtnAddDrive_Click(object sender, RoutedEventArgs e)
        {
            if (5 > locationBoxPassword.Password.Length)
                return;
            if (5 > secretBoxPassword.Password.Length)
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
    }
}
