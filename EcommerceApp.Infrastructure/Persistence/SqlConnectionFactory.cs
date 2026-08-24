using System.Data;
using Microsoft.Data.SqlClient;

namespace EcommerceApp.Infrastructure.Persistence;

public class SqlConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public IDbConnection CreateConnection() => new SqlConnection(connectionString);
}
