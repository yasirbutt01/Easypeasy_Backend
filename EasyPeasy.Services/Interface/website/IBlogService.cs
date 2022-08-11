using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EasyPeasy.DataViewModels.Requests.website;
using EasyPeasy.DataViewModels.Response.website;

namespace EasyPeasy.Services.Interface.website
{
    public interface IBlogService
    {
        List<BlogResponse> Get(int skip, int take);
        BlogResponse BlogDetail(string slugUrl);
    }
}
