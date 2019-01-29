using System.Collections.Generic;

namespace Deployer.Execution
{
    public class Command
    {
        public string Name { get; }
        public ICollection<Argument> Arguments { get; }

        public Command(string name, ICollection<Argument> arguments)
        {
            Name = name;
            Arguments = arguments;
        }

        public override string ToString()
        {
            return $"{Name}({string.Join(",", Arguments)})";
        }
    }
}