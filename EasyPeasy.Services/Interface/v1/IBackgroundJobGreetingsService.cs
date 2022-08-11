using System;
using System.Collections.Generic;
using System.Text;

namespace EasyPeasy.Services.Interface.v1
{
    public interface IBackgroundJobGreetingsService
    {
        void RouteSchedulingGreetingJob(string rootpath);
    }
}
