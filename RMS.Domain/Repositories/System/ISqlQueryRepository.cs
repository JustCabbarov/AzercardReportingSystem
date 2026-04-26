using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Repositories.System
{
    public interface ISqlQueryRepository
    {
        Task<string> GetSqlQueryByKeyAsync(string queryKey);
        Task AddOrUpdateSqlQueryAsync(string queryKey, string querySql, string? description = null);
    }
}
