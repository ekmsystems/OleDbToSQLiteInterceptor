using System;

namespace OleDbToSQLiteInterceptor
{
    internal static class DateTimeExtensions
    {
        public static string ToSQLiteDateTime(this DateTime dt)
        {
            var minValue = new DateTime(1970, 1, 1);
            if (dt < minValue)
                dt = minValue;
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}