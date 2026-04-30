using Dapper;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using RMS.Domain.Entities.Oracle;
using System.Data;

namespace RMS.Persitence.Repositories.Oracle
{
    public abstract class OracleRepositoryBase
    {
        private readonly string _connectionString;

        protected OracleRepositoryBase(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("OracleConnection")
                ?? throw new InvalidOperationException("Oracle connection tapılmadı");
        }

        protected IDbConnection CreateConnection() =>
            new OracleConnection(_connectionString);



        protected async Task<PagedResult<T>> QueryPagedAsync<T>(
    string baseSql,
    string orderBy,
    PageRequest pageReq,
    DynamicParameters? param,
    CancellationToken ct)
        {
            var countSql = $"SELECT COUNT(*) FROM ({baseSql})";

            var pagedSql = $"""
    SELECT * FROM ({baseSql})
    ORDER BY {orderBy}
    OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY
    """;

            var dynParam = new DynamicParameters(param);
            dynParam.Add("Offset", (pageReq.Page - 1) * pageReq.PageSize);
            dynParam.Add("PageSize", pageReq.PageSize);

            using var conn = CreateConnection();

            var totalCount = await conn.ExecuteScalarAsync<int>(
                new CommandDefinition(countSql, param, cancellationToken: ct));

            var items = await conn.QueryAsync<T>(
                new CommandDefinition(pagedSql, dynParam, cancellationToken: ct));

            return new PagedResult<T>
            {
                Items = items,
                TotalCount = totalCount,
                Page = pageReq.Page,
                PageSize = pageReq.PageSize
            };
        }
    }
}