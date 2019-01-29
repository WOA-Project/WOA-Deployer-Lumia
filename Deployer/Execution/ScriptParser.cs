using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Superpower;

namespace Deployer.Execution
{
    public class ScriptParser
    {
        private readonly Tokenizer<LangToken> tokenizer;

        public ScriptParser(Tokenizer<LangToken> tokenizer)
        {
            this.tokenizer = tokenizer;
        }

        public Script Parse(string input)
        {
            var tokenList = tokenizer.Tokenize(input);
            return Parsers.Script.Parse(tokenList);
        }

        static IEnumerable<string> Lines(string str)
        {
            using (var reader = new StringReader(str))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }
        
        private static string Normalize(string str)
        {
            var filtered = RemoveComments(str);
            filtered = filtered.TrimStart();

            var builder = new StringBuilder();
            var list = Lines(filtered);
            foreach (var line in list.SkipLastN(1))
            {
                builder.Append(Filter(line) + "\n");
            }

            var last = list.LastOrDefault();
            if (last != null)
            {
                builder.Append(Filter(last));
            }

            return builder.ToString();
        }

        private static string Filter(string read)
        {
            var trimmed = read.Trim();

            trimmed = Regex.Replace(trimmed, @"\s+=\s+", "=");

            if (trimmed.StartsWith("PatchCode") || trimmed.StartsWith("EndPatch"))
            {
                trimmed = trimmed.Replace("\r\n", "");
                trimmed = trimmed.Replace("\n", "");
            }

            return trimmed;
        }

        private static string RemoveComments(string input)
        {
            var blockComments = @"/\*(.*?)\*/";
            var lineComments = @"//(.*?)\r?\n";
            var strings = @"""((\\[^\n]|[^""\n])*)""";
            var verbatimStrings = @"@(""[^""]*"")+";

            return Regex.Replace(input,
                blockComments + "|" + lineComments + "|" + strings + "|" + verbatimStrings,
                me =>
                {
                    if (me.Value.StartsWith("/*") || me.Value.StartsWith("//"))
                        return me.Value.StartsWith("//") ? Environment.NewLine : "";
                    // Keep the literal strings
                    return me.Value;
                },
                RegexOptions.Singleline);
        }
    }
}