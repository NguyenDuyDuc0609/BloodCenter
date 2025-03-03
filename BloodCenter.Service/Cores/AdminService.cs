using AutoMapper;
using BloodCenter.Data.DataAccess;
using BloodCenter.Data.Dtos;
using BloodCenter.Data.Dtos.AuthDto;
using BloodCenter.Data.Entities;
using BloodCenter.Service.Cores.Interface;
using BloodCenter.Service.Utils.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace BloodCenter.Service.Cores
{
    public class AdminService : IAdmin
    {
        private readonly BloodCenterContext _bloodCenterContext;
        private ModelResult _result;
        private IMapper _mapper;
        private readonly UserManager<Account> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private IEmailService _emailService;
        private IJwt _jwt;
        private readonly IConfiguration _configuration;
        public AdminService(BloodCenterContext bloodCenterContext, IMapper mapper, UserManager<Account> userManager, RoleManager<IdentityRole<Guid>> roleManager, IEmailService mailService, IJwt jwt, IConfiguration configuration)
        {
            _result = new ModelResult();
            _bloodCenterContext = bloodCenterContext;
            _mapper = mapper;
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = mailService;
            _jwt = jwt;
            _configuration = configuration;
        }
        private static string HashEmail(string email)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(email);
                var hashEmail = sha256.ComputeHash(bytes);
                var builder = new StringBuilder();
                foreach (var item in hashEmail)
                {
                    builder.Append(item.ToString("x2"));
                }
                return builder.ToString();
            }
        }
        public async Task<ModelResult> AddNewHospital(RegisterDto registerDto)
        {
            using (var transaction = _bloodCenterContext.Database.BeginTransaction()) {
                try
                {
                    if (registerDto == null)
                    {
                        return new ModelResult { Success = false, Message = "Missing parameter" };
                    }
                    string sqlString = @"SELECT COUNT(*) FROM ""AspNetUsers"" WHERE ""FullName"" = {0}";
                    var result = await _bloodCenterContext.Database.ExecuteSqlRawAsync(sqlString);
                    if (result > 0) {
                        return new ModelResult { Success = false, Message = "Hospital is already exits" };
                    }
                    var hashEmail = HashEmail(registerDto.Email);
                    var newHospital = _mapper.Map<Account>(registerDto);
                    newHospital.Note = "Hospital";
                    newHospital.hashedEmail = hashEmail;
                    var createHospital = await _userManager.CreateAsync(newHospital);
                    bool roleExist = await _roleManager.Roles.AnyAsync(r => r.Name == registerDto.Role.ToString());
                    if (!roleExist)
                    {
                        await _roleManager.CreateAsync(new IdentityRole<Guid>(registerDto.Role.ToString()));
                    }
                    await _userManager.AddToRoleAsync(newHospital, registerDto.Role.ToString());
                    if (registerDto.Role == Data.Enums.Role.Hospital)
                    {
                        bool donorExists = await _bloodCenterContext.Donors.AnyAsync(d => d.Id == newHospital.Id);
                        if (!donorExists)
                        {
                            var newDonor = new Hospital
                            {
                                Id = newHospital.Id,
                                Account = newHospital
                            };
                            _bloodCenterContext.Hospitals.Add(newDonor);
                        }
                    }

                    var sendMail = await _emailService.SendMailActiveAccount(registerDto.Email, hashEmail);
                    if (!sendMail.Success)
                    {
                        await transaction.RollbackAsync();
                        return new ModelResult { Success = false, Message = sendMail.Message };
                    }

                    await _bloodCenterContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new ModelResult { Success = true, Message = "Registration successful" };
                }
                catch (Exception ex) 
                {
                    await transaction.RollbackAsync();
                    return new ModelResult { Success = false, Message = ex.Message };
                }
            }
        }

        public async Task<ModelResult> DeleteHospital(string hospitalId)
        {
            using (var transaction = await _bloodCenterContext.Database.BeginTransactionAsync()) {
                try
                {
                    if (string.IsNullOrWhiteSpace(hospitalId)) {
                        return new ModelResult { Success = false, Message = "Missing paremeter" };
                    }
                    string sqlString = @"DELETE FROM ""AspNetUsers"" WHERE ""Id"" = @p0";
                    await _bloodCenterContext.Database.ExecuteSqlRawAsync(sqlString, new[] { hospitalId});
                    await _bloodCenterContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return new ModelResult { Success = true, Message = "Delete success" };
                }
                catch(Exception ex)
                {
                    await transaction.RollbackAsync();
                    return new ModelResult { Success = false, Message = ex.ToString() };
                }
            }
        }
        public async Task<ModelResult> GetActivity(int pageNumber, int pageSize, string openTime, int status, bool isDelete, string date)
        {
            try
            {
                if (pageNumber < 1 || pageSize < 1)
                {
                    return new ModelResult { Success = false, Message = "Page number or page size is not valid" };
                }
                DateTime? dateActivity = null;
                if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out DateTime parsedDate))
                {
                    dateActivity = parsedDate;
                }

                string query = @"
                        SELECT * FROM ""Activities""
                        WHERE (@p0 IS NULL OR ""OpenratingHour"" = @p0)
                        AND (@p1 IS NULL OR ""Status"" = @p1)
                        AND (@p2 IS NULL OR ""IsDelete"" = @p2)
                        AND (@p3 IS NULL OR ""DateActivity"" = @p3)
                        ORDER BY ""HospitalId""
                        OFFSET @p4 LIMIT @p5;
                    ";

                var parameters = new object[]
                {
                    string.IsNullOrEmpty(openTime) ? DBNull.Value : openTime,
                    status == -1 ? DBNull.Value : status, 
                    isDelete, 
                    dateActivity.HasValue ? (object)dateActivity.Value : DBNull.Value,
                    (pageNumber - 1) * pageSize, 
                    pageSize 
                };

                var activities = await _bloodCenterContext.Activities.FromSqlRaw(query, parameters).ToListAsync();

                return new ModelResult { Success = true, Data = activities };
            }
            catch (Exception ex)
            {
                return new ModelResult { Success = false, Message = ex.Message };
            }
        }


        public async Task<ModelResult> GetUser()
        {
            throw new NotImplementedException();
        }
    }
}
