using System.Linq;
using Deployer.Lumia.Gui;
using Deployer.UI.ViewModels;
using FluentAssertions;
using ReactiveUI;
using Xunit;

namespace Deployer.Lumia.Tests
{
    public class DependencyChecker
    {
        [Fact]
        public void Test()
        {
            var container = CompositionRoot.CreateContainer();
            var types = typeof(MainViewModelBase).Assembly.ExportedTypes.ThatImplement<ReactiveObject>().ToList();

            foreach (var type in types)
            {
                container.Locate(type);
            }
        }
    }
}