using Microsoft.Extensions.Configuration;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Persitence.Repositories.Oracle
{
    public abstract class OracleRepositoryBase
    {
        private readonly string _connectionString;

        protected OracleRepositoryBase(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("PostgreSqlConnection")
                ?? throw new InvalidOperationException("Connection tapılmadı");
        }

        protected IDbConnection CreateConnection() =>
            new NpgsqlConnection(_connectionString);
    }
}
