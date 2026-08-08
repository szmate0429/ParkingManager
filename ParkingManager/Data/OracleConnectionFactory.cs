using Oracle.ManagedDataAccess.Client;
using ParkingManager.Data;
using System.Data;

namespace ParkingManager.Data
{

    public class OracleConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public OracleConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection CreateConnection()
        {
            return new OracleConnection(_connectionString);
        }
    }
}