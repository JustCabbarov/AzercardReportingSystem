
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
            CreateMap<Position, PositionDTO>()
                .ForMember(dest => dest.Employees, opt => opt.MapFrom(src => src.Employees.Select(e => e.Id).ToList())) // Employee Id'lerini alıp listele
                .ReverseMap(); // PositionDTO -> Position dönüşümünü de tanımlıyoruz (eğer gerekliyse)

            // Diğer dönüşümler:
            CreateMap<Department, DepartmentDTO>()
                .ForMember(dest => dest.Employees, opt => opt.MapFrom(src => src.Employees.Select(e => e.Id).ToList()))
                .ReverseMap();

            CreateMap<CreateRoleDTO, AppRole>().ReverseMap();

        }
        
    }
}
