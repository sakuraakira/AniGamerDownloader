using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BindingConvert
{

    public class BooleanToWhite : IValueConverter
    {
        public object Convert(object values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if ((Boolean)values == false)
                    return Brushes.Black;
                else
                    return Brushes.White;
            }
            catch
            {
                return Brushes.Black;
            }
        }

        public object ConvertBack(object value, Type targetTypes, object parameter, CultureInfo culture)
        {
            try
            {
                if (value.ToString() == "")
                    return 0;
                else
                    return Int32.Parse(value.ToString());
            }
            catch
            {
                return 0;
            }
        }
    }

    public class BooleanToVisibility : IValueConverter
    {
        public object Convert(object values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if ((Boolean)values == true)
                    return System.Windows.Visibility.Visible;
                else
                    return System.Windows.Visibility.Collapsed;
            }
            catch
            {
                return System.Windows.Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
