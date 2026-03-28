using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Contract.DTOs
{
    public record AssignRoleDTO
    {
        public Guid UserId { get; set; }
        public string RoleName { get; set; } = null!;
    }
}
