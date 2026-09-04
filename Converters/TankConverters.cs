using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using TwoD5DMonitoring.Models;

namespace TwoD5DMonitoring.Converters
{
    public class BoolToSensorColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool active)
                return active ? Brushes.LimeGreen : Brushes.Gray;
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}