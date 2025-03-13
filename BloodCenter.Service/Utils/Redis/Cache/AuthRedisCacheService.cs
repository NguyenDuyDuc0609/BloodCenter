using BloodCenter.Data.Entities;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BloodCenter.Service.Utils.Redis.Cache
{
    public class AuthRedisCacheService : IAuthRedisCacheService
    {
        private readonly IDistributedCache _cache;
        public AuthRedisCacheService(IDistributedCache cache) {
            _cache = cache;
        }
        public async Task<T?> GetAsync<T>(string key)
        {
           string? jsonData = await _cache.GetStringAsync(key);
            return jsonData is null ? default : JsonSerializer.Deserialize<T>(jsonData);
        }

        public async Task<List<Activity>> GetPageActivitiesAsync(int pageNumber, int pageSize)
        {
            var jsonData = await _cache.GetStringAsync("activities");
            if (jsonData is null) return new List<Activity>();

            var activities = JsonSerializer.Deserialize<List<Activity>>(jsonData);
            return activities.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        }

        public async Task RemoveAsync(string key)
        {
            await _cache.RemoveAsync(key);
        }

        public async Task SaveActivityListAsync(List<Activity> activities)
        {
            var jsonData = JsonSerializer.Serialize(activities);
            await _cache.SetStringAsync("activities", jsonData, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) 
            });
        }

        public async Task SetAsync(string key, object value, TimeSpan? expiration = null)
        {
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromHours(1)
            };
            string data = JsonSerializer.Serialize(value);
            await _cache.SetStringAsync(key, data, cacheOptions);
        }
    }
}
