using System.Data;

namespace EcommerceApp.Infrastructure.Persistence;

public interface IDbConnectionFactory
{
    // Returns a new, unopened connection. Dapper opens it automatically on first use.
    IDbConnection CreateConnection();
}
