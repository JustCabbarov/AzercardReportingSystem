using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Contract.DTOs.Position
{
    public class PositionDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
            public bool IsActive { get; set; }
        public List<Guid>? Employees { get; set; } 

    }
}
