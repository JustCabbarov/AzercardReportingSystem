using System.Collections.Generic;
using Dapper;

namespace RMS.Persitence.Repositories.Oracle
{
    public class FilterBuilder
    {
        private readonly List<string> _conditions = [];
        private readonly DynamicParameters _param = new();

        /// <summary>
        /// Ümumi tip üçün — null olduqda şərt əlavə edilmir.
        /// String üçün AddString istifadə et.
        /// </summary>
        public FilterBuilder Add(string condition, string paramName, object? value)
        {
            if (value is null) return this;
            _conditions.Add(condition);
            _param.Add(paramName, value);
            return this;
        }

        /// <summary>
        /// String sahələr üçün — null və ya boş olduqda şərt əlavə edilmir.
        /// </summary>
        public FilterBuilder AddString(string condition, string paramName, string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return this;
            _conditions.Add(condition);
            _param.Add(paramName, value.Trim());
            return this;
        }

        /// <summary>
        /// Boolean sahələr üçün — null olduqda şərt əlavə edilmir.
        /// Oracle-da boolean adətən NUMBER(1) 1/0 saxlanılır.
        /// </summary>
        public FilterBuilder AddBool(string column, string paramName, bool? value)
        {
            if (value is null) return this;
            _conditions.Add($"{column} = :{paramName}");
            _param.Add(paramName, value.Value ? 1 : 0);
            return this;
        }

        public FilterBuilder AddList(string column, string paramName, List<string>? values)
        {
            if (values is null || values.Count == 0) return this;
            _conditions.Add($"{column} IN :{paramName}");
            _param.Add(paramName, values);
            return this;
        }

        /// <summary>
        /// Ay əsaslı tarix filteri üçün — Oracle TRUNC(..., 'MM') istifadə edir.
        /// </summary>
        public FilterBuilder AddMonth(string column, string paramName, DateTime? value)
        {
            if (value is null) return this;
            _conditions.Add(
                $"TRUNC({column}, 'MM') = TRUNC(:{paramName}, 'MM')");
            _param.Add(paramName, value.Value.Date);
            return this;
        }

        /// <summary>
        /// Hazır SQL şərti birbaşa əlavə etmək üçün.
        /// </summary>
        public FilterBuilder AddRaw(string? rawSql)
        {
            if (!string.IsNullOrEmpty(rawSql))
                _conditions.Add(rawSql);
            return this;
        }

        /// <summary>
        /// Tarix aralığı filteri üçün — from və/və ya to null ola bilər.
        /// </summary>
        public FilterBuilder AddRange(string column,
            string fromParam, string toParam,
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
