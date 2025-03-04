using BloodCenter.Data.Entities;
using BloodCenter.Service.Utils.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Service.Utils.Auth
{
    public class Jwt : IJwt
    {
        private readonly IConfiguration _config;
        public Jwt(IConfiguration config, UserManager<Account> userManager)
        {
            _config = config;
        }
        public string GenerateJWT(Account account, List<string> roles)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, account.FullName),
                new Claim(ClaimTypes.Email, account.Email),
                new Claim(ClaimTypes.Name, account.UserName),
            };
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            var token = new JwtSecurityToken(_config["Jwt:Issuer"],
                _config["Jwt:Audience"],
                claims,
                expires: DateTime.UtcNow.AddMinutes(3),
                signingCredentials: credentials
                );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var generator = RandomNumberGenerator.Create();
            generator.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
        public static ClaimsPrincipal? GetClaimsPrincipalToken(string? token, IConfiguration _configuration)
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
    }
}
