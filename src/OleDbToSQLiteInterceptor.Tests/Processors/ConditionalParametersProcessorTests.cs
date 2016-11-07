using DatabaseConnections;
using Moq;
using NUnit.Framework;
using OleDbToSQLiteInterceptor.Processors;

namespace OleDbToSQLiteInterceptor.Tests.Processors
{
    [TestFixture]
    [Parallelizable]
    public class ConditionalParametersProcessorTests
    {
        [SetUp]
        public void SetUp()
        {
            _database = new Mock<IDatabase>();
            _processor = new ConditionalParametersProcessor();
        }
        
        private Mock<IDatabase> _database;
        private ConditionalParametersProcessor _processor;

        [Test]
        [TestCase("FROM [test] ON (SELECT [x] FROM [test_2] WHERE [myvar]='x')")]
        [TestCase("WHERE [test].[condition] LIKE '%new%'")]
        [TestCase("WHERE [test].[condition] NOT LIKE '%new%'")]
        [TestCase("WHERE [test].[brand] IN ('ekm', 'powershop')")]
        [TestCase("WHERE [test].[brand] NOT IN ('ekm', 'powershop')")]
        [TestCase("WHERE CASE WHEN [test].[price] > 10 THEN 'x' ELSE 'y' END")]
        [TestCase("WHERE [test].[description] IS NULL")]
        public void Process_Given_ConditionThatShouldNotBeProcessed_ShouldNot_ModifyCommandText(string commandText)
        {
            var command = new DatabaseCommand
            {
                CommandText = commandText
            };

            _processor.Process(command, _database.Object);

            Assert.AreEqual(commandText, command.CommandText);
        }

        [Test]
        public void Process_Given_ConditionWrappedInParenthesis_ShouldHandle_WrappingCoalesce()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"WHERE ([test].[column]=@value)"
            };
            const string expected = @"WHERE (COALESCE([test].[column], 0) = @value)";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        [TestCase("=")]
        [TestCase(">=")]
        [TestCase("<=")]
        public void Process_Given_DifferentEqualityOperators_ShouldHandle_PassingOperatorToCoalesce(
            string equalityOperator)
        {
            var command = new DatabaseCommand
            {
                CommandText = string.Format("WHERE [test].[column] {0} @value", equalityOperator)
            };
            var expected = string.Format("WHERE COALESCE([test].[column], 0) {0} @value", equalityOperator);

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        public void Process_Given_IsNotOperator_ShouldConvertTo_NotEqualOperator()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"WHERE [test].[column] IS NOT @value"
            };
            const string expected = @"WHERE COALESCE([test].[column], @value) <> @value";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        public void Process_Given_NotEqualOperator_ShouldHave_ValueInCoalesce()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"WHERE [test].[column] <> @value"
            };
            const string expected = @"WHERE COALESCE([test].[column], @value) <> @value";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        public void Process_ShouldConvert_Null_To_Zero()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"WHERE [test].[description]=NULL"
            };
            const string expected = @"WHERE COALESCE([test].[description], 0) = 0";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        [TestCase("WHERE ([test].[price]) > 20")]
        [TestCase("WHERE (([test].[price])) > 20")]
        [TestCase("WHERE (((((([test].[price])))))) > 20")]
        public void Process_ShouldHandle_ColumnsWrappedInParenthesis(string conditional)
        {
            var command = new DatabaseCommand
            {
                CommandText = conditional
            };
            const string expected = @"WHERE COALESCE([test].[price], 0) > 20";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        public void Process_ShouldHandle_ConditionalsAndColumnsWrappedInParenthesis()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"WHERE ((([test].[price]) > 20))"
            };
            const string expected = @"WHERE ((COALESCE([test].[price], 0) > 20))";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        public void Process_ShouldHandle_DeepNestedQueries()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"WHERE [x].[id] IN (SELECT [y].[id] FROM [y] WHERE [y].[price] > 20 AND [y].[category_id] IN (SELECT [z].[id] FROM [z] WHERE [z].[department]='Electronics'))"
            };
            const string expected =
                @"WHERE [x].[id] IN (SELECT [y].[id] FROM [y] WHERE COALESCE([y].[price], 0) > 20 AND [y].[category_id] IN (SELECT [z].[id] FROM [z] WHERE COALESCE([z].[department], 0) = 'Electronics'))";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        public void Process_ShouldHandle_MultipleConditions()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"WHERE [test].[price] > 20 AND [test].[name] LIKE '%car%' AND [test].[condition] IS NOT 'used'"
            };
            const string expected =
                @"WHERE COALESCE([test].[price], 0) > 20 AND [test].[name] LIKE '%car%' AND COALESCE([test].[condition], 'used') <> 'used'";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        public void Process_ShouldHandle_NestedQueries()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"WHERE [test].[id] IN (SELECT [test_2].[id] FROM [test_2] WHERE [test_2].[price] > 20)"
            };
            const string expected =
                @"WHERE [test].[id] IN (SELECT [test_2].[id] FROM [test_2] WHERE COALESCE([test_2].[price], 0) > 20)";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        public void Process_ShouldHandle_WrappedConditionals()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"WHERE ((([test].[price] > 20) AND ([test].[price] < 50) AND [test].[name] LIKE '%car%')"
            };
            const string expected =
                @"WHERE (((COALESCE([test].[price], 0) > 20) AND (COALESCE([test].[price], 0) < 50) AND [test].[name] LIKE '%car%')";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }

        [Test]
        public void Process_ShouldWrap_ConditionalsInCoalesce()
        {
            var command = new DatabaseCommand
            {
                CommandText = @"WHERE [test].[id]=@id"
            };
            const string expected = @"WHERE COALESCE([test].[id], 0) = @id";

            _processor.Process(command, _database.Object);

            Assert.AreEqual(expected, command.CommandText);
        }
    }
}