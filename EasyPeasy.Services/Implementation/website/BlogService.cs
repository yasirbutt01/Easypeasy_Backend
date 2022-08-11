using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EasyPeasy.Common;
using EasyPeasy.Data.Context;
using EasyPeasy.Services.Interface.website;
using System.Linq;
using EasyPeasy.Data.DTOs;
using EasyPeasy.DataViewModels.Requests.website;
using EasyPeasy.DataViewModels.Response.website;
using Microsoft.EntityFrameworkCore;

namespace EasyPeasy.Services.Implementation.website
{
    public class BlogService: IBlogService
    {
        public List<BlogResponse> Get(int skip, int take)
        {
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    var totalBlogs = db.Blogs.Count(x => x.IsEnabled);
                    var list = db.Blogs
                        .Where(x => x.IsEnabled)
                        .Select(x => new BlogResponse
                        {
                            Id = x.Id,
                            Title = x.Title,
                            MetaDescription = x.MetaDiscription,
                            MetaTag = x.MetaTag,
                            SlugUrl = x.SlugUrl,
                            Date = x.CreatedOn,
                            Description = x.Text,
                            ImageUrl = x.FileUrl,
                            ImageThumbnailUrl = x.FileThumbnailUrl
                        })
                        .OrderByDescending(x => x.Date)
                        .Skip(skip)
                        .Take(take)
                        .ToList();
                    
                    list.ForEach(x =>
                    {
                        x.TotalBlogCount = totalBlogs;
                    });

                    return list;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public BlogResponse BlogDetail(string slugUrl)
        {
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    var response = db.Blogs
                        .Where(x => x.SlugUrl.Equals(slugUrl))
                        .Select(x => new BlogResponse
                        {
                            Id = x.Id,
                            Title = x.Title,
                            MetaDescription = x.MetaDiscription,
                            MetaTag = x.MetaTag,
                            SlugUrl = x.SlugUrl,
                            Date = x.CreatedOn,
                            Description = x.Text,
                            ImageUrl = x.FileUrl,
                            ImageThumbnailUrl = x.FileThumbnailUrl,
                        })
                        .FirstOrDefault() ?? new BlogResponse();

                    return response;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}
