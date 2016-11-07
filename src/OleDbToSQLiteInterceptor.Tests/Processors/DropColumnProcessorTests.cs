using DatabaseConnections;
using Moq;
using NUnit.Framework;
using OleDbToSQLiteInterceptor.Processors;

namespace OleDbToSQLiteInterceptor.Tests.Processors
{
    [TestFixture]
    [Parallelizable]
    public class DropColumnProcessorTests
    {
        [SetUp]
        public void SetUp()
        {
            _database = new Mock<IDatabase>();
            _processor = new DropColumnProcessor();
        }

        private Mock<IDatabase> _database;
        private DropColumnProcessor _processor;

        [Test]
        public void Process_Given_NotDropColumnCommand_ShouldNot_ModifyCommandText()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"SELECT * FROM [test];"
            };
            const string expected = @"SELECT * FROM [test];";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        public void Process_ShouldCreate_CommandToCreateTableWithRemovedColumn()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"ALTER TABLE [test] DROP COLUMN [remove_me]"
            };
            const string expected =
                @"CREATE TABLE [test_new] ([id] INTEGER PRIMARY KEY, [column_1] INTEGER, [column_2] VARCHAR(255));INSERT INTO [test_new] SELECT [id],[column_1],[column_2] FROM [test];DROP TABLE [test];ALTER TABLE [test_new] RENAME TO [test];";

            _database
                .Setup(x => x.ExecuteScalar(
                    It.Is<DatabaseCommand>(
                        y => y.CommandText == @"SELECT sql FROM sqlite_master WHERE type=@type AND name LIKE @name")))
                .Returns(
                    @"CREATE TABLE [test] ([id] INTEGER PRIMARY KEY, [column_1] INTEGER, [remove_me] BOOLEAN NOT NULL, [column_2] VARCHAR(255))");

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        public void Process_ShouldCreate_SimpleSelect_If_ColumnToRemoveDoesNotExist()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"ALTER TABLE [test] DROP COLUMN [remove_me]"
            };
            const string expected = @"SELECT 1;";

            _database
                .Setup(x => x.ExecuteScalar(
                    It.Is<DatabaseCommand>(
                        y => y.CommandText == @"SELECT sql FROM sqlite_master WHERE type=@type AND name LIKE @name")))
                .Returns(@"CREATE TABLE [test] ([id] INTEGER PRIMARY KEY, [column_1] INTEGER, [column_2] VARCHAR(255))");

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        public void Process_ShouldHandle_ForeignKeyDefinitions()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"ALTER TABLE [test] DROP COLUMN [remove_me]"
            };
            const string expected =
                @"CREATE TABLE [test_new] ([id] INTEGER PRIMARY KEY, [column_1] INTEGER, [column_2] VARCHAR(255), FOREIGN KEY ([column_1]) REFERENCES [test_2]([id]) ON DELETE SET NULL);INSERT INTO [test_new] SELECT [id],[column_1],[column_2] FROM [test];DROP TABLE [test];ALTER TABLE [test_new] RENAME TO [test];";

            _database
                .Setup(x => x.ExecuteScalar(
                    It.Is<DatabaseCommand>(
                        y => y.CommandText == @"SELECT sql FROM sqlite_master WHERE type=@type AND name LIKE @name")))
                .Returns(
                    @"CREATE TABLE [test] ([id] INTEGER PRIMARY KEY, [column_1] INTEGER, [remove_me] BOOLEAN NOT NULL, [column_2] VARCHAR(255), FOREIGN KEY ([column_1]) REFERENCES [test_2]([id]) ON DELETE SET NULL)");

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }
    }
}