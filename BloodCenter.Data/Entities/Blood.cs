using BloodCenter.Data.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Data.Entities
{
    public class Blood : EntityAuditBase<Guid>
    {
        public Guid HospitalId { get; set; }

        public Hospital? Hospital { get; set; }

        public string BloodType { get; set; }

        public int Quantity { get; set; }

        public DateTime ExpiryDate { get; set; }

    }
}
