using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Data.Abstractions.IEntities
{
    public interface IAccount
    {
        public string FullName { get; set; }
        public string refreshToken { get; set; }
        public DateTime? createAt { get; set; }
        public DateTime? expiresAt { get; set; }
    }
}
