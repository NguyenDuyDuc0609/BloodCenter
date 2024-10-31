using BloodCenter.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection.Emit;

namespace BloodCenter.Data.DataAccess
{
    public class BloodCenterContext : IdentityDbContext<Account, IdentityRole<Guid>, Guid>
    {
        public DbSet<Account> Accounts { get; set; }
        public BloodCenterContext(DbContextOptions<BloodCenterContext> options)
            : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
