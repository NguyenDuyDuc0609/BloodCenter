using BloodCenter.Data.DataAccess;
using BloodCenter.Data.Enums;
using BloodCenter.Service.Utils.Backgrounds.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Service.Utils.Backgrounds
{
    public class QuartzWorker : IQuartzWorker
    {
        private readonly BloodCenterContext _context;
        private ILogger<QuartzWorker> _logger;
        public QuartzWorker(BloodCenterContext context, ILogger<QuartzWorker> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task DoWork(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"[Quartz] Job 'UpdateActivity' started at: {DateTime.UtcNow}");
                var activities = await _context.Activities.FromSqlInterpolated($@"
                    SELECT * FROM ""Activities"" 
                    WHERE (""Status"" = {StatusActivity.IsGoing} OR ""Status"" = {StatusActivity.IsWaiting}) 
                    AND ""DateActivity"" <= {DateTime.UtcNow.Date}"
                )
                    .ToListAsync();
                foreach (var a in activities) {
                    if (a.Status == StatusActivity.IsGoing) {
                        a.Status = StatusActivity.Done;
                    }
                    else
                    {
                        a.Status = StatusActivity.IsGoing;
                    }
                }
                _context.Activities.UpdateRange(activities);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex) {
                _logger.LogError(ex, "[Quartz] Job 'UpdateActivity' failed!");
            }
        }
    }
}
