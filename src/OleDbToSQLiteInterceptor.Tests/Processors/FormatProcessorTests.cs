using DatabaseConnections;
using Moq;
using NUnit.Framework;
using OleDbToSQLiteInterceptor.Processors;

namespace OleDbToSQLiteInterceptor.Tests.Processors
{
    [TestFixture]
    [Parallelizable]
    public class FormatProcessorTests
    {
        [SetUp]
        public void SetUp()
        {
            _database = new Mock<IDatabase>();
            _processor = new FormatProcessor();
        }

        private Mock<IDatabase> _database;
        private FormatProcessor _processor;

        [Test]
        public void Process_ShouldFix_NoSpaceAfterClosedParenthesis()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"(something)gar"
            };
            const string expected = @"(something) gar";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        public void Process_ShouldFix_NotEquals()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"[myvar] != 'test'"
            };
            const string expected = @"[myvar] <> 'test'";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        public void Process_ShouldFix_SquareBrackets()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"[dbo.table]"
            };
            const string expected = @"[dbo].[table]";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }
    }
}