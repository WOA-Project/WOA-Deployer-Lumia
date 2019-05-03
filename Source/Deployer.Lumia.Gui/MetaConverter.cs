using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using Grace.DependencyInjection;

namespace Deployer.Lumia.Gui
{
    public class MetaConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Binding.DoNothing;
            }

            if (value.GetType().GetGenericTypeDefinition() == typeof(Meta<>))
            {
                var convert = (IActivationStrategyMetadata)value.GetType().GetProperty("Metadata").GetValue(value);
                return convert[parameter];
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}