using System.Data;

namespace LeadHype.Api.Core.Database;

public interface IDbConnectionService
{
    Task<IDbConnection> CreateConnectionAsync();
    Task<IDbConnection> GetConnectionAsync();
    Task ExecuteInTransactionAsync(Func<IDbConnection, IDbTransaction, Task> action);
    Task<T> ExecuteInTransactionAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> action);
}