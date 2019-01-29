using Superpower;
using Superpower.Parsers;

namespace Deployer.Execution
{
    public static class Parsers
    {
        public static TokenListParser<LangToken, string> String => Token.EqualTo(LangToken.String).Select(x =>
        {
            var stringValue = x.ToStringValue();
            return stringValue.Substring(1, stringValue.Length-2);
        });
        public static TokenListParser<LangToken, string> Identifier => Token.EqualTo(LangToken.Identifier).Select(x => x.ToStringValue());
        public static TokenListParser<LangToken, string> Number => Token.EqualTo(LangToken.Number).Select(x => x.ToStringValue());

        public static TokenListParser<LangToken, string> Value => String.Or(Number).Or(Identifier);

        public static TokenListParser<LangToken, Argument> PositionalArgument =>
            from t in Value
            select (Argument)new PositionalArgument(t);

        public static TokenListParser<LangToken, Argument> Argument => PositionalArgument;

        public static TokenListParser<LangToken, Argument[]> Arguments =>
            from _ in Token.EqualTo(LangToken.Space)
            from t in Argument.ManyDelimitedBy(Token.EqualTo(LangToken.Space))
            select t;

        public static TokenListParser<LangToken, Command> RegularCommand =>
            from name in Identifier
            from args in Arguments.OptionalOrDefault()
            select (Command)new Command(name, args ?? new Argument[0]);

        public static TokenListParser<LangToken, Command> Command => RegularCommand;

        public static TokenListParser<LangToken, Sentence> Sentence => CommandSentence;
        
        private static TokenListParser<LangToken, Sentence> CommandSentence => Command.Select(x => new Sentence(x));

        public static TokenListParser<LangToken, Script> Script =>
            from cmds in Sentence.ManyDelimitedBy(Token.EqualTo(LangToken.NewLine))
                .AtEnd()
            select new Script(cmds);
    }    
}

