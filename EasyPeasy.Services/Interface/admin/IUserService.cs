using EasyPeasy.DataViewModels.Common;
using EasyPeasy.DataViewModels.Response.admin;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EasyPeasy.Services.Interface.admin
{
    public interface IUserService
    {
        GenralListResponse<GetUserResponse> GetUsers(GenralListResponse<GetUserResponse> page, DateTime? str, DateTime? endd, List<Guid> packages);
        Task<bool> ControlActivation(Guid id, bool activation, string userId);
        Task<UserDetailResponse> UserDetail(Guid userId);
        List<General<Guid>> GetPackages();
        EGiftDetail GetEGiftDetail(Guid id);
    }
}
