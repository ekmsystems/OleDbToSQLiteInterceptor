using System;
using System.Collections.Generic;
using System.Linq;
using DatabaseConnections;
using OleDbToSQLiteInterceptor.Processors;

namespace OleDbToSQLiteInterceptor
{
    public class OleDbToSQLite : IDatabaseCommandInterceptor
    {
        private readonly List<IDatabaseCommandProcessor> _primaryProcessors;
        private readonly List<IDatabaseCommandProcessor> _secondaryProcessors;

        public OleDbToSQLite()
            : this(new List<IDatabaseCommandProcessor>
            {
                new DropColumnProcessor(),
                new DateTimeProcessor()
            },  new List<IDatabaseCommandProcessor>
            {
                new FormatProcessor(),
                new SyntaxProcessor(),
                new ColumnAliasProcessor(),
                new ConditionalParametersProcessor()
            })
        {
        }

        internal OleDbToSQLite(
            List<IDatabaseCommandProcessor> primaryProcessors,
            List<IDatabaseCommandProcessor> secondaryProcessors)
        {
            _primaryProcessors = primaryProcessors;
            _secondaryProcessors = secondaryProcessors;
        }

        public void Intercept(DatabaseCommand command, IDatabase database)
        {
            foreach (var processor in _primaryProcessors)
                processor.Process(command, database);

            var replacements = GetSectionsWeDoNotWantToModify(command.CommandText);

            command.CommandText = HideSections(command.CommandText, replacements);

            foreach (var processor in _secondaryProcessors)
                processor.Process(command, database);

            command.CommandText = ShowSections(command.CommandText, replacements);

            command.CommandText = command.CommandText.TrimEnd(';') + ";";
        }

        private static string GeneratePlaceholder()
        {
            return "{" + Guid.NewGuid() + "}";
        }

        private static IDictionary<string, string> GetSectionsWeDoNotWantToModify(string query)
        {
            var replacements = (from tag in new[] { "html", "script", "div" }
                                let startTag = string.Format("<{0}>", tag)
                                let endTag = string.Format("</{0}>", tag)
                                from value in query.GetStringsBetween(startTag, endTag)
                                select value).ToDictionary(value => GeneratePlaceholder());

            foreach (var value in query.GetStringsBetween("'", "'"))
                replacements.Add(GeneratePlaceholder(), value);

            return replacements;
        }

        private static string HideSections(string query, IEnumerable<KeyValuePair<string, string>> sections)
        {
            return sections
                .Where(x => query.Contains(x.Value))
                .Aggregate(query, (current, section) => current.Replace(section.Value, section.Key));
        }

        private static string ShowSections(string query, IEnumerable<KeyValuePair<string, string>> sections)
        {
            return sections
                .Reverse()
                .Where(x => query.Contains(x.Key))
                .Aggregate(query, (current, section) => current.Replace(section.Key, section.Value));
        }
    }
}
