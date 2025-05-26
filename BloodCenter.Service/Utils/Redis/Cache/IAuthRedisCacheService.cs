using BloodCenter.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Service.Utils.Redis.Cache
{
    public interface IAuthRedisCacheService
    {
        Task SetAsync(string key, object value, TimeSpan? expiration = null);
        Task<T?> GetAsync<T>(string key);
        Task RemoveAsync(string key);
        Task SaveActivityListAsync(List<Activity> activities);
        Task<(List<Activity> data, int totalCount)> GetPageActivitiesAsync(int pageNumber, int pageSize);
    }
}
