using BloodCenter.Data.Dtos;
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
        }
        [HttpGet("ActivityIsGoing")]
        [AllowAnonymous]
        public async Task<IActionResult> ActivityIsGoing()
        {
            _result = new ModelResult();
            return Ok(_result);
        }
    }
}
