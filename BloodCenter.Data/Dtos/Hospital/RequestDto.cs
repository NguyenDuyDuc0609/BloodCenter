using BloodCenter.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Data.Dtos.Hospital
{
    public class RequestDto
    {
        public Guid HospitalId { get; set; }

        public Guid? HospitalAccept { get; set; }

        public string BloodType { get; set; }

        public int Quantity { get; set; }

        public StatusRequestBlood Status { get; set; }

        public string? Address { get; set; }
    }
}
