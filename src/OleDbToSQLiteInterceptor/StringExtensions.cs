using System.Collections.Generic;

namespace OleDbToSQLiteInterceptor
{
    internal static class StringExtensions
    {
        public static IEnumerable<string> GetStringsBetween(this string text, string start, string end)
        {
            var results = new List<string>();
            var n1 = 0;
            var n2 = 0;
            var n3 = 0;

            for (var i = 0; i < text.Length; i++)
            {
                var c1 = Mid(text, i, start.Length);
                var c2 = Mid(text, i, end.Length);

                if ((n1 > 0) && (c2 == end))
                {
                    n2++;
                }
                else if (c1 == start)
                {
                    if (n1 == 0)
                        n3 = i;
                    n1++;
                }

                if ((n1 <= 0) || (n1 != n2))
                    continue;

                results.Add(text.Substring(n3, i - n3 + end.Length));
                n1 = n2 = 0;
            }

            return results;
        }

        private static string Mid(string text, int index, int length)
        {
            return text.Length >= index + length
                ? text.Substring(index, length)
                : text.Substring(index, text.Length - index);
        }
    }
}
