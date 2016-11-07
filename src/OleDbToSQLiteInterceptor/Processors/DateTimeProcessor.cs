using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using DatabaseConnections;

namespace OleDbToSQLiteInterceptor.Processors
{
    internal class DateTimeProcessor : IDatabaseCommandProcessor
    {
        public void Process(DatabaseCommand command, IDatabase database)
        {
            FixDates(command);
            FixTimes(command);
            FixDateTimes(command);
            FixDateTimeParameters(command);
        }

        private static void FixDates(DatabaseCommand command)
        {
            var result = command.CommandText;
            var regex = new Regex(
                @"[#'](0[1-9]|[12]\d|3[01]|[1-9]|[12]\d|3[01])[\-\/\s+](Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec|January|February|March|April|June|July|August|September|October|November|December|[1-9]|[0][1-9]|[12]\d)[\-\/\s+](\d{4})[#']",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            foreach (Match match in regex.Matches(command.CommandText))
            {
                var original = command.CommandText.Substring(match.Index, match.Length);
                var dt = ParseDateTime(match.Value.Replace("#", "").Replace("'", ""));
                var change = original.Replace(match.Value, "'" + dt.ToString("yyyy-MM-dd HH:mm:ss") + "'");

                result = result.Replace(original, change);
            }

            command.CommandText = result;
        }

        private static void FixTimes(DatabaseCommand command)
        {
            var result = command.CommandText;
            var regex = new Regex(
                @"[#'](?:(?:([01]?\d|2[0-3]):)([0-5]?\d):)([0-5]?\d)[#']",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            foreach (Match match in regex.Matches(command.CommandText))
            {
                var original = command.CommandText.Substring(match.Index, match.Length);
                var change = original.Replace(match.Value, match.Value.Replace("#", "'"));

                result = result.Replace(original, change);
            }

            command.CommandText = result;
        }

        private static void FixDateTimes(DatabaseCommand command)
        {
            var result = command.CommandText;
            var regex = new Regex(
                @"(?:[#'](0[1-9]|[12]\d|3[01]|[1-9]|[12]\d|3[01])[\-\/\s+](Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec|January|February|March|April|June|July|August|September|October|November|December|[1-9]|[0][1-9]|[12]\d)[\-\/\s+](\d{4}))\s+(?:(?:(?:([01]?\d|2[0-3]):)([0-5]?\d):)([0-5]?\d)[#'])",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            foreach (Match match in regex.Matches(command.CommandText))
            {
                var original = command.CommandText.Substring(match.Index, match.Length);
                var dt = ParseDateTime(match.Value.Replace("#", "").Replace("'", ""));
                var change = original.Replace(match.Value, "'" + dt.ToString("yyyy-MM-dd HH:mm:ss") + "'");

                result = result.Replace(original, change);
            }

            command.CommandText = result;
        }

        private static void FixDateTimeParameters(DatabaseCommand command)
        {
            for (var i = 0; i < command.Parameters.Count; i++)
            {
                var parameter = command.Parameters[i];
                if (Regex.IsMatch(Convert.ToString(parameter.Value), @"#.*#") || ((parameter.DbType == DbType.Date) ||
                                                                                  (parameter.DbType == DbType.DateTime) ||
                                                                                  (parameter.DbType == DbType.DateTime2) ||
                                                                                  (parameter.DbType ==
                                                                                   DbType.DateTimeOffset)))
                {
                    command.Parameters[i] = new DbParam(parameter.ParameterName, parameter.DbType,
                        ParseDateTimeParameterValue(parameter.Value));
                }
            }
        }

        private static string ParseDateTimeParameterValue(object value)
        {
            if (value is DateTime)
                return ((DateTime) value).ToSQLiteDateTime();

            var parsedValue = StripDateTimeParameterValue(value);

            parsedValue = FixMinDateTime(parsedValue);

            if (IsOnlyTime(parsedValue))
                return parsedValue;

            var dt = ParseDateTime(parsedValue);

            return dt == DateTime.MinValue
                ? parsedValue
                : dt.ToSQLiteDateTime();
        }

        private static bool IsOnlyTime(string s)
        {
            return Regex.IsMatch(s, @"^\d{2}:\d{2}:\d{2}$");
        }

        private static string StripDateTimeParameterValue(object obj)
        {
            return Convert.ToString(obj).Replace("#", "").Replace("'", "");
        }

        private static DateTime ParseDateTime(string s)
        {
            var dateFormats = new[]
            {
                "HH:mm:ss",
                ""
            }.SelectMany(x => new[]
            {
                "dd/MM/yyyy " + x,
                "dd-MM-yyyy " + x,
                "dd/MMM/yyyy " + x,
                "dd-MMM-yyyy " + x,
                "dd MMM yyyy " + x,
                "dd MMMM yyyy " + x
            }).Select(x => x.Trim()).ToArray();

            var dt = DateTime.MinValue;

            foreach (var format in dateFormats)
                if (DateTime.TryParseExact(s, format, CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces, out dt))
                    break;

            return dt;
        }

        private static string FixMinDateTime(string dateTimeString)
        {
            return dateTimeString.Replace("0001", "1970");
        }
    }
}