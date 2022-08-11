using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EasyPeasy.Services.Interface.v1
{
    public interface IBackgroundJobService
    {
        void RouteSchedulingJob(string rootpath);
    }
}
