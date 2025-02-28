using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Data.Entities
{
    public class Donor
    {
        public Guid Id {  get; set; }
        public Account Account { get; set; }
        public virtual ICollection<History>? Histories { get; set; }
        public virtual ICollection<SessionDonor>? SessionDonors { get; set; }
    }
}
