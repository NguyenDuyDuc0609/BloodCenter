using BloodCenter.Data.Abstractions.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Data.Abstractions
{
    public abstract class EntityAuditBase<T> : IEntityAuditBase<T>
    {
        public T Id { get; set; }
        public bool IsDelete { get ; set ; }
        public DateTimeOffset DeleteDate { get ; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public DateTimeOffset ModifiedDate { get; set; }
    }
}
