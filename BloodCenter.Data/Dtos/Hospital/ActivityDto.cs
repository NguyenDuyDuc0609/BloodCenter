using BloodCenter.Data.Entities;
using BloodCenter.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Data.Dtos.Hospital
{
    public class ActivityDto
    {
        public DateTime DateActivity { get; set; }

        public string OperatingHour { get; set; }

        public int Quantity { get; set; }

        public int NumberIsRegistration { get; set; } = 0;

        public StatusActivity Status { get; set; }
    }
}
