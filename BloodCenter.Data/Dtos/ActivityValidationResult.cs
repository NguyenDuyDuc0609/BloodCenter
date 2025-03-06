using BloodCenter.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Data.Dtos
{
    public class ActivityValidationResult
    {
        public Account? Donor { get; set; }
        public Activity? Activity { get; set; }
    }
}
