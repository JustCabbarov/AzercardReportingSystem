using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using RMS.Domain.Entities.Oracle;
using System.Data;

namespace RMS.Persitence.Repositories.Oracle
{
    public abstract class OracleRepositoryBase
    {
        private readonly string _connectionString;

        protected OracleRepositoryBase(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("PostgreSQLConnection")
                ?? throw new InvalidOperationException("PostgreSQL connection tapılmadı");
        }

        protected IDbConnection CreateConnection() =>
            new NpgsqlConnection(_connectionString);

        protected async Task<PagedResult<T>> QueryPagedAsync<T>(
            string baseSql,
            string orderBy,
            PageRequest pageReq,
            DynamicParameters? param,
            CancellationToken ct)
        {
            var countSql = $"SELECT COUNT(*) FROM ({baseSql}) AS _count";

            var pagedSql = $"""
    SELECT * FROM ({baseSql}) AS _paged
    ORDER BY {orderBy}
    LIMIT @PageSize OFFSET @Offset
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