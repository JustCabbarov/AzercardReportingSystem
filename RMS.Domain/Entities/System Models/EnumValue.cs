using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities
{
    // EnumValue.cs
    public class EnumValue : BaseEntity
    {
        public string Name { get; set; } = null!;        // "Increase", "Decrease", "Both" vs
        public string? Description { get; set; }
        public int OrderIndex { get; set; }
        public bool IsActive { get; set; } = true;

        public Guid EnumCategoryId { get; set; }
        public EnumCategory? EnumCategory { get; set; }
    }
}
