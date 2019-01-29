using System;
using Deployer;
using Deployer.Services;

namespace Deployment.Console
{
    internal class BcdInvokerFactory : IBcdInvokerFactory
    {
        public IBcdInvoker Create(string path)
        {
            return new BcdInvoker(path);
        }
    }
}