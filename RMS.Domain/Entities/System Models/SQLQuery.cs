using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities
{
    public class SqlQuery : BaseEntity
    {
        public string QueryKey { get; set; } = default!;
        public string QuerySql { get; set; } = default!;
        public string? Description { get; set; }
        
    }

}

