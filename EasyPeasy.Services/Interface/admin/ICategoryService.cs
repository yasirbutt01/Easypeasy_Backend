using EasyPeasy.DataViewModels.Common;
using EasyPeasy.DataViewModels.Requests.admin;
using EasyPeasy.DataViewModels.Response.admin;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EasyPeasy.Services.Interface.admin
{
    public interface ICategoryService
    {
        Task<bool> SaveMasterCategory(Guid? id, string name, string UserId);
        GenralListResponse<GeneralCategoryResponse> GetMasterCategories(GenralListResponse<GeneralCategoryResponse> page, DateTime? str, DateTime? endd);
        Task<bool> SaveCategoryType(Guid? id, string name, string UserId);
        GenralListResponse<GeneralCategoryResponse> GetCategoryTypes(GenralListResponse<GeneralCategoryResponse> request, DateTime? str, DateTime? endd);
        GenralListResponse<GeneralCategoryResponse> GetCategoryTypes(Guid categoryId);
        Task<bool> SaveCategory(CreateCategoryRequest request, string UserId);
        CreateCategoryRequest GetEditCategory(Guid id);
        GenralListResponse<GetCategoryResponse> GetCategories(GenralListResponse<GetCategoryResponse> request, DateTime? str, DateTime? endd);
        GenralListResponse<GetMappedCategory> GetMasterToCategories(GenralListResponse<GetMappedCategory> page, DateTime? str, DateTime? endd);
        Task<bool> SaveMasterToCategories(GetMappedCategory request, string UserId);
        GenralListResponse<GetMappedCategory> GetCategoryToTypes(GenralListResponse<GetMappedCategory> page, DateTime? str, DateTime? endd);
        Task<bool> SaveCategoryToTypes(GetMappedCategory request, string UserId);
        GenralListResponse<GeneralCategoryTypesResponse> GetCategoryTypes(List<Guid> categoryIds);
    }
}
