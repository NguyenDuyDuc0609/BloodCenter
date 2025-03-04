using BloodCenter.Data.Dtos;
using BloodCenter.Data.Dtos.AuthDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Service.Cores.Interface
{
    public interface IAdmin
    {
        public Task<ModelResult> GetUser(string note, int pageNumber, int pageSize);
        public Task<ModelResult> GetActivity(int pageNumber, int pageSize, string openTime, int status, bool isDelete, string date);
        public Task<ModelResult> AddNewHospital(RegisterDto registerDto);
        public Task<ModelResult> DeleteHospital(string hospitalId);
    }
}
