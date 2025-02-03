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
        public class LoginStatusToColorConverter : IValueConverter
        {
                public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
                {
                        bool isSuccessful = value is bool boolean ? boolean : false;
                        if (isSuccessful)
                                return new SolidColorBrush(Color.FromRgb(40, 167, 69));  // Success green
                        else
                                return new SolidColorBrush(Color.FromRgb(220, 53, 69));  // Failed red
                }

                public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
                {
                        throw new NotImplementedException();
                }
        }
}
