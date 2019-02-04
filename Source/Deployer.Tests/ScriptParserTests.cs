using Deployment;
using FluentAssertions;
using Xunit;

namespace Deployer.Test
{
    public class ScriptParserTests
    {
        [Theory]
        [InlineData("Task", "Task()")]
        public void Test1(string input, string expected)
        {
            AssertParse(input, expected);
        }

        private void AssertParse(string input, string expected)
        {
            var parser = new ScriptParser(Tokenizer.Create());
            parser.Parse(input).ToString().Should().Be(expected);        
        }
    }
}
