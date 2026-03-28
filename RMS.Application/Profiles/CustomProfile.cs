
using AutoMapper;
using RMS.Contract.DTOs;
using RMS.Contract.DTOs.Position;
using RMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Profiles
{
   public class CustomProfile : Profile
    {
        public CustomProfile()
        {
            CreateMap<DepartmentDTO, Department>().ReverseMap();
            CreateMap<PositionDTO, Position>().ReverseMap();
              CreateMap<CreateRoleDTO, AppRole>().ReverseMap();

        }
        
    }
}
