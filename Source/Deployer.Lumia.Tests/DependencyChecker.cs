using System.Linq;
using Deployer.Lumia.Gui;
using Deployer.Lumia.Gui.ViewModels;
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
            var types = typeof(MainViewModel).Assembly.ExportedTypes.ThatImplement<ReactiveObject>().ToList();

            foreach (var type in types)
            {
                container.Locate(type);
            }
        }
    }
}