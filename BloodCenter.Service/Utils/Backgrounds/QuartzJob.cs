using BloodCenter.Service.Utils.Backgrounds.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Service.Utils.Backgrounds
{
    public class QuartzJob : IJob
    {
        public IServiceProvider _service { get; set; }
        private readonly ILogger<QuartzJob> _logger;
        public QuartzJob(IServiceProvider service, ILogger<QuartzJob> logger)
        {
            _service = service;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation($"[Quartz] Job 'UpdateActivity' started at: {DateTime.UtcNow}");

            using (var scope = _service.CreateScope())
            {
                var quartzWoker = scope.ServiceProvider.GetRequiredService<IQuartzWorker>();
                try
                {
                    await quartzWoker.DoWork(context.CancellationToken);
                    _logger.LogInformation($"[Quartz] Job 'UpdateActivity' completed at: {DateTime.UtcNow}");
                }
                catch (Exception ex)
                {
                   _logger.LogError(ex, "[Quartz] Job 'UpdateActivity' failed!");
                }
            }
        }
    }
}
