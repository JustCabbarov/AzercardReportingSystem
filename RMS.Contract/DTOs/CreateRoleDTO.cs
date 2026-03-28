using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Contract.DTOs
{
    public record CreateRoleDTO
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
    }
}
