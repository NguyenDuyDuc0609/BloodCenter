using BloodCenter.Data.Dtos;
using BloodCenter.Data.Dtos.Hospital;
using BloodCenter.Service.Cores.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BloodCenter.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HospitalController : ControllerBase
    {
        private readonly IHospital _hospital;
        private ModelResult _result;
        public HospitalController(IHospital hospital)
        {
            _hospital = hospital;
            _result = new ModelResult();
        }
        [HttpPost]
        [Authorize(Roles ="Hospital")]
        public async Task<IActionResult> AddNewActivity(ActivityDto activityDto)
        {
            _result = await _hospital.AddNewActivity(activityDto, Request.Headers["Authorization"]);
            return Ok(_result);
        }
    }
}
