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
        public class StatusToColorConverter : IValueConverter
        {
                public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
                {
                        string status = value as string;
                        if (status != null)
                        {
                                switch (status.ToLower())
                                {
                                        case "good":
                                                return new SolidColorBrush(Color.FromRgb(232, 245, 233)); // Light green
                                        case "warning":
                                                return new SolidColorBrush(Color.FromRgb(255, 243, 224)); // Light orange
                                        case "critical":
                                                return new SolidColorBrush(Color.FromRgb(255, 235, 238)); // Light red
                                        case "info":
                                                return new SolidColorBrush(Color.FromRgb(227, 242, 253)); // Light blue
                                        default:
                                                return new SolidColorBrush(Color.FromRgb(245, 245, 245)); // Light gray
                                }
                        }
                        return new SolidColorBrush(Color.FromRgb(245, 245, 245));
                }

                public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
                {
                        throw new NotImplementedException();
                }
        }
}
