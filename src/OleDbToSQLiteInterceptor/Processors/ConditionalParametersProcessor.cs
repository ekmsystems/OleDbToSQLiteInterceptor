using System;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using DatabaseConnections;

namespace OleDbToSQLiteInterceptor.Processors
{
    internal class ConditionalParametersProcessor : IDatabaseCommandProcessor
    {
        public void Process(DatabaseCommand command, IDatabase database)
        {
            var result = command.CommandText;
            var regex = new Regex(
                @"(?:\b(WHERE|AND|OR|ON))(?!\s+COALESCE)\s+(?:(.*?))([\<\>]?[=>]|[\<\>]|(?:\s+(?:IS(?:\s+NOT)?|(?:NOT\s+)?(?:LIKE|IN))))\s*((?:'[^']*'|[^\s+?;])*)",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var skipClauses = new[]
            {
                "ON"
            };
            var skipOperations = new[]
            {
                "LIKE",
                "NOT LIKE",
                "IN",
                "NOT IN"
            };

            foreach (Match match in regex.Matches(command.CommandText))
            {
                var original = command.CommandText.Substring(match.Index, match.Length);
                var clause = match.Groups[1].Value.Trim().ToUpper();
                var columnName = RemoveWrappedBrackets(match.Groups[2].Value.Trim());
                var op = match.Groups[3].Value.Trim();
                var columnValue = RemoveWrappedBrackets(match.Groups[4].Value.Trim());
                var prefix = "";

                if (skipClauses.Any(x => clause.ToUpper() == x))
                    continue;

                if (skipOperations.Any(x => op.ToUpper() == x))
                    continue;

                if (columnName.StartsWith("CASE WHEN"))
                    continue;

                if (op.ToUpper().Contains("IS") && columnValue.ToUpper().Contains("NULL"))
                    continue;

                // Sort out the prefix
                if (columnName.StartsWith("("))
                {
                    var left = 0;
                    var diff = 0;

                    for (var i = columnName.Length - 1; i >= 0; --i)
                        if (columnName[i] == '(') left++;

                    if (left > 0)
                    {
                        var tmp = columnName.Substring(left);
                        diff = columnName.Replace(tmp, "").Length;
                        columnName = tmp;
                    }

                    prefix = string.Join("", Enumerable.Range(0, diff).Select(x => "(").ToArray());
                }

                // Make sure our parameters are safe for use with COALESCE
                if (columnValue.StartsWith("@"))
                {
                    var parameter = command.Parameters
                        .Cast<IDataParameter>()
                        .SingleOrDefault(x => x.ParameterName == columnValue);

                    if (parameter != null)
                        parameter.Value = EnsureSafeForCoalesce(parameter.Value);
                }

                // Make sure NULLS are converted to 0
                if (Regex.IsMatch(columnValue, "(null)", RegexOptions.IgnoreCase | RegexOptions.Singleline))
                    columnValue = CaseInsensitiveReplace(columnValue, "null", "0");

                var altered = original.Replace(match.Value, string.Format("{0} {1}COALESCE({2}, {3}) {4} {5}",
                    clause,
                    prefix,
                    string.IsNullOrEmpty(prefix) ? columnName : columnName.Replace(prefix, ""),
                    (op == "IS NOT") || (op == "<>") ? columnValue.Trim('(', ')') : "0",
                    op,
                    columnValue));

                altered = CaseInsensitiveReplace(altered, "IS NOT", "<>");
                altered = CaseInsensitiveReplace(altered, "IS 0", "= 0");

                result = result.Replace(original, altered);
            }

            command.CommandText = result;
        }

        private static object EnsureSafeForCoalesce(object value)
        {
            if ((value == null) || Convert.IsDBNull(value))
                return 0;
            return value;
        }

        private static string RemoveWrappedBrackets(string s)
        {
            var result = s;

            while (result.StartsWith("(") && result.EndsWith(")"))
            {
                result = result.Substring(1, result.Length - 1);
                result = result.Substring(0, result.Length - 1);
            }

            return result;
        }

        private static string CaseInsensitiveReplace(string input, string search, string replacement)
        {
            return Regex.Replace(input,
                Regex.Escape(search),
                replacement.Replace("$", "$$"),
                RegexOptions.IgnoreCase);
        }
    }
}