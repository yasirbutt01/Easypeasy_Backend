using EasyPeasy.DataViewModels.Common;
using EasyPeasy.DataViewModels.Requests;
using EasyPeasy.DataViewModels.Response.v1;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EasyPeasy.Services.Interface.v1
{
    public interface ICommonService
    {
        Task<bool> IsEmailExists(string email);
        Task<List<CommonFaqsResponse>> GetFaqs();
        Task<List<General<int>>> GetFonts();
        Task<List<CommonGetPackagesResponse>> GetPackages(Guid userId);

        Task<StaticContentDataResponse> GetStaticContent(int type);

        Task<bool> AddUserFeedback(Guid userId, FeedBackRequest request);
    }
}
