using AutoMapper;
using BloodCenter.Data.DataAccess;
using BloodCenter.Data.Dtos;
using BloodCenter.Data.Entities;
using BloodCenter.Service.Cores.Interface;
using BloodCenter.Service.Utils.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Service.Cores
{
    public class DonorService : IDonor
    {
        private readonly BloodCenterContext _context;
        private ModelResult _result;
        private IMapper _mapper;
        private readonly UserManager<Account> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private IEmailService _emailService;
        private readonly IConfiguration _config;
        public DonorService(BloodCenterContext context, IMapper mapper, UserManager<Account> userManager, RoleManager<IdentityRole<Guid>> roleManager, IEmailService emailService, IConfiguration config)
        {
            _context = context;
            _mapper = mapper;
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _result = new ModelResult();
            _config = config;
        }
        public Task<ModelResult> CancelRegistration(string token, string activity)
        {
            throw new NotImplementedException();
        }


        public Task<ModelResult> GetPersonalHistory(string token)
        {
            throw new NotImplementedException();
        }

        public async Task<ModelResult> GetActivityIsGoing(int pageNumber, int pageSize, int status)
        {
            try
            {
                if(pageNumber < 1 || pageSize < 1) return new ModelResult { Success = false, Message = "Page number or page size is not valid" };
                var listActivities = await _context.Activities
                        .FromSqlRaw(@"SELECT * FROM ""Activities"" WHERE ""Status"" = 1 ORDER BY ""CreateDate"" DESC OFFSET {0} LIMIT {1};",
                            (pageNumber - 1) * pageSize, pageSize)
                        .ToListAsync();
                return new ModelResult { Success = true, Data = listActivities, Message = "Get activities success"};
            }
            catch (Exception ex) {
                return new ModelResult { Success = false, Message = ex.ToString() };
            }
        }

        public async Task<ModelResult> RegisterDonate(string token, string activity)
        {
            try
            {
                if (string.IsNullOrEmpty(token)) return new ModelResult { Success = false, Message = "Please login" };
                if (string.IsNullOrEmpty(activity)) return new ModelResult { Success = false, Message = "Please choose activity is going or waiting" };
                if (token.StartsWith("Bearer "))
                {
                    token = token.Substring("Bearer ".Length).Trim();
                }
                var principal = Jwt.GetClaimsPrincipalToken(token, _config);
                var donor = await _userManager.FindByNameAsync(principal.Identity.Name); 
                if(donor == null) return new ModelResult { Success = false, Message = "User is not already exits" };
                var lastDonation = await _context.Histories.FromSqlRaw(@"Select * from ""Histories"" where ""DonorId"" = {0} order by ""CreateDate"" desc;", donor.Id).LastOrDefaultAsync();
                var activityIsGoing = await _context.Activities
                                    .FromSqlRaw(@"SELECT * FROM ""Activities"" WHERE ""Id"" = {0}::uuid;", Guid.Parse(activity))
                                    .FirstOrDefaultAsync();
                if(activityIsGoing == null) return new ModelResult { Success = false, Message = "Activity is not valid" };
                if (lastDonation != null && lastDonation.CreatedDate >= activityIsGoing.DateActivity.AddDays(-90))
                {
                    return new ModelResult { Success = false, Message = "Less than 3 months since last blood donation" };
                }
                activityIsGoing.Quantity += 1;
                
                return new ModelResult { Success = true, Message = "Register success" };


            }
            catch (Exception ex) 
            {
                return new ModelResult { Success = false, Message = ex.ToString() };
            }
        }
    }
}
