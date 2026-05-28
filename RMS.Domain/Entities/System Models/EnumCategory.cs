using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities
{
   
    public class EnumCategory : BaseEntity
    {
        public string Name { get; set; } = null!;        
        public string? Description { get; set; }

        public ICollection<EnumValue> EnumValues { get; set; } = new List<EnumValue>();
    }
}
