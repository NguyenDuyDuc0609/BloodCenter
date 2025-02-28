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
using BloodCenter.Data.Abstractions.Interface;
using BloodCenter.Data.Abstractions;

namespace BloodCenter.Data.DataAccess
{
    public class BloodCenterContext : IdentityDbContext<Account, IdentityRole<Guid>, Guid>
    {
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Donor> Donors { get; set; }
        public DbSet<Hospital> Hospitals { get; set; }
        public DbSet<History> Histories { get; set; }
        public DbSet<Activity> Activities { get; set; }
        public DbSet<RequestBlood> RequestBloods { get; set; }
        public DbSet<SessionDonor> SessionDonors { get; set; }
        public DbSet<Blood> Bloods { get; set; }
        public BloodCenterContext(DbContextOptions<BloodCenterContext> options)
        : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<IdentityUserLogin<Guid>>()
                .HasKey(l => new { l.LoginProvider, l.ProviderKey });
            modelBuilder.Entity<Donor>()
                .HasOne(d => d.Account)
                .WithOne(a => a.Donor) 
                .HasForeignKey<Donor>(d => d.Id);

            modelBuilder.Entity<Hospital>()
                .HasOne(h => h.Account)
                .WithOne(a => a.Hospital)
                .HasForeignKey<Hospital>(h => h.Id);

            modelBuilder.Entity<History>()
                .HasOne(h => h.Donor)
                .WithMany(d => d.Histories)
                .HasForeignKey(h => h.DonorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Blood>()
                .HasOne(b => b.Hospital)
                .WithMany(h => h.Bloods)
                .HasForeignKey(b => b.HospitalId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SessionDonor>()
                .HasOne(sd => sd.Donor)
                .WithMany(d => d.SessionDonors)
                .HasForeignKey(sd => sd.DonorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Activity>()
                .HasOne(a => a.Hospital)
                .WithMany(h => h.Activities)
                .HasForeignKey(a => a.HospitalId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SessionDonor>()
                .HasOne(sd => sd.Activity)
                .WithMany(a => a.SessionDonors)
                .HasForeignKey(sd => sd.ActivityId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Activity>()
                .HasIndex(a => a.DateActivity)
                .HasDatabaseName("IX_Activity_DateActivity");
        }
        public override int SaveChanges()
        {
            UpdateIsDelete();
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateIsDelete();
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        // Xử lý xóa mềm (Soft Delete)
        private void UpdateIsDelete()
        {
            var entities = ChangeTracker.Entries<ISoftDelete>().Where(x => x.State == EntityState.Deleted).ToList();

            foreach (var entity in entities)
            {
                entity.State = EntityState.Modified;
                entity.Entity.IsDelete = true;
                entity.Entity.DeleteDate = DateTime.UtcNow;
            }
        }

        // Cập nhật CreatedDate và ModifiedDate
        private void UpdateTimestamps()
        {
            var entities = ChangeTracker.Entries<EntityAuditBase<Guid>>().Where(x => x.State == EntityState.Added || x.State == EntityState.Modified).ToList();

            foreach (var entity in entities)
            {
                if (entity.State == EntityState.Added && entity.Entity.GetType().GetProperty("CreatedDate") != null)
                {
                    entity.Entity.CreatedDate = DateTime.UtcNow;
                }

                if (entity.Entity.GetType().GetProperty("ModifiedDate") != null)
                {
                    entity.Entity.ModifiedDate = DateTime.UtcNow;
                }
            }
        }

    }
}
