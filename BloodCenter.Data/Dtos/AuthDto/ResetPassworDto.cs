using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Data.Dtos.AuthDto
{
    public class ResetPassworDto
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? NewPassword { get; set; }
    }
}
