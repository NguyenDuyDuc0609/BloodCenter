using BloodCenter.Data.DataAccess;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Data.ApplyMigrationDocker
{
    public static class MigrationExtensions
    {
        public static void ApplyMigration(this IApplicationBuilder app)
        {
            using IServiceScope scope = app.ApplicationServices.CreateScope();
            using BloodCenterContext dbContext = scope.ServiceProvider.GetRequiredService<BloodCenterContext>();

            dbContext.Database.Migrate();
        }
    }
}
