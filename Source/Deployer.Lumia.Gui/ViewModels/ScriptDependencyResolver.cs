using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Deployer.Execution;
using Deployer.Tasks;
using Deployer.UI.ViewModels;
using Grace.DependencyInjection;

namespace Deployer.Lumia.Gui.ViewModels
{
    public class ScriptDependencyResolver : IScriptDependencyResolver
    {
        private readonly IEnumerable<Type> typeUniverse;
        private readonly IExportLocatorScope locatorService;
        private readonly IDialog dialog;

        public ScriptDependencyResolver(IEnumerable<Type> typeUniverse, IExportLocatorScope locatorService,
            IDialog dialog)
        {
            this.typeUniverse = typeUniverse;
            this.locatorService = locatorService;
            this.dialog = dialog;
        }

        public async Task<IDeploymentContext> GetDeploymentContext(Script script)
        {
            WimPickViewModel wimPickViewModel;
            using (var scope = locatorService.BeginLifetimeScope())
            {
                wimPickViewModel = scope.Locate<WimPickViewModel>();
            }

            var allRequirements = GetRequirements(script);
            var deploymentContext = new DeploymentContext();

            if (!allRequirements.Contains(Dependency.DeploymentOptions))
            {
                return deploymentContext;
            }

            var dialogResult = await dialog.Show("WindowsDeployment", wimPickViewModel);
            if (dialogResult != DialogResult.Yes || wimPickViewModel.WimMetadata == null)
            {
                return null;
            }

            deploymentContext.DeploymentOptions = new WindowsDeploymentOptions
            {
                ImageIndex = wimPickViewModel.WimMetadata.SelectedDiskImage.Index,
                ImagePath = wimPickViewModel.WimMetadata.Path,
                UseCompact = false,
            };

            return deploymentContext;
        }

        private IEnumerable<Dependency> GetRequirements(Script script)
        {
            var requirementsByType =
                from taskName in script.Sentences.OfType<CommandSentence>().Select(x => x.Command.Name)
                let type = typeUniverse.First(x => x.Name.Equals(taskName))
                let attributes = type.GetTypeInfo().GetCustomAttributes<RequiresAttribute>()
                let requirements = from requirement in attributes select requirement.Dependency
                select new {Type = type, Requirements = requirements};

            var allRequirements = requirementsByType.SelectMany(x => x.Requirements);
            return allRequirements;
        }
    }
}