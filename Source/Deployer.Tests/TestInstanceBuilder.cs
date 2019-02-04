using System.Collections.Generic;
using Deployer.Core;
using Deployment;
using Grace.DependencyInjection;

namespace Deployer.Test
{
    internal class TestInstanceBuilder : InstanceBuilder
    {
        private readonly List<object> createdInstances = new List<object>();

        public TestInstanceBuilder(ILocatorService container, IPathBuilder pathBuilder) : base(container, pathBuilder)
        {
        }

        public IReadOnlyCollection<object> CreatedInstances => createdInstances.AsReadOnly();

        protected override void OnInstanceCreated(object instance)
        {
            createdInstances.Add(instance);
        }
    }
}