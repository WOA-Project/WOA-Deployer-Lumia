using System;
using System.Globalization;
using System.Windows.Data;
using ByteSizeLib;

namespace Deployer.Lumia.Gui.Views.Transformation
{
    public class GbsToByteSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ByteSize bs)
            {
                return bs.GigaBytes;
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double b)
            {
                return ByteSize.FromGigaBytes(b);
            }

            return Binding.DoNothing;
        }
    }
}