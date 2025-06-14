﻿using BloodCenter.Data.Dtos;
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
        [AllowAnonymous]
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
            _result = await _auth.Refresh(refreshDto);
            return Ok(_result);
        }
        [HttpPost("Forgotpassword")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest forgotPasswordRequest)
        {
            _result = await _auth.ForgotPassword(forgotPasswordRequest.Email);
            return Ok(_result);
        }
        [HttpPost("ResetPassword")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPassworDto resetPassworDto)
        {
            _result = await _auth.ResetPassword(resetPassworDto.Username , resetPassworDto.Password, resetPassworDto.NewPassword);
            return Ok(_result);
        }
        [HttpPost("ChangePassword")]
        [AllowAnonymous]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            _result = await _auth.ChangePassword(changePasswordDto.username, changePasswordDto.password, changePasswordDto.newPassword);
            return Ok(_result);
        }
    }
}
