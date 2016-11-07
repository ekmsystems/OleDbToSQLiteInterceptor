using DatabaseConnections;
using Moq;
using NUnit.Framework;
using OleDbToSQLiteInterceptor.Processors;

namespace OleDbToSQLiteInterceptor.Tests.Processors
{
    [TestFixture]
    [Parallelizable]
    public class DateTimeProcessorTests
    {
        [SetUp]
        public void SetUp()
        {
            _database = new Mock<IDatabase>();
            _processor = new DateTimeProcessor();
        }

        private Mock<IDatabase> _database;
        private DateTimeProcessor _processor;

        [Test]
        [TestCase("#12/01/2016#", "'2016-01-12 00:00:00'")]
        [TestCase("#12-01-2016#", "'2016-01-12 00:00:00'")]
        [TestCase("#12/Jan/2016#", "'2016-01-12 00:00:00'")]
        [TestCase("#12-Jan-2016#", "'2016-01-12 00:00:00'")]
        [TestCase("#12 January 2016#", "'2016-01-12 00:00:00'")]
        [TestCase("#12 January 2016#", "'2016-01-12 00:00:00'")]
        public void Process_ShouldFix_Dates(string dateString, string expected)
        {
            var command = new DatabaseCommand
            {
                CommandText = dateString
            };

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        [TestCase("#12/01/2016#", "2016-01-12 00:00:00")]
        [TestCase("#12-01-2016#", "2016-01-12 00:00:00")]
        [TestCase("#12/Jan/2016#", "2016-01-12 00:00:00")]
        [TestCase("#12-Jan-2016#", "2016-01-12 00:00:00")]
        [TestCase("#12 January 2016#", "2016-01-12 00:00:00")]
        [TestCase("#12 January 2016#", "2016-01-12 00:00:00")]
        [TestCase("#10:11:12#", "10:11:12")]
        [TestCase("#14:11:12#", "14:11:12")]
        [TestCase("#12/01/2016 10:11:12#", "2016-01-12 10:11:12")]
        [TestCase("#12-01-2016 10:11:12#", "2016-01-12 10:11:12")]
        [TestCase("#12/Jan/2016 10:11:12#", "2016-01-12 10:11:12")]
        [TestCase("#12-Jan-2016 10:11:12#", "2016-01-12 10:11:12")]
        [TestCase("#12 January 2016 10:11:12#", "2016-01-12 10:11:12")]
        [TestCase("#12 January 2016 10:11:12#", "2016-01-12 10:11:12")]
        public void Process_ShouldFix_DateTimeParameters(string parameterValue, string expected)
        {
            var command = new DatabaseCommand
            {
                CommandText = "@test",
                Parameters = new[]
                {
                    new DbParam("@test", parameterValue)
                }
            };

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.Parameters[0].Value);
        }

        [Test]
        [TestCase("#12/01/2016 10:11:12#", "'2016-01-12 10:11:12'")]
        [TestCase("#12-01-2016 10:11:12#", "'2016-01-12 10:11:12'")]
        [TestCase("#12/Jan/2016 10:11:12#", "'2016-01-12 10:11:12'")]
        [TestCase("#12-Jan-2016 10:11:12#", "'2016-01-12 10:11:12'")]
        [TestCase("#12 January 2016 10:11:12#", "'2016-01-12 10:11:12'")]
        [TestCase("#12 January 2016 10:11:12#", "'2016-01-12 10:11:12'")]
        public void Process_ShouldFix_DateTimes(string dateTimeString, string expected)
        {
            var command = new DatabaseCommand
            {
                CommandText = dateTimeString
            };

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        [TestCase("#10:11:12#", "'10:11:12'")]
        [TestCase("#14:11:12#", "'14:11:12'")]
        public void Process_ShouldFix_Times(string timeString, string expected)
        {
            var command = new DatabaseCommand
            {
                CommandText = timeString
            };

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [TestCase("#01/01/0001#")]
        [TestCase("#01-01-0001#")]
        [TestCase("#01/Jan/0001#")]
        [TestCase("#01-Jan-0001#")]
        [TestCase("#01 January 0001#")]
        [TestCase("#01 January 0001#")]
        [TestCase("#01/01/0001 00:00:00#")]
        [TestCase("#01-01-0001 00:00:00#")]
        [TestCase("#01/Jan/0001 00:00:00#")]
        [TestCase("#01-Jan-0001 00:00:00#")]
        [TestCase("#01 January 0001 00:00:00#")]
        [TestCase("#01 January 0001 00:00:00#")]
        public void Process_ShouldFix_MinimumDateTimeValue(string parameterValue)
        {
            var command = new DatabaseCommand
            {
                CommandText = "@test",
                Parameters = new[]
                {
                    new DbParam("@test", parameterValue)
                }
            };
            const string expected = "1970-01-01 00:00:00";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.Parameters[0].Value);
        }
    }
}