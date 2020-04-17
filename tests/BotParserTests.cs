using System;
using Xunit;

namespace src
{
    public class UnitTest1
    {
        [Theory]
        // Commands and args uppercase/lowercase
        [InlineData("search hello world!", "search", "hello world!", null)]
        [InlineData("Search hello world!", "search", "hello world!", null)]
        // Commands with bot user
        [InlineData("<@!699673369158877225>search hello world!", "search", "hello world!", null)]
        [InlineData("<@!699673369158877225> Search Hello world!", "search", "Hello world!", null)]
        [InlineData("<@!699673369158877225> search hello world!", "search", "hello world!", null)]
        [InlineData("<@!699673369158877225> command ", "command", null, null)]
        [InlineData("<@!699673369158877225> command", "command", null, null)]
        [InlineData("<@!699673369158877225>command", "command", null, null)]
        [InlineData("<@!699673369158877225>command ", "command", null, null)]

        [InlineData(" Command ", "command", null, null)]
        [InlineData(" command ", "command", null, null)]
        [InlineData(" command", "command", null, null)]
        [InlineData("command", "command", null, null)]
        [InlineData("command ", "command", null, null)]

        [InlineData("<@!699673369158877225> any query?", null, null, "any query")]
        public void GivenExpressionTestCorrectParsing(
            string expression,
            string? expectedCommand,
            string? expectedArgs,
            string? expectedQuery)
        {
            // ARRANGE & ACT
            var parser = new BotParser(expression, false, new string[] { "role" }, false);

            Assert.Equal(expectedCommand, parser.Command);
            Assert.Equal(expectedArgs, parser.CommandArgs);
            Assert.Equal(expectedQuery, parser.Query);
        }
    }
}
