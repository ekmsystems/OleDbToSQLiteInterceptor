using System;
using System.Text.RegularExpressions;
using DatabaseConnections;

namespace OleDbToSQLiteInterceptor.Processors
{
    internal class SyntaxProcessor : IDatabaseCommandProcessor
    {
        public void Process(DatabaseCommand command, IDatabase database)
        {
            FixAutoIncrement(command);
            FixBooleans(command);
            FixDelete(command);
            FixRightJoin(command);
            FixTop(command);
            FixFirst(command);
            FixLCase(command);
            FixUCase(command);
            FixLen(command);
            FixLeft(command);
            FixIsNull(command);
            FixNow(command);
            FixIIF(command);
            FixStrComp(command);
        }

        private static void FixAutoIncrement(DatabaseCommand command)
        {
            var result = Regex.Replace(command.CommandText,
                @"AUTOINCREMENT",
                "INTEGER",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            command.CommandText = result;
        }

        private static void FixBooleans(DatabaseCommand command)
        {
            var result = command.CommandText;

            result = Regex.Replace(result,
                @"\btrue\b",
                "1",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            result = Regex.Replace(result,
                @"\bfalse\b",
                "0",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            command.CommandText = result;
        }

        private static void FixDelete(DatabaseCommand command)
        {
            var result = Regex.Replace(command.CommandText,
                @"DELETE(.*?)\bFROM",
                "DELETE FROM",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            command.CommandText = result;
        }

        private static void FixRightJoin(DatabaseCommand command)
        {
            const string pattern =
                @"(\[?\w*\]?(?:\s+AS\s\[?\w*\]?)?)\s+RIGHT\s+JOIN\s+((?:\(.*?\)|(?:\[?\w*\]?(?:\s+AS\s\[?\w*\]?)?)*)(?:\s+AS\s\[?\w*\]?)?)";
            var result = command.CommandText;
            var re = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            while (re.Matches(result).Count > 0)
                result = re.Replace(result, "$2 LEFT JOIN $1");

            command.CommandText = result;
        }

        private static void FixTop(DatabaseCommand command)
        {
            var result = command.CommandText;

            result = Regex.Replace(result,
                @"\((SELECT)\s+TOP\s+(\d+)\s+(.*?FROM.*?)\)",
                "($1 $3 LIMIT $2)",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            result = Regex.Replace(result,
                @"(SELECT)\s+TOP\s+(\d+)\s+(.*?FROM.*[^;])",
                "$1 $3 LIMIT $2",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            command.CommandText = result;
        }

        private static void FixFirst(DatabaseCommand command)
        {
            var result = Regex.Replace(command.CommandText,
                @"first\((.*?)\)",
                "$1",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            command.CommandText = result;
        }

        private static void FixLCase(DatabaseCommand command)
        {
            var result = Regex.Replace(command.CommandText,
                @"lcase\((.*?)\)",
                "LOWER($1)",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            command.CommandText = result;
        }

        private static void FixUCase(DatabaseCommand command)
        {
            var result = Regex.Replace(command.CommandText,
                @"ucase\((.*?)\)",
                "UPPER($1)",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            command.CommandText = result;
        }

        private static void FixLen(DatabaseCommand command)
        {
            var result = Regex.Replace(command.CommandText,
                @"len\((.*?)\)",
                "LENGTH($1)",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            command.CommandText = result;
        }

        private static void FixLeft(DatabaseCommand command)
        {
            var result = Regex.Replace(command.CommandText,
                @"left\((.*?)\)",
                "SUBSTR($1)",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            command.CommandText = result;
        }

        private static void FixIsNull(DatabaseCommand command)
        {
            var result = Regex.Replace(command.CommandText,
                @"isnull\((.*?)\)",
                "COALESCE($1, 0) = 0",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            command.CommandText = result;
        }

        private static void FixNow(DatabaseCommand command)
        {
            var result = Regex.Replace(command.CommandText,
                @"now\(\)",
                "date('now', 'localtime')",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            command.CommandText = result;
        }

        private static void FixIIF(DatabaseCommand command)
        {
            var result = Regex.Replace(command.CommandText,
                @"\bIIF\((.*),\s*(.*),\s*(.*?)\)",
                "(CASE WHEN $1 THEN $2 ELSE $3 END)",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            command.CommandText = result;
        }

        private static void FixStrComp(DatabaseCommand command)
        {
            var result = command.CommandText;
            var regex = new Regex(
                @"strcomp\((.*?),\s*(.*?),\s*\d+\)\s*(.*?)\s*(\d+)",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            foreach (Match match in regex.Matches(command.CommandText))
            {
                var original = command.CommandText.Substring(match.Index, match.Length);
                var change = string.Format("INSTR(LOWER({0}), {1}) {2} {3}",
                    match.Groups[1].Value,
                    match.Groups[2].Value,
                    match.Groups[3].Value,
                    Convert.ToInt32(match.Groups[4].Value) + 1);

                result = result.Replace(original, change);
            }

            command.CommandText = result;
        }
    }
}