using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SAFE.NetworkDrive.UI
{
    class ValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
                return !(bool)value;
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
                return !(bool)value;
            return value;
        }
    }

    public class TextInputToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Always test MultiValueConverter inputs for non-null
            // (to avoid crash bugs for views in the designer)
            if (values[0] is bool && values[1] is bool)
            {
                bool hasText = !(bool)values[0];
                bool hasFocus = (bool)values[1];

                if (hasFocus || hasText)
                    return Visibility.Collapsed;
            }

            return Visibility.Visible;
        }


        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PasswordBoxMonitor : DependencyObject
    {
        public static bool GetIsMonitoring(DependencyObject obj)
            => (bool)obj.GetValue(IsMonitoringProperty);

        public static void SetIsMonitoring(DependencyObject obj, bool value)
            => obj.SetValue(IsMonitoringProperty, value);

        public static readonly DependencyProperty IsMonitoringProperty =
            DependencyProperty.RegisterAttached("IsMonitoring", typeof(bool), typeof(PasswordBoxMonitor), new UIPropertyMetadata(false, OnIsMonitoringChanged));


        public static int GetPasswordLength(DependencyObject obj)
            => (int)obj.GetValue(PasswordLengthProperty);

        public static void SetPasswordLength(DependencyObject obj, int value)
            => obj.SetValue(PasswordLengthProperty, value);

        public static readonly DependencyProperty PasswordLengthProperty =
            DependencyProperty.RegisterAttached("PasswordLength", typeof(int), typeof(PasswordBoxMonitor), new UIPropertyMetadata(0));

        private static void OnIsMonitoringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is PasswordBox pb))
                return;
            if ((bool)e.NewValue)
                pb.PasswordChanged += PasswordChanged;
            else
                pb.PasswordChanged -= PasswordChanged;
        }

        static void PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!(sender is PasswordBox pb))
                return;
            SetPasswordLength(pb, pb.Password.Length);
        }
    }

    //class BorderVisibilitySetter : IValueConverter
    //{

    //    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {
    //        //check if the control's content property is null or empty        
    //        if (value == null || value.ToString() == string.Empty)
    //            return Visibility.Collapsed;
    //        else
    //            return Visibility.Visible;
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}