﻿using BloodCenter.Data.Dtos;
using BloodCenter.Service.Cores.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BloodCenter.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class DonorController : ControllerBase
    {
        private readonly IDonor _donor;
        private ModelResult _result;
        public DonorController(IDonor donor)
        {
            _donor = donor;
            _result = new ModelResult();
        }
        [HttpGet("ActivityIsGoing")]
        [AllowAnonymous]
        public async Task<IActionResult> ActivityIsGoing(int pageNumber, int pageSize, int Status)
        {
            _result = await _donor.GetActivityIsGoing(pageNumber, pageSize, Status);
            return Ok(_result);
        }
        [HttpPost("RegisterDonate")]
        [Authorize(Roles ="Donor")]
        public async Task<IActionResult> RegisterDonateBlood([FromBody] string hospitalId)
        {
            _result = await _donor.RegisterDonate(Request.Headers["Authorization"], hospitalId);
            return Ok(_result);
        }
        [HttpPost("CancelDonation")]
        [Authorize(Roles ="Donor")]
        public async Task<IActionResult> CancelDonation([FromBody] string activityId)
        {
            _result = await _donor.CancelRegistration(Request.Headers["Authorization"], activityId);
            return Ok(_result);
        }
        [HttpGet("History")]
        [Authorize(Roles ="Donor")]
        public async Task<IActionResult> GetHistories(int pageNumber, int pageSize)
        {
            _result = await _donor.GetPersonalHistory(Request.Headers["Authorization"], pageNumber, pageSize);
            return Ok(_result);
        }
    }
}
