using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DatabaseConnections;

namespace OleDbToSQLiteInterceptor.Processors
{
    internal class DropColumnProcessor : IDatabaseCommandProcessor
    {
        public void Process(DatabaseCommand command, IDatabase database)
        {
            var re = new Regex(
                @"alter table\s+(.*)\s+drop column\s+(.*)",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (!re.IsMatch(command.CommandText))
                return;

            var match = re.Matches(command.CommandText)[0];
            var table = match.Groups[1].Value.Replace("[", "").Replace("]", "").Trim();
            var column = match.Groups[2].Value.Replace("[", "").Replace("]", "").TrimEnd(';').Trim();
            var schema = GetTableSchema(table, database);

            // Get the column name and its definition from the schema
            var columnDefinitions = ExtractColumnDefinitions(schema);
            var tmpTable = table + "_new";
            var sb = new StringBuilder();

            string value;
            if (columnDefinitions.TryGetValue(column, out value))
            {
                sb.AppendFormat(ReplaceTableName(schema, tmpTable)
                                    .Replace(value + ",", "")
                                    .Replace("  ", " ")
                                    .TrimEnd(';') + ";");
                sb.AppendFormat("INSERT INTO [{0}] SELECT [{1}] FROM [{2}];",
                    tmpTable,
                    string.Join("],[", columnDefinitions.Keys.Where(x => x != column).ToArray()),
                    table);
                sb.AppendFormat("DROP TABLE [{0}];", table);
                sb.AppendFormat("ALTER TABLE [{0}] RENAME TO [{1}];", tmpTable, table);
            }
            else
            {
                sb.Append("SELECT 1;");
            }

            command.CommandText = sb.ToString();
        }

        private static string GetTableSchema(string tableName, IDatabase database)
        {
            return database.ExecuteScalar(new DatabaseCommand
            {
                CommandText = @"SELECT sql FROM sqlite_master WHERE type=@type AND name LIKE @name",
                Parameters = new[]
                {
                    new DbParam("@type", "table"),
                    new DbParam("@name", tableName.ToLower())
                }
            }) as string;
        }

        private static IDictionary<string, string> ExtractColumnDefinitions(string createTableCommand)
        {
            var re = new Regex(@"CREATE TABLE\s+.*?\s+\((.*)\)", RegexOptions.IgnoreCase);
            var match = re.Match(createTableCommand);
            var columnDefinitions = match.Groups[1].Value.Split(',')
                .Select(x => x.Trim())
                .Where(x => !x.ToUpperInvariant().Contains("FOREIGN KEY"));

            return columnDefinitions.ToDictionary(x => x.Split(' ')[0].Replace("[", "").Replace("]", "").Trim());
        }

        private static string ReplaceTableName(string schema, string newTableName)
        {
            var re = new Regex(@"CREATE TABLE\s+(.*?)\s+", RegexOptions.IgnoreCase);
            var altered = re.Replace(schema, string.Format("CREATE TABLE [{0}] ", newTableName));

            return altered;
        }
    }
}