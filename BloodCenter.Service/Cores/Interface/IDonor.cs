using BloodCenter.Data.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Service.Cores.Interface
{
    public interface IDonor
    {
        public Task<ModelResult> GetActivityIsGoing(int pageNumber, int pageSize, int status);
        public Task<ModelResult> RegisterDonate(string token, string activity);
        public Task<ModelResult> GetPersonalHistory(string token, int pageNumber, int pageSize);
        public Task<ModelResult> CancelRegistration(string token, string activity);
    }

}
