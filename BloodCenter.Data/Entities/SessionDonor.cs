using BloodCenter.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Data.Entities
{
    public class SessionDonor
    {
        public Guid Id { get; set; }
        public Guid DonorId { get; set; }

        public Donor? Donor { get; set; }

        public Guid ActivityId { get; set; }

        public Activity? Activity { get; set; }

        public StatusSession Status { get; set; }
    }
}
