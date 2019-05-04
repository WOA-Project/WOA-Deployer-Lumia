using System.Windows;
using System.Windows.Controls;
using Deployer.Lumia.DiskPreparers;
using Grace.DependencyInjection;

namespace Deployer.Lumia.Gui
{
    public class DiskPreparerSelector : DataTemplateSelector
    {
        public DataTemplate KeepW10MTemplate { get; set; }
        public DataTemplate OverwriteW10PTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item != null && item.GetType().GetGenericTypeDefinition() == typeof(Meta<>))
            {
                var propertyInfo = item.GetType().GetProperty("Value");
                if (propertyInfo == null)
                {
                    return base.SelectTemplate(item, container);
                }

                var value = propertyInfo.GetValue(item);

                switch (value)
                {
                    case KeepMobileOSDiskLayoutPreparer _:
                        return KeepW10MTemplate;
                    case WipeMobileOSDiskLayoutPreparer _:
                        return OverwriteW10PTemplate;
                }
            }

            return base.SelectTemplate(item, container);
        }
    }
}