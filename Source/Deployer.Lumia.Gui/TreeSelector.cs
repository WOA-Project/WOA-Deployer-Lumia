using System.Windows;
using System.Windows.Controls;
using Deployer.Lumia.Gui.ViewModels;

namespace Deployer.Lumia.Gui
{
    public class TreeSelector : DataTemplateSelector
    {
        public DataTemplate LeafTemplate { get; set; }
        public DataTemplate NodeTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is FolderNode fn)
            {
                return fn.Children == null ? LeafTemplate : NodeTemplate;
            }

            return base.SelectTemplate(item, container);
        }
    }
}