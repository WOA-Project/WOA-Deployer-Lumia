using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Serilog;

namespace Deployer.Execution
{
    public class Runner : IRunner
    {
        private readonly IEnumerable<Type> typeUniverse;
        private readonly IInstanceBuilderProvider instanceBuilder;

        public Runner(IEnumerable<Type> typeUniverse, IInstanceBuilderProvider instanceBuilder)
        {
            this.typeUniverse = typeUniverse;
            this.instanceBuilder = instanceBuilder;
        }

        public async Task Run(Script script)
        {
            foreach (var sentence in script.Sentences)
            {
                await Run(sentence);
            }
        }

        private async Task Run(Sentence sentence)
        {
            var builder = await instanceBuilder.Create();
            var instance = BuildInstance(builder, sentence);
            
            var operationStr = GetOperationStr(sentence.Command.Name, instance.GetType());
            Log.Information($"{operationStr} {{Params}}", string.Join(",", sentence.Command.Arguments));

            await instance.Execute();
        }

        private static string GetOperationStr(string commandName, Type type)
        {
            var description = type.GetTypeInfo().GetCustomAttribute<TaskDescriptionAttribute>()?.Text;

            if (description != null)
            {
                return description;
            }

            return "Executing " + commandName;
        }

        private static string GetTaskDescription(IDeploymentTask instance)
        {
            return instance.GetType().GetCustomAttribute<DescriptionAttribute>().Description ?? instance.GetType().Name;
        }

        private IDeploymentTask BuildInstance(IInstanceBuilder builder, Sentence sentence)
        {
            try
            {
                var type = typeUniverse.Single(x => x.Name == sentence.Command.Name);
                var parameters = sentence.Command.Arguments.Select(x => x.Value);
                return (IDeploymentTask) builder.Create(type, parameters.ToArray());
            }
            catch (InvalidOperationException)
            {
                throw new ScriptException($"Task '{sentence.Command.Name}' not found");
            }
        }
    }
}