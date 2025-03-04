using BloodCenter.Data.Dtos;
using BloodCenter.Data.Dtos.Hospital;
using BloodCenter.Service.Cores.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BloodCenter.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class HospitalController : ControllerBase
    {
        private readonly IHospital _hospital;
        private ModelResult _result;
        public HospitalController(IHospital hospital)
        {
            _hospital = hospital;
            _result = new ModelResult();
        }
        [HttpPost("AddNewActivity")]
        [Authorize(Roles ="Hospital")]
        public async Task<IActionResult> AddNewActivity([FromBody]ActivityDto activityDto)
        {
            _result = await _hospital.AddNewActivity(activityDto, Request.Headers["Authorization"]);
            return Ok(_result);
        }
    }
}
