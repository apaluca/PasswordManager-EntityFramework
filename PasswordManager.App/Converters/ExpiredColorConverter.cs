using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace PasswordManager.App.Converters
{
        public class ExpiredColorConverter : IValueConverter
        {
                public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
                {
                        if (value is bool expired && expired)
                                return new SolidColorBrush(Color.FromRgb(220, 53, 69)); // #DC3545
                        return new SolidColorBrush(Color.FromRgb(102, 102, 102)); // #666666
                }

                public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
                {
                        throw new NotImplementedException();
                }
        }
}
