using BloodCenter.Data.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Service.Cores.Interface
{
    public interface IAdmin
    {
        public Task<ModelResult> GetUser();
        public Task<ModelResult> GetActivity();
        public Task<ModelResult> AddNewHospital();
        public Task<ModelResult> DeleteHospital();
    }
}
