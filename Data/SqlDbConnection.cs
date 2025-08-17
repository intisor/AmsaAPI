using Microsoft.Data.SqlClient;

namespace AmsaAPI.Data
{
    public class SqlDbConnection
    {
        private readonly string _connectionString = "Server=INTITECH\\SQLEXPRESS;Database=AmsaApiDb;Integrated Security=True;TrustServerCertificate=True;";

        public SqlConnection GetOpenConnection()
        {
            var connection = new SqlConnection(_connectionString);
            connection.Open();
            Console.WriteLine("Connected to SQL Server!");
            return connection;
        }
    }
}