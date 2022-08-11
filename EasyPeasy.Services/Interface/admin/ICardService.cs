using EasyPeasy.DataViewModels.Requests.admin;
using EasyPeasy.DataViewModels.Response.admin;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EasyPeasy.Services.Interface.admin
{
    public interface ICardService
    {
        GenralListResponse<GetCardResponse> GetCards(GenralListResponse<GetCardResponse> page, Guid? categoryId, DateTime? str, DateTime? endd);
        Task<bool> Save(SaveCardRequest request, string userId);
        SaveCardRequest GetEdit(Guid id);
        Task<bool> ControlActivation(Guid id, bool activation, string userId);
    }
}
