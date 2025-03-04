using BloodCenter.Service.Utils.Backgrounds.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Service.Utils.Backgrounds
{
    public class QuartzStartProgram : BackgroundService
    {
        private readonly ILogger<QuartzStartProgram> _logger;
        private IScheduler _scheduler;
        private readonly IServiceProvider _serviceProvider;
        public QuartzStartProgram(ILogger<QuartzStartProgram> logger, IScheduler scheduler, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _scheduler = scheduler;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await UpdateDatabase(stoppingToken);
        }
        private async Task UpdateDatabase(CancellationToken cancellationToken)
        {

            using (var scope = _serviceProvider.CreateScope()) {
                var quartzWoker = scope.ServiceProvider.GetRequiredService<IQuartzWorker>();
                await quartzWoker.DoWork(cancellationToken);
            } 
        }
        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MyBackgroundService is stopping.");
            await base.StopAsync(stoppingToken);
        }
    }
}
