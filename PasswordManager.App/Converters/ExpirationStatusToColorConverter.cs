using PasswordManager.Core.Models;
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
        public class ExpirationStatusToColorConverter : IValueConverter
        {
                public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
                {
                        if (value is StoredPasswordModel password)
                        {
                                if (password.IsExpired)
                                        return new SolidColorBrush(Colors.Red);  // Expired

                                if (password.IsExpiringSoon)
                                        return new SolidColorBrush(Colors.Orange);  // Expiring soon
                        }

                        return new SolidColorBrush(Colors.Black);  // Default
                }

                public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
                {
                        throw new NotImplementedException();
                }
        }
}
