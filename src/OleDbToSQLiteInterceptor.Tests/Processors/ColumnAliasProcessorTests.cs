using DatabaseConnections;
using Moq;
using NUnit.Framework;
using OleDbToSQLiteInterceptor.Processors;

namespace OleDbToSQLiteInterceptor.Tests.Processors
{
    [TestFixture]
    [Parallelizable]
    public class ColumnAliasProcessorTests
    {
        [SetUp]
        public void SetUp()
        {
            _database = new Mock<IDatabase>();
            _processor = new ColumnAliasProcessor();
        }

        private Mock<IDatabase> _database;
        private ColumnAliasProcessor _processor;

        [Test]
        [TestCase("SELECT [name] AS 'ProductName' FROM [test]")]
        [TestCase("SELECT [name] AS 'ProductName', [price] AS 'ProductPrice' FROM [test]")]
        public void Process_ShouldNotModify_ExistingAliases(string commandText)
        {
            var command = new DatabaseCommand
            {
                CommandText = commandText
            };

            _processor.Process(command, _database.Object);

            Assert.AreEqual(commandText, command.CommandText);
        }

        [Test]
        [TestCase("SELECT [] FROM [test]")]
        [TestCase("SELECT [], [name] AS 'ProductName' FROM [test]")]
        public void Process_With_BlankColumn_ShouldNot_ApplyAlias(string commandText)
        {
            var command = new DatabaseCommand
            {
                CommandText = commandText
            };

            _processor.Process(command, _database.Object);

            Assert.AreEqual(commandText, command.CommandText);
        }
    }
}