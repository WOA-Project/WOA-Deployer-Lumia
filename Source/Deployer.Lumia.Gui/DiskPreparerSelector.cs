using System.Windows;
using System.Windows.Controls;
using Deployer.Lumia.DiskPreparers;
using Deployer.Lumia.Gui.ViewModels;

namespace Deployer.Lumia.Gui
{
    public class DiskPreparerSelector : DataTemplateSelector
    {
        public DataTemplate KeepW10MTemplate { get; set; }
        public DataTemplate OverwriteW10PTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is DiskLayoutPreparerViewModel vm)
            {
                switch (vm.Preparer)
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