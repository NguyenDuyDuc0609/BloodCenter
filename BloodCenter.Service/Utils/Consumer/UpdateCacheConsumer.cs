using BloodCenter.Data.Constracts;
using BloodCenter.Data.DataAccess;
using BloodCenter.Service.Utils.Redis.Cache;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MimeKit.Cryptography;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Service.Utils.Consumer
{
    public class UpdateCacheConsumer : IConsumer<UpdateCache>
    {
        private readonly IAuthRedisCacheService _redis;
        private readonly BloodCenterContext _context;
        private readonly ILogger<UpdateCacheConsumer> _logger;
        public UpdateCacheConsumer(IAuthRedisCacheService redis, BloodCenterContext context, ILogger<UpdateCacheConsumer> logger) {
            _redis = redis;
            _context = context;
            _logger = logger;
        }
        public async Task Consume(ConsumeContext<UpdateCache> context)
        {
            var activities = await _context.Activities.FromSqlRaw(@"Select * from ""Activities"" order by ""CreatedDate"" Desc").ToListAsync();
            await _redis.SaveActivityListAsync(activities);
            await Task.CompletedTask;
        }
    }
}
