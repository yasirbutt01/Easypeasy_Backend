using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EasyPeasy.DataViewModels.Response.admin;

namespace EasyPeasy.Services.Interface.admin
{
    public interface IHomeService
    {
        Task<HomeCountsResponse> GetHomeCounts();
    }
}
