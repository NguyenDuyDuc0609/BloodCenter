using BloodCenter.Data.Abstractions;
using BloodCenter.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Data.Entities
{
    public class History : EntityAuditBase<Guid>
    {
        public Guid DonorId { get; set; }
        public Guid ActivityId { get; set; }

        public Donor? Donor { get; set; }

        public DateTimeOffset DonationDate { get; set; }

        public int Quantity { get; set; }

        public Guid HospitalId { get; set; }
        public StatusHistories StatusHistories { get; set; }
        public string HospitalName { get; set; }
    }
}
