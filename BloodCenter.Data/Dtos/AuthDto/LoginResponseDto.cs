using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Data.Dtos.AuthDto
{
    public class LoginResponseDto
    {
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Token { get; set; }
        public string refreshToken { get; set; }
        public List<string> Role { get; set; }
    }
}
