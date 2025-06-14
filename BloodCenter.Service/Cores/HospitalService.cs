﻿using AutoMapper;
using BloodCenter.Data.DataAccess;
using BloodCenter.Data.Dtos;
using BloodCenter.Data.Dtos.Hospital;
using BloodCenter.Data.Entities;
using BloodCenter.Data.Enums;
using BloodCenter.Service.Cores.Interface;
using BloodCenter.Service.Utils.Auth;
using MassTransit;
using MassTransit.NewIdProviders;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Asn1.X509;
using System.Net.WebSockets;
using System.Reflection.Metadata.Ecma335;

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

        private async Task<ModelResult> ValidateHospital(string token, string id)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                    return new ModelResult { Success = false, Message = "Please login" };
                if (token.StartsWith("Bearer "))
                    token = token.Substring("Bearer ".Length).Trim();

                var principal = Jwt.GetClaimsPrincipalToken(token, _config);
                if (principal?.Identity?.Name == null)
                    return new ModelResult { Success = false, Message = "Invalid token" };
                var hospital = await _userManager.FindByNameAsync(principal.Identity.Name);
                if (hospital == null) return new ModelResult { Success = false, Message = "hospital not found" };

                if (string.IsNullOrEmpty(id))
                    return new ModelResult { Success = false, Message = "Please choose an activity that is ongoing or waiting" };
                if (!Guid.TryParse(id, out var activityId))
                    return new ModelResult { Success = false, Message = "Invalid activity ID format" };

                var activity = await _context.Activities
                    .FromSqlRaw(@"SELECT * FROM ""Activities"" WHERE ""Id"" = {0} ORDER BY ""CreatedDate""", Guid.Parse(id))
                    .FirstOrDefaultAsync();
                return new ModelResult { Success = true, Data = new ActivityValidationResult { Activity = activity, Donor = hospital } };
            }
            catch (Exception ex)
            {
                return new ModelResult { Success = false, Message = ex.ToString() };
            }
        }
        //private async Task<ModelResult> DeleteSession(Guid activityId, BloodCenterContext _bloodCenterContext)
        //{
        //    try
        //    {
        //        var hasSession = await _bloodCenterContext.SessionDonors
        //            .FromSqlRaw(@"SELECT * FROM ""SessionDonors"" WHERE ""ActivityId"" = {0} LIMIT 1", activityId)
        //            .AsNoTracking()
        //            .FirstOrDefaultAsync();

        //        if (hasSession == null)
        //            return new ModelResult { Success = false, Message = "No sessions found for this activity" };

        //        //await _bloodCenterContext.Database.ExecuteSqlRawAsync(@"
        //        //        UPDATE ""SessionDonors""
        //        //        SET ""Status"" = {0}
        //        //        WHERE ""ActivityId"" = {1}", (int)StatusSession.Cancel, activityId);
        //        var list = await _bloodCenterContext.SessionDonors.Where(x => x.ActivityId == activityId).ToListAsync();
        //        foreach (var item in list) {
        //           item.Status = StatusSession.Cancel;
        //        }
        //        await _bloodCenterContext.SaveChangesAsync();
        //        return new ModelResult { Success = true, Message = "Change status session success" };
        //    }
        //    catch (Exception ex)
        //    {
        //        return new ModelResult { Success = false, Message = ex.Message };
        //    }
        //}
        //private async Task<ModelResult> EditHistory(Guid activityId, BloodCenterContext _bloodCenterContext)
        //{
        //    try
        //    {
        //        var hasHistories = await _bloodCenterContext.Histories
        //            .FromSqlRaw(@"SELECT * FROM ""Histories"" WHERE ""ActivityId"" = {0} LIMIT 1", activityId)
        //            .AsNoTracking()
        //            .FirstOrDefaultAsync();

        //        if (hasHistories == null)
        //            return new ModelResult { Success = false, Message = "No sessions found for this activity" };

        //        //await _bloodCenterContext.Database.ExecuteSqlRawAsync(@"
        //        //        UPDATE ""Histories""
        //        //        SET ""Status"" = {0}
        //        //        WHERE ""ActivityId"" = {1}", (int)StatusHistories.Cancel, activityId);
        //        var list = await _bloodCenterContext.Histories.Where(x => x.ActivityId == activityId).ToListAsync();
        //        foreach (var item in list) {
        //            item.StatusHistories = StatusHistories.Cancel;
        //        }
        //        await  _bloodCenterContext.SaveChangesAsync();
        //        return new ModelResult { Message = "Change status histories sucess", Success = true };
        //    }
        //    catch (Exception ex)
        //    {
        //        return new ModelResult { Success = false, Message = ex.ToString() };
        //    }
        //}
        private static async Task<ModelResult> ChangeStatus<T>(Guid activityId, BloodCenterContext context, StatusSession? newSessionStatus = null, StatusHistories? newHistoryStatus = null) where T : class
        {
            try
            {
                var list = await context.Set<T>()
                    .Where(x => EF.Property<Guid>(x, "ActivityId") == activityId)
                    .ToListAsync();

                if (!list.Any())
                    return new ModelResult { Success = false, Message = "No records found for this activity" };

                foreach (var item in list)
                {
                    if (item is SessionDonor sessionDonor && newSessionStatus.HasValue)
                        sessionDonor.Status = newSessionStatus.Value;

                    if (item is History history && newHistoryStatus.HasValue)
                        history.StatusHistories = newHistoryStatus.Value;
                }

                await context.SaveChangesAsync();
                return new ModelResult { Success = true, Message = "Status updated successfully" };
            }
            catch (Exception ex)
            {
                return new ModelResult { Success = false, Message = ex.Message };
            }
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
                var newActivity = _mapper.Map<Data.Entities.Activity>(activityDto);
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

        public Task<ModelResult> EditActivity(ActivityDto activityDto, string id)
        {
            throw new NotImplementedException();
        }

        public async Task<ModelResult> CancelActivity(string token, string id)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync()) {
                try
                {
                    var validation = await ValidateHospital(token, id);
                    if (validation.Data is not ActivityValidationResult result)
                        return new ModelResult { Success = false, Message = "Data format is invalid" };
                    var hospital = result.Donor;
                    var activity = result.Activity;
                    if (hospital.Id != activity.HospitalId) return new ModelResult { Success = false, Message = "This is another hospital activity" };
                    if (activity.Status == StatusActivity.Cancel) return new ModelResult { Success = false, Message = "Activity cancelled" };
                    activity.Status = StatusActivity.Cancel;
                    await _context.SaveChangesAsync();
                    var resultSession = await ChangeStatus<SessionDonor>(activity.Id, _context, newSessionStatus: StatusSession.Cancel);
                    var resultHistory = await ChangeStatus<History>(activity.Id, _context, newHistoryStatus: StatusHistories.Cancel);
                    if (resultSession.Success == false || resultHistory.Success == false)
                    {
                        await transaction.RollbackAsync();
                        return new ModelResult { Success = false, Message = resultSession.Message };
                    }
                    await transaction.CommitAsync();
                    return new ModelResult { Success = true, Message = "Cancelled activity" };

                }
                catch (Exception ex) 
                {
                    await transaction.RollbackAsync();
                    return new ModelResult
                    {
                        Success = false,
                        Message = ex.ToString()
                    };
                }
            }
        }

        public async Task<ModelResult> EndActivity(string token, string id)
        {
            try
            {
                var validation = await ValidateHospital(token, id);
                if (validation.Data is not ActivityValidationResult result)
                    return new ModelResult { Success = false, Message = "Data format is invalid" };
                var hospital = result.Donor;
                var activity = result.Activity;
                if (hospital.Id != activity.HospitalId) return new ModelResult { Success = false, Message = "This is another hospital activity" };
                activity.Status = StatusActivity.Done;
                await _context.SaveChangesAsync();
                return new ModelResult { Success = true, Message = "Activity ended" };
            }
            catch (Exception ex)
            {
                    return new ModelResult { Success = false, Message = ex.ToString() };
            }
        }
        public async Task<ModelResult> ComfirmDonor(string token, string activityId, string id)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var validation = await ValidateHospital(token, activityId);
                    if (validation.Data is not ActivityValidationResult result)
                        return new ModelResult { Success = false, Message = "Data format is invalid" };

                    var hospital = result.Donor;
                    var activity = result.Activity;

                    if (hospital.Id != activity.HospitalId)
                        return new ModelResult { Success = false, Message = "This is another hospital activity" };

                    if (activity.Status == StatusActivity.Cancel)
                        return new ModelResult { Success = false, Message = "Activity cancelled" };

                    var donorId = Guid.Parse(id);
                    var actId = Guid.Parse(activityId);

                    var session = await _context.SessionDonors
                        .FirstOrDefaultAsync(x => x.DonorId == donorId
                                                  && x.Status == StatusSession.IsWaitingDonor
                                                  && x.ActivityId == actId);

                    if (session == null)
                        return new ModelResult { Success = false, Message = "User cancel regis" };

                    var history = await _context.Histories
                        .FirstOrDefaultAsync(x => x.ActivityId == actId
                                                  && x.DonorId == donorId
                                                  && x.StatusHistories == StatusHistories.Waiting);

                    if (history == null)
                        return new ModelResult { Success = false, Message = "User cancel regis" };

                    session.Status = StatusSession.Done;
                    history.StatusHistories = StatusHistories.Done;

                    _context.SessionDonors.Update(session);
                    _context.Histories.Update(history);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new ModelResult { Success = true, Message = "Donate success" };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return new ModelResult { Success = false, Message = ex.Message };
                }
            }
        }
        public async Task<ModelResult> StartActivity(string token, string id)
        {
            try
            {
                var validation = await ValidateHospital(token, id);
                if (validation.Data is not ActivityValidationResult result)
                    return new ModelResult { Success = false, Message = "Data format is invalid" };
                var hospital = result.Donor;
                var activity = result.Activity;
                if (hospital.Id != activity.HospitalId) return new ModelResult { Success = false, Message = "This is another hospital activity" };
                if (activity.Status == StatusActivity.Cancel) return new ModelResult { Success = false, Message = "Activity cancelled" };
                activity.Status = StatusActivity.IsGoing;
                await _context.SaveChangesAsync();
                return new ModelResult { Success = false, Message = "Activity start" };
            }
            catch(Exception ex) {
                return new ModelResult { Success = false, Message = ex.ToString() };
            }
        }

        public async Task<ModelResult> GetAcivity(string token, int pageNumber, int pageSize, int status)
        {
            try
            {
                if (token.StartsWith("Bearer "))
                    token = token.Substring("Bearer ".Length).Trim();

                var principal = Jwt.GetClaimsPrincipalToken(token, _config);
                if (principal?.Identity?.Name == null)
                    return new ModelResult { Success = false, Message = "Invalid token" };
                var hospital = await _userManager.FindByNameAsync(principal.Identity.Name);
                var activities = await _context.Activities.FromSqlRaw(@"Select * from ""Activities"" where ""HospitalId"" = {0} and ""Status"" = {1} order by ""CreatedDate"" offset {2} limit {3}",
                    hospital?.Id, status, (pageNumber - 1) * pageSize, pageSize).
                    ToListAsync();
                return new ModelResult { Data = activities, Success = true };
            }
            catch (Exception ex) {
                return new ModelResult { Success = false, Message = ex.ToString() };
            }
        }

        public async Task<ModelResult> CreateRequestBlood(RequestDto requetsDto)
        {
            try
            {
                if (requetsDto == null) return new ModelResult { Success = false, Message = "Missing parameter" };
                var newRequestBlood = _mapper.Map<RequestBlood>(requetsDto);
                newRequestBlood.Status = StatusRequestBlood.IsWaiting;
                newRequestBlood.Address = requetsDto.Address;
                await _context.RequestBloods.AddAsync(newRequestBlood);
                await _context.SaveChangesAsync();
                return new ModelResult { Success = true, Message = "Create request success" };
                
            }
            catch (Exception ex) {
                return new ModelResult { Success = false, Message = ex.ToString() };
            }
        }

        public async Task<ModelResult> GetDonorActivity(string token, string activityId)
        {
            try
            {
                var validation = await ValidateHospital(token, activityId);
                if (validation.Data is not ActivityValidationResult result)
                    return new ModelResult { Success = false, Message = "Data format is invalid" };

                var hospital = result.Donor;
                var activity = result.Activity;
                if (hospital.Id != activity.HospitalId)
                    return new ModelResult { Success = false, Message = "This is another hospital activity" };

                if (activity.Status == StatusActivity.Cancel)
                    return new ModelResult { Success = false, Message = "Activity cancelled" };
                var list = await _context.Activities.Include(x => x.SessionDonors).Where(x => x.Id == activity.Id).FirstOrDefaultAsync();
                return new ModelResult { Success = true, Message = "get User activity success", Data = list };
            }
            catch (Exception ex) {
                return new ModelResult { Success = false, Message = ex.Message };
            }
        }

        public async Task<ModelResult> GetRequestBlood(int pageNumber, int pageSize, int status)
        {
            try
            {
                if(pageNumber <= 0 || pageSize <= 0)
                    return new ModelResult { Success = false, Message = "Invalid page number or page size" };
                if (status < 0 || status > 2)
                    return new ModelResult { Success = false, Message = "Invalid status" };
                var requests = await _context.RequestBloods.Where(x => x.Status == (StatusRequestBlood)status)
                    .OrderByDescending(x => x.CreatedDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                var totalCount = requests.Count;
                return new ModelResult { Data = requests, Success = true, TotalCount = totalCount, Message = "Get request blood success" };
            }
            catch (Exception ex)
            {
                return new ModelResult { Success = false, Message = ex.ToString() };
            }
        }

        public async Task<ModelResult> AcceptRequestBlood(string token, string requestId)
        {
            try
            {
                if (token.StartsWith("Bearer "))
                    token = token.Substring("Bearer ".Length).Trim();
                var principal = Jwt.GetClaimsPrincipalToken(token, _config);
                if (principal?.Identity?.Name == null)
                    return new ModelResult { Success = false, Message = "Invalid token" };
                var hospital = await _userManager.FindByNameAsync(principal.Identity.Name);
                if(hospital == null)
                    return new ModelResult { Success = false, Message = "Hospital not found" };
                if (Guid.TryParse(requestId, out Guid parsedRequestId) && hospital.Id == parsedRequestId)
                {
                    return new ModelResult { Success = false, Message = "You cannot accept your own request" };
                }
                var request = await _context.RequestBloods
                    .FirstOrDefaultAsync(x => x.Id == Guid.Parse(requestId) && x.Status == StatusRequestBlood.IsWaiting);
                if (request == null) return new ModelResult { Success = false, Message = "Request not found or already processed" };
                request.Status = StatusRequestBlood.IsAccept;
                request.HospitalAccept = hospital.Id;
                await _context.SaveChangesAsync();
                return new ModelResult { Success = true, Message = "Request accepted successfully" };
            }
            catch(Exception ex)
            {
                return new ModelResult { Success = false, Message = ex.ToString() };
            }
        }
    }
}
