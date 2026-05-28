using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities
{
    public class Position : BaseEntity
    {
        public string Name { get; set; } 
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<Employee>? Employees { get; set; } = new List<Employee>();
    }
}
