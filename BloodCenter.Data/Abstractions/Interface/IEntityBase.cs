﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Data.Abstractions.Interface
{
    public interface IEntityBase<T>
    {
        T Id { get; set; }
    }
}
