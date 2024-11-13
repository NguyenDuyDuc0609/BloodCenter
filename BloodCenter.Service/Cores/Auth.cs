using AutoMapper;
using BloodCenter.Data.DataAccess;
using BloodCenter.Data.Dtos;
using BloodCenter.Data.Dtos.AuthDto;
using BloodCenter.Data.Entities;
using BloodCenter.Service.Cores.Interface;
using BloodCenter.Service.Utils.Interface;
using MailKit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection.Metadata.Ecma335;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
namespace BloodCenter.Service.Cores
{
    public class Auth : IAuth
    {
        private readonly BloodCenterContext _bloodCenterContext;
        private ModelResult _result;
        private IMapper _mapper;
        private readonly UserManager<Account> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private IEmailService _emailService;
        private IJwt _jwt;
        private readonly IConfiguration _configuration;
        public Auth(BloodCenterContext bloodCenterContext, IMapper mapper, UserManager<Account> userManager, RoleManager<IdentityRole<Guid>> roleManager, IEmailService mailService, IJwt jwt, IConfiguration configuration)
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


        private static string HashEmail(string email) {
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
        private ClaimsPrincipal? GetClaimsPrincipalToken(string? token)
        {
            var validation = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]))
            };
            return new JwtSecurityTokenHandler().ValidateToken(token, validation, out _);
        } 
        public async Task<ModelResult> Login(LoginDto loginDto)
        {
            using (var transaction = await _bloodCenterContext.Database.BeginTransactionAsync())
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(loginDto.UserName) || string.IsNullOrWhiteSpace(loginDto.Password))
                    {
                        _result.Success = false;
                        _result.Message = "Missing parameter";
                    }
                    var user = await _userManager.FindByNameAsync(loginDto.UserName);
                    if (user == null)
                    {
                        _result.Success = false;
                        _result.Message = "User does not exist";
                        return _result;
                    }
                    if(user.EmailConfirmed != true)
                    {
                        _result.Success = false;
                        _result.Message = "Active account in mail to login";
                        return _result;
                    }
                    var checkPassword = await _userManager.CheckPasswordAsync(user, loginDto.Password);
                    if (!checkPassword)
                    {
                        _result.Success = false;
                        _result.Message = "Incorrect password. Please try again";
                        return _result;
                    }
                    var refreshToken = "";
                    if (user.refreshToken != null && user.expiresAt > DateTime.UtcNow)
                    {
                        refreshToken = user.refreshToken;
                    }
                    else
                    {
                        refreshToken = _jwt.GenerateRefreshToken();
                        user.refreshToken = refreshToken;
                        user.expiresAt = DateTime.UtcNow.AddDays(7);
                    }
                    var tokenTask = _jwt.GenerateJWT(user);
                    var rolesTask = _userManager.GetRolesAsync(user);
                    await Task.WhenAll(tokenTask, rolesTask);
                    var token = await tokenTask;
                    var roles = await rolesTask;
                    List<string> roleList = roles.ToList();
                    await _bloodCenterContext.SaveChangesAsync();
                    var loginResponse = new LoginResponseDto
                    {
                        Token = token,
                        refreshToken = refreshToken,
                        UserName = user.UserName,
                        FullName = user.FullName,
                        Role = roleList
                    };
                    _result.Success = true;
                    _result.Data = loginResponse;
                    _result.Message = "Login success";
                    await transaction.CommitAsync();
                    return _result;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _result.Success = false;
                    _result.Message = ex.Message;
                    return _result;
                }
            }
        }

        public async Task<ModelResult> Register(RegisterDto registerDto)
        {
            using (var transaction = await _bloodCenterContext.Database.BeginTransactionAsync())
            {
                try
                {
                    if (registerDto == null)
                    {
                        _result.Success = false;
                        _result.Message = "Missing parameter";
                        return _result;
                    }
                    var checkEmailTask = _userManager.FindByEmailAsync(registerDto.Email);
                    var chekcUsernameTask = _userManager.FindByNameAsync(registerDto.UserName);
                    await Task.WhenAll(checkEmailTask, chekcUsernameTask);
                    var checkEmailExist = checkEmailTask.Result;
                    var chekcUsernameExist = chekcUsernameTask.Result;
                    if (checkEmailExist != null && chekcUsernameExist != null)
                    {
                        _result.Success = false;
                        _result.Message = "The email or Username has already been used by another user";
                        return _result;
                    }
                    var hashEmail = HashEmail(registerDto.Email);
                    var newUser = _mapper.Map<Account>(registerDto);
                    newUser.hashedEmail = hashEmail;
                    var result = await _userManager.CreateAsync(newUser, registerDto.Password);
                    if (!result.Succeeded)
                    {
                        _result.Success = false;
                        _result.Message = "Create user failed";
                        return _result;
                    }
                    var roleExist = await _roleManager.RoleExistsAsync(registerDto.Role.ToString());
                    if (!roleExist)
                    {
                        await _roleManager.CreateAsync(new IdentityRole<Guid>(registerDto.Role.ToString()));
                    }
                    await _userManager.AddToRoleAsync(newUser, registerDto.Role.ToString());
                    var sendMail = await _emailService.SendMailActiveAccount(registerDto.Email, hashEmail);
                    if (!sendMail.Success)
                    {
                        await transaction.RollbackAsync();
                        _result.Success = false;
                        _result.Message = sendMail.Message;
                        return _result;
                    }
                    await _bloodCenterContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    _result.Success = true;
                    _result.Message = "Registration successful";
                    return _result;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _result.Success = false;
                    _result.Message = ex.Message;
                    return _result;
                }
            }
        }
        public async Task<ModelResult> GetUser()
        {
            var user = await _userManager.FindByEmailAsync("nguyenduyduc0609genshin@gmail.com");
            var role = await _userManager.GetRolesAsync(user);
            _result.Success = true;
            _result.Message = "success";
            _result.Data = role;
            return _result;
        }

        public async Task<ModelResult> EmailConfirm(string hashedEmail)
        {
            using (var transaction = await _bloodCenterContext.Database.BeginTransactionAsync())
            {
                try
                {
                    if (hashedEmail == null)
                    {
                        _result.Success = false;
                        _result.Message = "Missing parameter";
                        return _result;
                    }
                    var user = await _bloodCenterContext.Users.Where(u => u.hashedEmail == hashedEmail).FirstOrDefaultAsync();
                    if (user == null)
                    {
                        _result.Success = false;
                        _result.Message = "Email not valid";
                        return _result;
                    }
                    user.EmailConfirmed = true;
                    await _bloodCenterContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    _result.Success = true;
                    _result.Message = "Verify success";
                    return _result;
                }
                catch (Exception ex) {
                    await transaction.RollbackAsync();
                    _result.Success = false;
                    _result.Message = ex.Message;
                    return _result;
                }
            }
        }
        
        public async Task<ModelResult> Refresh(RefreshDto refreshDto)
        {
            var priciple = GetClaimsPrincipalToken(refreshDto.AccessToken);
            //if (priciple?.Identity?.Name is null)
            //{
            //    _result.Message = "Khong lay duoc";
            //    return _result;
            //}
            //else
            //{
            //    _result.Data = priciple?.Identity?.Name;
            //    return _result;
            //}
            if (priciple?.Identity?.Name is null)
            {
                _result.Success = false;
                return _result;
            }

            var user = await _userManager.FindByNameAsync(priciple.Identity.Name);
            if (user == null || user.refreshToken != refreshDto.RefreshToken || user.expiresAt < DateTime.UtcNow)
            {
                _result.Success = false;
                return _result;
            }
            var newAccessToken = await _jwt.GenerateJWT(user);
            _result.Success = true;
            _result.Data = newAccessToken;
            _result.Message = "Create new token success";
            return _result;
        }
    }
}
