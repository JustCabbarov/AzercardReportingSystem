using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Domain.Entities
{
    // Employee.cs
    public class Employee : BaseEntity
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
       
        public string? Phone { get; set; }
        public bool IsActive { get; set; } = true;

        public Guid? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public Guid? PositionId { get; set; }
        public Position? Position { get; set; }

     
        public Guid AppUserId { get; set; }
        public AppUser AppUser { get; set; }
    }
}
