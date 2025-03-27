using BloodCenter.Data.Abstractions;
using BloodCenter.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Data.Entities
{
    public class RequestBlood : EntityAuditBase<Guid>
    {
            public Guid HospitalId { get; set; }

            public Hospital? Hospital { get; set; }

            public Guid? HospitalAccept { get; set; }

            public string BloodType { get; set; }

            public int Quantity { get; set; }

            public StatusRequestBlood Status { get; set; }

            public string? Address { get; set; }
    }
}
