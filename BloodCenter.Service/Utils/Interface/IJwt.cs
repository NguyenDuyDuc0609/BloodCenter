using BloodCenter.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Service.Utils.Interface
{
    public interface IJwt
    {
        string GenerateJWT(Account account, List<string> roles);
        string GenerateRefreshToken();
    }
}
