using System;
using System.Collections.Generic;
using System.Text;

namespace RMS.Contract.DTOs
{
    public record RegisterDTO
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string? Phone { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public Guid DepartmentId { get; set; }
        public Guid PositionId { get; set; }
    }
}