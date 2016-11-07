using System.Text.RegularExpressions;
using DatabaseConnections;

namespace OleDbToSQLiteInterceptor.Processors
{
    internal class FormatProcessor : IDatabaseCommandProcessor
    {
        public void Process(DatabaseCommand command, IDatabase database)
        {
            FixNoSpaceAfterClosedParenthesis(command);
            FixSquareBrackets(command);
            FixNotEquals(command);
        }

        private static void FixNoSpaceAfterClosedParenthesis(DatabaseCommand command)
        {
            var result = Regex.Replace(command.CommandText,
                @"\)(\w)",
                ") $1",
                RegexOptions.Singleline);

            command.CommandText = result;
        }

        private static void FixSquareBrackets(DatabaseCommand command)
        {
            var result = Regex.Replace(command.CommandText,
                @"\[(\w+)[.](\w+)",
                "[$1].[$2",
                RegexOptions.Singleline);

            command.CommandText = result;
        }

        private static void FixNotEquals(DatabaseCommand command)
        {
            var result = Regex.Replace(command.CommandText,
                @"!=",
                "<>",
                RegexOptions.Singleline);

            command.CommandText = result;
        }
    }
}