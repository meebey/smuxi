using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Smuxi.Frontend.Wpf
{
    public class HeightToCornerRadiusConverter
        :IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double height = (double)value;
            return new CornerRadius(height / 2);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}