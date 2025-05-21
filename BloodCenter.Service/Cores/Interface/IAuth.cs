using BloodCenter.Data.Dtos;
using BloodCenter.Data.Dtos.AuthDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Service.Cores.Interface
{
    public interface IAuth
    {
        public Task<ModelResult> Register(RegisterDto registerDto);
        public Task<ModelResult> Login(LoginDto loginDto);
        public Task<ModelResult> EmailConfirm(string hashedEmail);
        public Task<ModelResult> Refresh(RefreshDto refreshDto);
        public Task<ModelResult> ForgotPassword(string email);
        public Task<ModelResult> ResetPassword(string passwordTemp, string newPassword);
    }
}
