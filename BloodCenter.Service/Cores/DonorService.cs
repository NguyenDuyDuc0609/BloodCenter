using AutoMapper;
using BloodCenter.Data.Constracts;
using BloodCenter.Data.DataAccess;
using BloodCenter.Data.Dtos;
using BloodCenter.Data.Dtos.Donor;
using BloodCenter.Data.Entities;
using BloodCenter.Service.Cores.Interface;
using BloodCenter.Service.Utils.Auth;
using BloodCenter.Service.Utils.Redis.Cache;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        private readonly IAuthRedisCacheService _cache;
        private readonly IPublishEndpoint _publishEndpoint;
        
        public DonorService(BloodCenterContext context, IMapper mapper, UserManager<Account> userManager,
            RoleManager<IdentityRole<Guid>> roleManager, IEmailService emailService, IConfiguration config,
            IAuthRedisCacheService redis, IPublishEndpoint publishEndpoint)
        {
            _context = context;
            _mapper = mapper;
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _result = new ModelResult();
            _config = config;
            _cache = redis;
            _publishEndpoint = publishEndpoint;

        }
        private async Task<ModelResult> ValidateAndGetActivity(string token, string activity, BloodCenterContext bloodCenterContext)
        {
            if (token.StartsWith("Bearer "))
            {
                token = token.Substring("Bearer ".Length).Trim();
            }
            if (string.IsNullOrEmpty(token))
                return new ModelResult { Success = false, Message = "Please login" };

            if (string.IsNullOrEmpty(activity))
                return new ModelResult { Success = false, Message = "Please choose an activity that is ongoing or waiting" };

            if (token.StartsWith("Bearer "))
                token = token.Substring("Bearer ".Length).Trim();

            var principal = Jwt.GetClaimsPrincipalToken(token, _config);
            if (principal?.Identity?.Name == null)
                return new ModelResult { Success = false, Message = "Invalid token" };

            var donor = await _userManager.FindByNameAsync(principal.Identity.Name);
            if (donor == null)
                return new ModelResult { Success = false, Message = "User does not exist" };

            if (!Guid.TryParse(activity, out var activityId))
                return new ModelResult { Success = false, Message = "Invalid activity ID format" };

            var activityIsGoing = await bloodCenterContext.Activities
                .FromSqlRaw(@"SELECT * FROM ""Activities"" WHERE ""Id"" = {0} ORDER BY ""CreatedDate""", Guid.Parse(activity))
                .FirstOrDefaultAsync();


            if (activityIsGoing == null)
                return new ModelResult { Success = false, Message = "Activity is not valid" };

            return new ModelResult { Success = true, Data = new ActivityValidationResult { Donor = donor, Activity = activityIsGoing } };

        }
        private ModelResult CheckActivityConditions(Activity activityIsGoing)
        {
            if (activityIsGoing.Quantity == 0)
                return new ModelResult { Success = false, Message = "Activity is not accepting registrations" };

            if (activityIsGoing.NumberIsRegistration >= activityIsGoing.Quantity)
                return new ModelResult { Success = false, Message = "Activity is full" };

            return new ModelResult { Success = true };
        }

        private async Task<ModelResult> CheckLastDonation(Guid donorId)
        {
            var lastDonation = await _context.Histories
                .FromSqlRaw(@"SELECT * FROM ""Histories"" WHERE ""DonorId"" = {0} and (""StatusHistories"" = 0 or ""StatusHistories"" = 1) ORDER BY ""CreatedDate"" DESC LIMIT 1", donorId)
                .FirstOrDefaultAsync();

            if (lastDonation != null && lastDonation.CreatedDate.AddDays(90) > DateTime.UtcNow)
            {
                return new ModelResult { Success = false, Message = "Less than 3 months since last blood donation" };
            }

            return new ModelResult { Success = true, Data = lastDonation };
        }
        private void AddOrUpdateSessionDonor(Guid donorId, Guid activityId, BloodCenterContext bloodCenterContext)
        {
            var sessionDonor = bloodCenterContext.SessionDonors
                .FromSqlRaw(@"SELECT * FROM ""SessionDonors"" WHERE ""ActivityId"" = {0} AND ""DonorId"" = {1}", activityId, donorId)
                .FirstOrDefault();

            if (sessionDonor == null)
            {
                sessionDonor = new SessionDonor
                {
                    DonorId = donorId,
                    ActivityId = activityId,
                    Status = Data.Enums.StatusSession.IsWaitingDonor
                };
                bloodCenterContext.SessionDonors.Add(sessionDonor);
            }
            else
            {
                sessionDonor.Status = Data.Enums.StatusSession.IsWaitingDonor;
                bloodCenterContext.SessionDonors.Update(sessionDonor);
            }
        }


        public async Task<ModelResult> CancelRegistration(string token, string activity)
        {
            try
            {
                if (token.StartsWith("Bearer "))
                {
                    token = token.Substring("Bearer ".Length).Trim();
                }
                var validation = await ValidateAndGetActivity(token, activity, _context);
                if (!validation.Success)
                    return validation;

                if (validation.Data is not ActivityValidationResult result)
                    return new ModelResult { Success = false, Message = "Data format is invalid" };

                var donor = result.Donor;
                var activityIsGoing = result.Activity;

                var history = await _context.Histories
                    .FirstOrDefaultAsync(x => x.DonorId == donor.Id && x.ActivityId == activityIsGoing.Id);

                if (history == null)
                    return new ModelResult { Success = false, Message = "History not found" };

                history.StatusHistories = Data.Enums.StatusHistories.Cancel;

                if (activityIsGoing.NumberIsRegistration > 0)
                    activityIsGoing.NumberIsRegistration -= 1;

                var sessionDonor = await _context.SessionDonors
                    .FirstOrDefaultAsync(x => x.ActivityId == activityIsGoing.Id && x.DonorId == donor.Id);

                if (sessionDonor != null)
                {
                    sessionDonor.Status = Data.Enums.StatusSession.Cancel;
                    _context.SessionDonors.Update(sessionDonor);
                }

                _context.Histories.Update(history);
                _context.Activities.Update(activityIsGoing);
                await _publishEndpoint.Publish(new UpdateCache { message = "Cancel register" });
                await _context.SaveChangesAsync();

                return new ModelResult { Success = true, Message = "Cancel success" };
            }
            catch (Exception ex)
            {
                return new ModelResult { Success = false, Message = "An error occurred. Please try again later." };
            }
        }

        public async Task<ModelResult> GetPersonalHistory(string token, int pageNumber, int pageSize)
        {
            try
            {
                if (token.StartsWith("Bearer "))
                {
                    token = token.Substring("Bearer ".Length).Trim();
                }
                if (string.IsNullOrEmpty(token))
                    return new ModelResult { Success = false, Message = "Please login" };
                if (token.StartsWith("Bearer "))
                    token = token.Substring("Bearer ".Length).Trim();

                var principal = Jwt.GetClaimsPrincipalToken(token, _config);
                if (principal?.Identity?.Name == null)
                    return new ModelResult { Success = false, Message = "Invalid token" };
                var donor = await _userManager.FindByNameAsync(principal.Identity.Name);
                if (donor == null)
                    return new ModelResult { Success = false, Message = "User does not exist" };
                var list = await _context.Histories.FromSqlRaw(@"Select * from ""Histories"" where ""DonorId"" = {0} order by ""CreatedDate"" DESC offset {1} limit {2}", donor.Id, (pageNumber-1)*pageSize, pageSize)
                    .ToListAsync();
                var totalCount = list.Count;
                return new ModelResult { Data = list, Success = true, Message = "Get hisroty success", TotalCount = totalCount};
            }
            catch (Exception ex) 
            {
                return new ModelResult { Success = false, Message = ex.ToString() };
            }
        }

        public async Task<ModelResult> GetActivityIsGoing(int pageNumber, int pageSize, int status)
        {
            try
            {
                if(pageNumber < 1 || pageSize < 1) return new ModelResult { Success = false, Message = "Page number or page size is not valid" };

                var (activities, totalCount) = await _cache.GetPageActivitiesAsync(pageNumber, pageSize);

                if (activities == null || totalCount == 0)
                {
                    activities = await _context.Activities
                            .Where(a => a.Status == Data.Enums.StatusActivity.IsGoing)
                            .OrderByDescending(a => a.CreatedDate)
                            .ToListAsync();

                    if (activities.Count > 0)
                    {
                        await _cache.SaveActivityListAsync(activities);
                    }
                    totalCount = activities.Count;
                    var activitiesPage = activities.Skip((pageNumber - 1) * pageSize)
                                                    .Take(pageSize)
                                                    .ToList();
                    return new ModelResult { Success = false, Data = activitiesPage, Message = "Data from Database", TotalCount = totalCount };
                }
                else
                {

                    return new ModelResult { Success = false, Data = activities, Message = "Data from Cache", TotalCount = totalCount };
                }
            }
            catch (Exception ex) {
                return new ModelResult { Success = false, Message = ex.ToString() };
            }
        }   

        public async Task<ModelResult> RegisterDonate(string token, string activity)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    if (token.StartsWith("Bearer "))
                    {
                        token = token.Substring("Bearer ".Length).Trim();
                    }
                    var validation = await ValidateAndGetActivity(token, activity, _context);
                    if (!validation.Success)
                        return validation;

                    if (validation.Data is not ActivityValidationResult result)
                        return new ModelResult { Success = false, Message = "Data format is invalid" };

                    var donor = result.Donor;
                    var activityIsGoing = result.Activity;

                    var activityCheck = CheckActivityConditions(activityIsGoing);
                    if (!activityCheck.Success)
                        return activityCheck;

                    var lastDonationCheck = await CheckLastDonation(donor.Id);
                    if (!lastDonationCheck.Success)
                        return lastDonationCheck;

                    activityIsGoing.NumberIsRegistration += 1;
                    _context.Activities.Update(activityIsGoing);
                    var hospital = await _context.Hospitals
                        .Include(h => h.Account) 
                        .Where(x => x.Id == activityIsGoing.HospitalId)
                        .FirstAsync();

                    var donation = new History
                    {
                        DonorId = donor.Id,
                        Quantity = activityIsGoing.Quantity,
                        HospitalId = activityIsGoing.HospitalId,
                        HospitalName = hospital.Account.FullName,
                        ActivityId = activityIsGoing.Id,
                        DonationDate = activityIsGoing.DateActivity,
                        StatusHistories = Data.Enums.StatusHistories.Waiting
                    };
                    _context.Histories.Add(donation);

                    AddOrUpdateSessionDonor(donor.Id, activityIsGoing.Id, _context);
                    await _publishEndpoint.Publish(new UpdateCache { message = "Update cache" });
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new ModelResult { Success = true, Data = lastDonationCheck.Data, Message = "Register success" };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return new ModelResult { Success = false, Message = ex.ToString() };
                }
            }
        }

        public async Task<ModelResult> DonorInformation(string token)
        {
            try
            {
                if (token.StartsWith("Bearer "))
                {
                    token = token.Substring("Bearer ".Length).Trim();
                }
                var principal = Jwt.GetClaimsPrincipalToken(token, _config);
                if (principal?.Identity?.Name == null)
                    return new ModelResult { Success = false, Message = "Invalid token" };
                var user = await _context.Accounts.Include(x => x.Donor).Where(x => x.UserName == principal.Identity.Name).FirstAsync();
                var data = new InformationDto
                {
                    FullName = user?.FullName,
                    Email = user?.Email,
                    Note = user?.Note,
                    StatusAccount = user?.StatusAccount,
                    PhoneNumber = user?.PhoneNumber,
                    Username = user?.UserName,
                };
                return new ModelResult { Message = "Get information sucess", Data = data, Success = true};
            }
            catch (Exception ex)
            {
                return new ModelResult { Message = ex.ToString(), Success = true };
            }

        }

        public async Task<ModelResult> ChangeInformation(string token, InformationDto informationDto)
        {
            try
            {
                if (token.StartsWith("Bearer "))
                {
                    token = token.Substring("Bearer ".Length).Trim();
                }
                var principal = Jwt.GetClaimsPrincipalToken(token, _config);
                if (principal?.Identity?.Name == null)
                    return new ModelResult { Success = false, Message = "Invalid token" };
                var user = await _context.Accounts.Include(x => x.Donor).Where(x => x.UserName == principal.Identity.Name).FirstAsync();
                user.Email = informationDto.Email;
                user.FullName = informationDto.FullName;
                user.PhoneNumber = informationDto.PhoneNumber;
                user.UserName = informationDto.Username;
                await _context.SaveChangesAsync();
                var data = new InformationDto
                {
                    FullName = user.FullName,
                    Email = user.Email,
                    Note = user.Note,
                    StatusAccount = user.StatusAccount,
                    PhoneNumber = user.PhoneNumber,
                };
                return new ModelResult { Success = true, Message = "Change information sucess", Data = data };
            }
            catch (Exception ex) {
                return new ModelResult { Message = ex.ToString(), Success = false };
            }
        }
    }
}
