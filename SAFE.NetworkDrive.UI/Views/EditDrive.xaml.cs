using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace SAFE.NetworkDrive.UI
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class EditDrive : Window
    {
        readonly Action _removal;
        readonly BindingList<char> _availableLetters;

        public char? NewDriveLetter { get; set; }

        public EditDrive(bool mounted, Action removal, List<char> availableLetters)
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            _removal = removal;

            if (mounted)
                CmbDriveLetters.IsEnabled = false;
            else
            {
                _availableLetters = new BindingList<char>(availableLetters);
                CmbDriveLetters.ItemsSource = _availableLetters;
            }
        }

        void CmbDriveLetters_SelectionChanged(object sender, RoutedEventArgs e)
        {
            NewDriveLetter = (char)CmbDriveLetters.SelectedItem;
        }

        void BtnRemoveDrive_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("This will remove your local drive login. " +
                "If you don't remember your network login credentials, you will never be " +
                "able to access your data on this drive again.", "Remove drive.", MessageBoxButton.OKCancel);
            if (res == MessageBoxResult.OK)
            {
                _removal();
                Close();
            }
        }

        void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}