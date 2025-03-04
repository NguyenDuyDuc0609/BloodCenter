using AutoMapper;
using BloodCenter.Data.DataAccess;
using BloodCenter.Data.Dtos;
using BloodCenter.Data.Dtos.Hospital;
using BloodCenter.Data.Entities;
using BloodCenter.Data.Enums;
using BloodCenter.Service.Cores.Interface;
using BloodCenter.Service.Utils.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace BloodCenter.Service.Cores
{
    public class HospitalService : IHospital
    {
        private readonly BloodCenterContext _context;
        private ModelResult _result;
        private IMapper _mapper;
        private readonly UserManager<Account> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private IEmailService _emailService;
        private readonly IConfiguration _config;
        public  HospitalService(BloodCenterContext context, IMapper mapper, UserManager<Account> userManager, RoleManager<IdentityRole<Guid>> roleManager, IEmailService emailService, IConfiguration config)
        {
            _context = context;
            _mapper = mapper;
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _result = new ModelResult();
            _config = config;
        }

        public async Task<ModelResult> AddNewActivity(ActivityDto activityDto, string token)
        {
            try
            {
                if (activityDto == null)
                    return new ModelResult { Success = false, Message = "Missing parameter" };

                if (string.IsNullOrWhiteSpace(token))
                    return new ModelResult { Success = false, Message = "Hospital ID is required." };
                if (token.StartsWith("Bearer "))
                {
                    token = token.Substring("Bearer ".Length).Trim();
                }
                var principal = Jwt.GetClaimsPrincipalToken(token, _config);
                var hospital = await _userManager.FindByNameAsync(principal.Identity.Name);
                if (hospital == null)
                {
                    return new ModelResult { Success = false, Message = "Hospital is not exits" };
                }
                var newActivity = _mapper.Map<Activity>(activityDto);
                newActivity.Status = StatusActivity.IsWaiting;
                newActivity.NumberIsRegistration = 0;
                newActivity.HospitalId = hospital.Id;

                await _context.Activities.AddAsync(newActivity);
                await _context.SaveChangesAsync();

                return new ModelResult { Success = true, Message = "Create new activity success" };
            }
            catch (DbUpdateException dbEx)
            {
                return new ModelResult { Success = false, Message = dbEx.ToString() };
            }
            catch (Exception ex)
            {
                return new ModelResult { Success = false, Message = ex.ToString() };
            }
        }

    }
}
