using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Deployer.Execution
{
    public class Deployer
    {
        private readonly IEnumerable<Type> typeUniverse;
        private readonly IRunner runner;

        public Deployer(IEnumerable<Type> typeUniverse, IRunner runner)
        {
            this.typeUniverse = typeUniverse;
            this.runner = runner;
        }

        public async Task Deploy(string path)
        {
            var script = new ScriptParser(Tokenizer.Create()).Parse(File.ReadAllText(path));
            await runner.Run(script);
        }

        public async Task ToggleDualBoot(bool enabled)
        {
            throw new NotImplementedException();
        }
    }
}