using Deployer.Core;
using Deployment;

namespace Deployer.Test
{
    public class TestStringBuilder : IPathBuilder
    {
        public string Replace(string str)
        {
            return str;
        }
    }
}