using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DatabaseConnections;

namespace OleDbToSQLiteInterceptor.Processors
{
    internal class ColumnAliasProcessor : IDatabaseCommandProcessor
    {
        public void Process(DatabaseCommand command, IDatabase database)
        {
            var query = command.CommandText;
            var replacements = query.GetStringsBetween("(", ")")
                .Distinct()
                .ToDictionary(x => x, y => GeneratePlaceholder());

            query = replacements.Aggregate(query, (current, subquery) => current.Replace(subquery.Key, subquery.Value));

            var result = query;
            var regex = new Regex(
                @"(?:UNION\s+)?SELECT(?:\s+DISTINCT)?\s+(.*?)\s+FROM\b",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            foreach (Match match in regex.Matches(query))
            {
                var original = query.Substring(match.Index, match.Length);
                var change = original;
                var columns = ExtractValidColumns(match.Groups[1].Value.Split(','))
                    .Distinct()
                    .Select(x => x.Trim('[', ']'))
                    .ToList();

                foreach (var column in columns.OrderByDescending(x => x.Length))
                    if (Regex.IsMatch(column, @"\s+AS\s+"))
                    {
                        var placeholder = GeneratePlaceholder();
                        replacements.Add(column, placeholder);
                        change = change.Replace(column, placeholder);
                    }
                    else
                    {
                        change = Regex.Replace(change,
                            string.Format(@"(SELECT.*?)\s*((?:\[|\b){0}(?:\]|\b)(?!\s+AS))([,]?\s*)(.*?FROM)", column),
                            @"$1 [$2] AS '$2'$3$4",
                            RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    }

                change = Regex.Replace(change,
                    @"\[(\[.*?\])\]",
                    @"$1",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                change = Regex.Replace(change,
                    @"\'\[(.*?)\]\'",
                    @"'$1'",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);

                result = result.Replace(original, change);
            }

            result = replacements.Aggregate(result, (current, subquery) => current.Replace(subquery.Value, subquery.Key));

            command.CommandText = result;
        }

        private static IEnumerable<string> ExtractValidColumns(IEnumerable<string> columns)
        {
            return (from column in columns
                select column.Trim()
                into c
                where !string.IsNullOrEmpty(c.Trim('[', ']'))
                where !c.Contains("*")
                where !c.Contains(".")
                where !c.Contains("{") && !c.Contains("}")
                select c).ToList();
        }

        private static string GeneratePlaceholder()
        {
            return "{" + Guid.NewGuid() + "}";
        }
    }
}