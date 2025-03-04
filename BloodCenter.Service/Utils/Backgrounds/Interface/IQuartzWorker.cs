using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloodCenter.Service.Utils.Backgrounds.Interface
{
    public interface IQuartzWorker
    {
        Task DoWork(CancellationToken cancellationToken);
    }
}
