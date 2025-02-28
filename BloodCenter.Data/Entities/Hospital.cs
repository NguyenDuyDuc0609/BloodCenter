using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Data.Entities
{
    public class Hospital
    {
        public Guid Id { get; set; }
        public virtual ICollection<Blood>? Bloods { get; set; }
        public virtual ICollection<Activity>? Activities { get; set; }
        public Account Account { get; set; }
    }

}
