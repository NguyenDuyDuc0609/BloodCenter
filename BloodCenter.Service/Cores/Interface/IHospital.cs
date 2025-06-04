using BloodCenter.Data.Dtos;
using BloodCenter.Data.Dtos.Hospital;
using BloodCenter.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Service.Cores.Interface
{
    public interface IHospital
    {
        public Task<ModelResult> AddNewActivity(ActivityDto activityDto, string id);
        public Task<ModelResult> EditActivity(ActivityDto activityDto, string id);
        public Task<ModelResult> CancelActivity(string token, string id);
        public Task<ModelResult> EndActivity(string token, string id);
        public Task<ModelResult> StartActivity(string token, string id);
        public Task<ModelResult> ComfirmDonor(string token, string activityId, string id);
        public Task<ModelResult> GetAcivity(string token, int pageNumber, int pageSize, int status);
        public Task<ModelResult> CreateRequestBlood(RequestDto requetsDto);
        public Task<ModelResult> GetDonorActivity(string token, string activityId);
        public Task<ModelResult> GetRequestBlood (int pageNumber, int pageSize, int status);
    }
}
