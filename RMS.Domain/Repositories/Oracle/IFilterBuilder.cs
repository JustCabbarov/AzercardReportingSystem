using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Repositories.Oracle
{
    public interface IFilterBuilder
    {
        IFilterBuilder Add(string condition, string paramName, object? value);
        IFilterBuilder AddMonth(string column, string paramName, DateTime? value);
        IFilterBuilder AddRaw(string? rawSql);
        IFilterBuilder AddRange(string column, string fromParam, string toParam,
            DateTime? from, DateTime? to);
        string WhereClause { get; }
    }
}
