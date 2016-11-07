using System;
using NUnit.Framework;

namespace OleDbToSQLiteInterceptor.Tests
{
    [TestFixture]
    [Parallelizable]
    public class DateTimeExtensionsTests
    {
        [Test]
        public void ToSQLiteDateTime_ShouldNotReturn_DateBefore1970()
        {
            var dt = new DateTime(1969, 12, 31);

            var result = dt.ToSQLiteDateTime();

            Assert.AreEqual("1970-01-01 00:00:00", result);
        }

        [Test]
        public void ToSQLiteDateTime_ShouldReturn_FormattedDateString()
        {
            var dt = new DateTime(2000, 5, 23, 12, 15, 2);

            var result = dt.ToSQLiteDateTime();

            Assert.AreEqual("2000-05-23 12:15:02", result);
        }
    }
}