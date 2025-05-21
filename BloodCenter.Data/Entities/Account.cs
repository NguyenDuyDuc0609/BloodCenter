using BloodCenter.Data.Abstractions.IEntities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Data.Entities
{
    public class Account : IdentityUser<Guid>, IAccount
    {
        [Required]
        public string FullName { get; set; }
        public string? hashedEmail { get; set; }
        public string? refreshToken { get; set; }
        public string Note {  get; set; }
        public string? PasswordReset { get; set; }
        public DateTime? createAt { get; set; }
        public DateTime? expiresAt { get; set; }
        public Donor? Donor { get; set; }
        public Hospital? Hospital { get; set; }
    }
}
