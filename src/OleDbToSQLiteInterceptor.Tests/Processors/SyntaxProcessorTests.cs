using DatabaseConnections;
using Moq;
using NUnit.Framework;
using OleDbToSQLiteInterceptor.Processors;

namespace OleDbToSQLiteInterceptor.Tests.Processors
{
    [TestFixture]
    [Parallelizable]
    public class SyntaxProcessorTests
    {
        [SetUp]
        public void SetUp()
        {
            _database = new Mock<IDatabase>();
            _processor = new SyntaxProcessor();
        }

        private Mock<IDatabase> _database;
        private SyntaxProcessor _processor;

        [Test]
        public void Process_ShouldFix_AutoIncrement()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"AUTOINCREMENT"
            };
            const string expected = @"INTEGER";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        public void Process_ShouldFix_Booleans()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"IsTrue=true AND IsFalse=false"
            };
            const string expected = @"IsTrue=1 AND IsFalse=0";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        public void Process_ShouldFix_Delete()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"DELETE * FROM [test]"
            };
            const string expected = @"DELETE FROM [test]";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        public void Process_ShouldFix_First()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"First([test])"
            };
            const string expected = @"[test]";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        public void Process_ShouldFix_IFF()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"IIF([price] > 1000, 'expensive', 'cheap')"
            };
            const string expected = @"(CASE WHEN [price] > 1000 THEN 'expensive' ELSE 'cheap' END)";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        public void Process_ShouldFix_IsNull()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"ISNULL([field])"
            };
            const string expected = @"COALESCE([field], 0) = 0";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        public void Process_ShouldFix_LCase()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"LCASE('SomeText')"
            };
            const string expected = @"LOWER('SomeText')";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        public void Process_ShouldFix_Left()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"LEFT('test', 2)"
            };
            const string expected = @"SUBSTR('test', 2)";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        public void Process_ShouldFix_Len()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"LEN('SomeText')"
            };
            const string expected = @"LENGTH('SomeText')";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        public void Process_ShouldFix_NestedRightJoin()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"SELECT [a].[field] FROM [test_1] AS [a] RIGHT JOIN ([test_2] AS [b] RIGHT JOIN [test_3] AS [c] ON [b].[id] = [c].[id]) AS [d] ON [a].[id] = [d].[id]"
            };
            const string expected = @"SELECT [a].[field] FROM ([test_3] AS [c] LEFT JOIN [test_2] AS [b] ON [b].[id] = [c].[id]) AS [d] LEFT JOIN [test_1] AS [a] ON [a].[id] = [d].[id]";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        public void Process_ShouldFix_NestedTop()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"SELECT [name] FROM [test] WHERE [id] IN (SELECT TOP 5 [id] FROM [test_2] WHERE [price] > 10)"
            };
            const string expected = @"SELECT [name] FROM [test] WHERE [id] IN (SELECT [id] FROM [test_2] WHERE [price] > 10 LIMIT 5)";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        public void Process_ShouldFix_Now()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"NOW()"
            };
            const string expected = @"date('now', 'localtime')";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        public void Process_ShouldFix_RightJoin()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"SELECT [a].[field] FROM [test_1] AS [a] RIGHT JOIN [test_2] AS [b] ON [a].[id] = [b].[id]"
            };
            const string expected = @"SELECT [a].[field] FROM [test_2] AS [b] LEFT JOIN [test_1] AS [a] ON [a].[id] = [b].[id]";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        public void Process_ShouldFix_StrComp()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"STRCOMP('this is a test', 'test', 1) > 0"
            };
            const string expected = @"INSTR(LOWER('this is a test'), 'test') > 1";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        public void Process_ShouldFix_Top()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"SELECT TOP 5 * FROM [test]"
            };
            const string expected = @"SELECT * FROM [test] LIMIT 5";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        public void Process_ShouldFix_UCase()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"UCASE('SomeText')"
            };
            const string expected = @"UPPER('SomeText')";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }
    }
}