using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Contract.DTOs
{
    public class EditUserDTO
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public Guid? DepartmentId { get; set; }
        public Guid? PositionId { get; set; }
        public string? Email { get; set; }
    }
}
