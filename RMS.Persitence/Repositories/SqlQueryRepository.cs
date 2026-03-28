using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using RMS.Domain.Repositories;
using RMS.Persitence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Persitence.Repositories
{
    public class SqlQueryRepository : ISqlQueryRepository
    {

        private readonly AppDbContext _context;

        public SqlQueryRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task AddOrUpdateSqlQueryAsync(string queryKey, string querySql, string? description = null)
        {
          
            var existingQuery = _context.SqlQueries.FirstOrDefault(q => q.QueryKey == queryKey);
            if (existingQuery != null)
            {
                existingQuery.QuerySql = querySql;
                existingQuery.Description = description;
                _context.SqlQueries.Update(existingQuery);
            }
            else
            {
                var newQuery = new Domain.Entities.SqlQuery
                {
                    
                    QueryKey = queryKey,
                    QuerySql = querySql,
                    Description = description
                };
                _context.SqlQueries.Add(newQuery);
            }
            return _context.SaveChangesAsync();



        }

        public async Task<string> GetSqlQueryByKeyAsync(string queryKey)
        {
          var sqlQuery =  await _context.SqlQueries.FirstOrDefaultAsync(q => q.QueryKey == queryKey);
            if (sqlQuery == null)
            {
                throw new Exception($"SQL query with key '{queryKey}' not found.");
            }
            return sqlQuery.QuerySql ;
        }
    }
}

