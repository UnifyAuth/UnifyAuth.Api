﻿using AutoMapper;
using Domain.Entities;
using Infrastructure.Common.IdentityModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Common.Mappings
{
    public class IdentityUserProfile : Profile
    {
        public IdentityUserProfile()
        {
            CreateMap<User, IdentityUserModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.Preferred2FAProvider, opt => opt.MapFrom(src => src.Preferred2FAProvider))
                .ForMember(dest => dest.ExternalProvider, opt => opt.MapFrom(src => src.ExternalProvider))
                .ForMember(dest => dest.ExternalProviderId, opt => opt.MapFrom(src => src.ExternalProviderId))
                .ReverseMap();
        }
    }
}
