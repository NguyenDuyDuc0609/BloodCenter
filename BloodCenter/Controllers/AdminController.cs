using BloodCenter.Data.Dtos;
using BloodCenter.Data.Dtos.AuthDto;
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
    public class AdminController : ControllerBase
    {
        private readonly IAdmin _admin;
        private ModelResult _result;
        public AdminController(IAdmin admin)
        {
            _admin = admin;
            _result = new ModelResult();
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddNewHospital(RegisterDto registerDto)
        {
            _result = await _admin.AddNewHospital(registerDto);
            return Ok(_result);
        }
    }
}
