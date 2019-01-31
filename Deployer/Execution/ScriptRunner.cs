using System.IO;
using System.Threading.Tasks;

namespace Deployer.Execution
{
    public class ScriptRunner
    {
        private readonly IRunner runner;

        public ScriptRunner(IRunner runner)
        {
            this.runner = runner;
        }

        public async Task Deploy(string path)
        {
            var script = new ScriptParser(Tokenizer.Create()).Parse(File.ReadAllText(path));
            await runner.Run(script);
        }
    }
}