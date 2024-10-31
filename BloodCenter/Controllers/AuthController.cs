using BloodCenter.Data.Dtos;
using BloodCenter.Data.Dtos.AuthDto;
using BloodCenter.Service.Cores.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BloodCenter.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuth _auth;
        private ModelResult _result;
        public AuthController(IAuth auth)
        {
            _auth = auth;
            _result = new ModelResult();
        }
        [HttpPost("signin")]
        [AllowAnonymous]
        public async Task<IActionResult> Resgiter([FromBody] RegisterDto regsiterDto)
        {
            _result = await _auth.Register(regsiterDto);
            return Ok(_result);
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            _result = await _auth.Login(loginDto);
            return Ok(_result);
        }
        [HttpGet("verify/{hashedEmail}")]
        [AllowAnonymous]
        public async Task<IActionResult> Verify(string hashedEmail)
        {
            _result = await _auth.EmailConfirm(hashedEmail);
            return Ok(_result);
        }
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshAccessToken([FromBody] RefreshDto refreshDto)
        {

            return Ok(_result);
        }
    }
}
