using BloodCenter.Data.Dtos;
using BloodCenter.Data.Dtos.Hospital;
using BloodCenter.Service.Cores.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Cmp;
using System.Diagnostics;
using Yarp.ReverseProxy.Forwarder;

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
        [HttpPost("Cancel-activity")]
        [Authorize(Roles ="Hospital")]
        public async Task<IActionResult> CancelActivity([FromBody] string activityId)
        {
            _result = await _hospital.CancelActivity(Request.Headers["Authorization"], activityId);
            return Ok(_result);
        }
        [HttpPost("end-activity")]
        [Authorize(Roles ="Hospital")]
        public async Task<IActionResult> EndActivity([FromBody] string activityId)
        {
            _result = await _hospital.EndActivity(Request.Headers["Authorization"], activityId);
            return Ok(_result);
        }
        [HttpPost("Start-activtity")]
        [Authorize(Roles ="Hospital")]
        public async Task<IActionResult> StartActivity([FromBody] string activityId)
        {
            _result = await _hospital.StartActivity(Request.Headers["Authorization"], activityId);
            return Ok(_result);
        }
        [HttpGet("Hospital-activity/{pageNumer}/{pageSize}/{status}")]
        [Authorize(Roles ="Hospital")]
        public async Task<IActionResult> HospitalGetActivity(int pageNumer, int pageSize, int status)
        {
            _result = await _hospital.GetAcivity(Request.Headers["Authorization"], pageNumer, pageSize, status);
            return Ok(_result);
        }
        [HttpPost]
        [Authorize(Roles ="Hospital")]
        public async Task<IActionResult> ConfirmDonor([FromBody] ConfirmDonor confirm)
        {
            _result = await _hospital.ComfirmDonor(Request.Headers["Authorization"], confirm.ActivityId, confirm.DonorId);
            return Ok(_result);
        }
        [HttpPost("CreateRequestBlood")]
        [Authorize(Roles ="Hospital")]
        public async Task<IActionResult> CreateRequestBlood([FromBody] RequestDto requestDto)
        {
            _result = await _hospital.CreateRequestBlood(requestDto);
            return Ok(_result);
        }
        [HttpPost("GetDonorActivity/{activityID}")]
        [Authorize(Roles ="Hospital")]
        public async Task<IActionResult> GetDonorActivity(string activityID)
        {
            _result = await _hospital.GetDonorActivity(Request.Headers["Authorization"], activityID);
            return Ok(_result);
        }
    }
}
