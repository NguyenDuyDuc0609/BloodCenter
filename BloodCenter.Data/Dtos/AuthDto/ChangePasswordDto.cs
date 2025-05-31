using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Data.Dtos.AuthDto
{
    public class ChangePasswordDto
    {
        public string username { get; set; }
        public string password { get; set; }
        public string newPassword { get; set; }
}
}
