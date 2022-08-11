using EasyPeasy.Data.DTOs;
using EasyPeasy.DataViewModels.Requests.admin;
using EasyPeasy.DataViewModels.Response.admin;
using EasyPeasy.DataViewModels.Response.AdminResponse;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EasyPeasy.Services.Interface.admin
{ 
   public interface IBlogAdminService
    {
        List<AdminBlogResponse> GetBlogs(string SearchFilter, int skip, int take);
        AdminBlogResponse GetBlogById(Guid id);
        Task<bool> SaveAdminBlog(BlogAdminRequest model);
        Task<bool> PostSatus(int flag,Guid id);

        Task<bool> EditBlog(BlogAdminRequest model);
        Task<bool> DeleteBlog(Guid id);

    }
}
