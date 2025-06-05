using AutoMapper;
using BloodCenter.Data.DataAccess;
using BloodCenter.Data.Dtos;
using BloodCenter.Data.Dtos.AuthDto;
using BloodCenter.Data.Entities;
using BloodCenter.Service.Cores.Interface;
using BloodCenter.Service.Utils.Interface;
using BloodCenter.Service.Utils.Redis.Cache;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Reflection.Metadata.Ecma335;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Yarp.ReverseProxy.Forwarder;
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
        private readonly IAuthRedisCacheService _cache;
        public Auth(BloodCenterContext bloodCenterContext, IMapper mapper, UserManager<Account> userManager, RoleManager<IdentityRole<Guid>> roleManager, IEmailService mailService, IJwt jwt, IConfiguration configuration, IAuthRedisCacheService cache)
        {
            _bloodCenterContext = bloodCenterContext;
            _mapper = mapper;
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = mailService;
            _jwt = jwt;
            _configuration = configuration;
            _cache = cache;
        }

        private ModelResult CreateResult(string message, bool success, object? data = default, int? totalCount = null ) => new ModelResult { Success = success, Message = message, Data = data, TotalCount = totalCount };
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
                    if (string.IsNullOrWhiteSpace(loginDto.UserName) || string.IsNullOrWhiteSpace(loginDto.Password)) return CreateResult("Missing parameter", false);

                    var user = await _userManager.Users
                                    .FirstOrDefaultAsync(u => u.UserName == loginDto.UserName);

                    if (user == null) return CreateResult("User does not exits", false);

                    var checkPassword = await _userManager.CheckPasswordAsync(user, loginDto.Password);

                    if (!checkPassword) return CreateResult("Incorrect password. Please try again", false);

                    if (user.EmailConfirmed != true) return CreateResult("Active account in mail to login", false);
                    if (user.StatusAccount == Data.Enums.StatusAccount.Locked) return CreateResult("Account locked, change password to login", false, user.UserName);

                    var roles = await _userManager.GetRolesAsync(user);
                    var role = roles.FirstOrDefault();
                    user.refreshToken = (user.refreshToken != null && user.expiresAt > DateTime.UtcNow) ? user.refreshToken : _jwt.GenerateRefreshToken();

                    var token = _jwt.GenerateJWT(user, roles.ToList());

                    List<string> roleList = roles.ToList();
                    await _bloodCenterContext.SaveChangesAsync();
                    var loginResponse = new LoginResponseDto
                    {
                        Token = token,
                        refreshToken = user.refreshToken,
                        UserName = user.UserName,
                        FullName = user.FullName,
                        Role = roleList,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        Note = user.Note,
                        StatusAccount = user.StatusAccount,
                        Id = user.Id
                    };
                    string redisKey = $"user:{user.Id}:token";
                    await _cache.SetAsync($"user:{user.Id}:token", token, TimeSpan.FromHours(1));
                    await _cache.SetAsync($"user:{user.Id}:role", role, TimeSpan.FromHours(1));
                    await transaction.CommitAsync();
                    return CreateResult("Login success", true, loginResponse);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return CreateResult(ex.ToString(), false);
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
                        return CreateResult("Missing parameter", false);
                    }

                    var existingUser = await _bloodCenterContext.Users
                        .Where(u => u.Email == registerDto.Email || u.UserName == registerDto.UserName)
                        .Select(u => new { u.Email, u.UserName })
                        .FirstOrDefaultAsync();

                    if (existingUser != null)
                    {
                        return CreateResult("The email or username has already been used by another user", false);
                    }

                    var hashEmail = HashEmail(registerDto.Email);

                    var newUser = _mapper.Map<Account>(registerDto);
                    newUser.Note = "Donor";
                    newUser.hashedEmail = hashEmail;

                    var createUserResult = await _userManager.CreateAsync(newUser, registerDto.Password);
                    if (!createUserResult.Succeeded)
                    {
                        return CreateResult("Create user failed", false);
                    }

                    registerDto.Role = Data.Enums.Role.Donor;
                    bool roleExist = await _roleManager.Roles.AnyAsync(r => r.Name == registerDto.Role.ToString());
                    if (!roleExist)
                    {
                        await _roleManager.CreateAsync(new IdentityRole<Guid>(registerDto.Role.ToString()));
                    }
                    await _userManager.AddToRoleAsync(newUser, registerDto.Role.ToString());

                    if (registerDto.Role == Data.Enums.Role.Donor)
                    {
                        bool donorExists = await _bloodCenterContext.Donors.AnyAsync(d => d.Id == newUser.Id);
                        if (!donorExists)
                        {
                            var newDonor = new Donor
                            {
                                Id = newUser.Id,
                                Account = newUser
                            };
                            _bloodCenterContext.Donors.Add(newDonor);
                        }
                    }

                    var sendMail = await _emailService.SendMailActiveAccount(registerDto.Email, hashEmail);
                    if (!sendMail.Success)
                    {
                        await transaction.RollbackAsync();
                        return CreateResult(sendMail.Message, false);
                    }

                    await _bloodCenterContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return CreateResult("Registration successful", true);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return CreateResult(ex.Message, false);
                }
            }
        }



        public async Task<ModelResult> EmailConfirm(string hashedEmail)
        {
            using (var transaction = await _bloodCenterContext.Database.BeginTransactionAsync())
            {
                try
                {
                    if (string.IsNullOrEmpty(hashedEmail))
                    {
                        return CreateResult("Missing parameter", false);
                    }

                    var user = await _bloodCenterContext.Users
                        .FirstOrDefaultAsync(u => u.hashedEmail == hashedEmail);

                    if (user == null)
                    {
                        return CreateResult("Email not valid", false);
                    }

                    user.EmailConfirmed = true;
                    user.StatusAccount = Data.Enums.StatusAccount.Actived;
                    await _bloodCenterContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return CreateResult("Verify success", true);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return CreateResult(ex.Message, false);
                }
            }
        }


        public async Task<ModelResult> Refresh(RefreshDto refreshDto)
        {
            var principal = GetClaimsPrincipalToken(refreshDto.AccessToken);
            if (principal?.Identity?.Name is null)
            {
                return CreateResult("Missing access token to get principal", false);
            }

            var user = await _userManager.FindByNameAsync(principal.Identity.Name);
            if (user == null || user.refreshToken != refreshDto.RefreshToken || user.expiresAt < DateTime.UtcNow)
            {
                return CreateResult("Invalid refresh token or user not found", false);
            }

            string? cachedRole = await _cache.GetAsync<string>($"user:{user.Id}:role");
            List<string> roles;

            if (!string.IsNullOrEmpty(cachedRole))
            {
                roles = new List<string> { cachedRole };
            }
            else
            {
                roles = (await _userManager.GetRolesAsync(user)).ToList();

                if (roles.Any())
                {
                    await _cache.SetAsync(
                        $"user:{user.Id}:role",
                        roles,
                        TimeSpan.FromHours(1)
                    );
                }
            }

            var newAccessToken = _jwt.GenerateJWT(user, roles);

            return CreateResult("Create new token success", true, newAccessToken);
        }


        public async Task<ModelResult> ForgotPassword(string email)
        {
            using var transaction = await _bloodCenterContext.Database.BeginTransactionAsync();
            try
            {
                if (string.IsNullOrEmpty(email))
                    return CreateResult("Missing email", false);

                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                    return CreateResult("Email not found", false);

                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

                user.StatusAccount = Data.Enums.StatusAccount.Locked;

                var sendMailResult = await EmailService.SendMailResetPassword(email, resetToken, _configuration);
                if (!sendMailResult.Success)
                {
                    await transaction.RollbackAsync();
                    return CreateResult(sendMailResult.Message, false);
                }

                await _bloodCenterContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreateResult("Reset password email sent successfully", true);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return CreateResult($"Exception: {ex.Message}", false);
            }
        }

        public async Task<ModelResult> ResetPassword(string username, string resetToken, string newPassword)
        {
            using var transaction = await _bloodCenterContext.Database.BeginTransactionAsync();
            try
            {
                if (string.IsNullOrEmpty(resetToken))
                    return CreateResult("Missing reset token", false);

                if (string.IsNullOrEmpty(newPassword))
                    return CreateResult("Missing new password", false);

                var user = await _userManager.FindByNameAsync(username);
                if (user == null)
                    return CreateResult("User not found", false);

                var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    return CreateResult($"New password is not valid: {errors}", false);
                }

                user.StatusAccount = Data.Enums.StatusAccount.Actived;

                await _bloodCenterContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreateResult("Reset password success", true);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return CreateResult(ex.ToString(), false);
            }
        }

        public async Task<ModelResult> ChangePassword(string username, string password, string newPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(username)) return CreateResult("Missing username", false);
                if (string.IsNullOrEmpty(password)) return CreateResult("Missing current password", false);
                if (string.IsNullOrEmpty(newPassword)) return CreateResult("Missing new password", false);

                var user = await _userManager.FindByNameAsync(username);
                if (user == null) return CreateResult("User not found", false);

                var result = await _userManager.ChangePasswordAsync(user, password, newPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    return CreateResult($"Password change failed: {errors}", false);
                }

                return CreateResult("Change password success", true);
            }
            catch (Exception ex)
            {
                return CreateResult($"Exception: {ex.ToString()}", false);
            }
        }

    }
}
