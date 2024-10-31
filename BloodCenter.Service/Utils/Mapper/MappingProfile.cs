using AutoMapper;
using BloodCenter.Data.Dtos.AuthDto;
using BloodCenter.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Service.Utils.Mapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<RegisterDto, Account>()
                .ForMember(dest => dest.UserName, otp => otp.MapFrom(src => src.UserName))
                .ForMember(dest => dest.Email, otp => otp.MapFrom(dst => dst.Email))
                .ForMember(dest => dest.FullName, otp => otp.MapFrom(dest => dest.FullName))
                .ForMember(dest => dest.PasswordHash, otp => otp.Ignore())
                ;
        }
    }
}
