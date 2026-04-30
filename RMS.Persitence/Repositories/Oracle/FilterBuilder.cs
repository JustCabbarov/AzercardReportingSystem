using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dapper;
namespace RMS.Persitence.Repositories.Oracle
{
    

    
        public class FilterBuilder
        {
            private readonly List<string> _conditions = [];
            private readonly DynamicParameters _param = new();

            public FilterBuilder Add(string condition, string paramName, object? value)
            {
                if (value is null) return this;
                _conditions.Add(condition);
                _param.Add(paramName, value);
                return this;
            }

            public FilterBuilder AddMonth(string column, string paramName, DateTime? value)
            {
                if (value is null) return this;
                _conditions.Add($"TRUNC({column},'MM') = TRUNC(:{paramName},'MM')");
                _param.Add(paramName, value.Value.Date);
                return this;
            }
        public FilterBuilder AddRaw(string? rawSql)
        {
            if (!string.IsNullOrEmpty(rawSql))
                _conditions.Add(rawSql);
            return this;
        }
        public FilterBuilder AddRange(string column, string fromParam, string toParam,
                DateTime? from, DateTime? to)
            {
                if (from is not null)
                {
                    _conditions.Add($"{column} >= :{fromParam}");
                    _param.Add(fromParam, from.Value.Date);
                }
                if (to is not null)
                {
                    _conditions.Add($"{column} <= :{toParam}");
                    _param.Add(toParam, to.Value.Date);
                }
                return this;
            }

            public string WhereClause => _conditions.Count > 0
                ? "WHERE " + string.Join(" AND ", _conditions)
                : "";

            public DynamicParameters Parameters => _param;
        }
    }

