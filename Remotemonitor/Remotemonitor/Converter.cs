using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace Remotemonitor.Converters
{
    public class BoolToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isStale = value is bool b && b;
            return isStale ? "Keine Daten (> 30s)" : "OK";
        }

        public object ConvertBack(object value, Type target, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isStale = value is bool b && b;
            return new SolidColorBrush(isStale ? Colors.Red : Colors.Lime);
        }

        public object ConvertBack(object value, Type target, object parameter, CultureInfo culture) => throw new NotImplementedException();

    }

}