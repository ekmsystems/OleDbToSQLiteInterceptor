using DatabaseConnections;

namespace OleDbToSQLiteInterceptor.Processors
{
    public interface IDatabaseCommandProcessor
    {
        void Process(DatabaseCommand command, IDatabase database);
    }
}
