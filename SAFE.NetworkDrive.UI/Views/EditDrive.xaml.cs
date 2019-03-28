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
        readonly char _driveLetter;
        readonly BindingList<char> _availableLetters;

        public char? NewDriveLetter { get; set; }

        public EditDrive(char driveLetter, bool mounted, List<char> availableLetters)
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Title = $"Edit drive {driveLetter}";
            _driveLetter = driveLetter;

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

        void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}