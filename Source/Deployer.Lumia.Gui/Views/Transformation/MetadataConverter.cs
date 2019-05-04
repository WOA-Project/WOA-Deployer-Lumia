using System;
using System.Globalization;
using System.Windows.Data;
using Grace.DependencyInjection;

namespace Deployer.Lumia.Gui.Views.Transformation
{
    public class MetadataConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Binding.DoNothing;
            }

            if (value.GetType().GetGenericTypeDefinition() == typeof(Meta<>))
            {
                var propertyInfo = value.GetType().GetProperty("Metadata");
                if (propertyInfo == null)
                {
                    return Binding.DoNothing;
                }

                var convert = (IActivationStrategyMetadata)propertyInfo.GetValue(value);
                return convert[parameter];
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}