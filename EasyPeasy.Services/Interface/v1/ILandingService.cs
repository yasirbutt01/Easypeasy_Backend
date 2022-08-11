
using EasyPeasy.DataViewModels.Requests.v1;
using EasyPeasy.DataViewModels.Response.v1;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EasyPeasy.Services.Interface.v1
{
    public interface ILandingService
    {
        Task<List<string>> GetRecentSearches(Guid userId);
        Task<bool> ClearAllSearches(Guid userId);
        Task<LandingCategoriesWithCardResponse> GetCategoriesWithCards(LandingGetCategoriesWithCardsRequest request, Guid userId);
        Task<LandingGetCardResponse> GetCards(int skip, int take, Guid categoryId);
        Task<LandingCategoryResponse> GetCategoryById(Guid categoryId);
    }
}
