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
    }
}
