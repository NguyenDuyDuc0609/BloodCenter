using AutoMapper;
using BloodCenter.Data.DataAccess;
using BloodCenter.Data.Dtos;
using BloodCenter.Data.Dtos.Hospital;
using BloodCenter.Data.Entities;
using BloodCenter.Data.Enums;
using BloodCenter.Service.Cores.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public  HospitalService(BloodCenterContext context, ModelResult result, IMapper mapper, UserManager<Account> userManager, RoleManager<IdentityRole<Guid>> roleManager, IEmailService emailService)
        {
            _context = context;
            _result = result;
            _mapper = mapper;
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
        }

        public async Task<ModelResult> AddNewActivity(ActivityDto activityDto, string id)
        {
            try
            {
                if (activityDto == null)
                    return new ModelResult { Success = false, Message = "Missing parameter" };

                if (string.IsNullOrWhiteSpace(id))
                    return new ModelResult { Success = false, Message = "Hospital ID is required." };

                var newActivity = _mapper.Map<Activity>(activityDto);
                newActivity.Status = StatusActivity.IsWaiting;
                newActivity.NumberIsRegistration = 0;
                newActivity.HospitalId = Guid.Parse(id);

                await _context.Activities.AddAsync(newActivity);
                await _context.SaveChangesAsync();

                return new ModelResult { Success = true, Message = "Create new activity success" };
            }
            catch (DbUpdateException dbEx)
            {
                return new ModelResult { Success = false, Message = "Database error occurred." };
            }
            catch (Exception ex)
            {
                return new ModelResult { Success = false, Message = "An unexpected error occurred." };
            }
        }

    }
}
