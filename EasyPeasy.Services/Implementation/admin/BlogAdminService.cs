using EasyPeasy.DataViewModels.Response.admin;
using EasyPeasy.Services.Interface.admin;
using EasyPeasy.Data.Context;
using EasyPeasy.Data.DTOs;

using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyPeasy.DataViewModels.Requests.admin;

namespace EasyPeasy.Services.Implementation.admin
{
    public class BlogAdminService : IBlogAdminService
    {
        public AdminBlogResponse GetBlogById(Guid id)
        {
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    return db.Blogs
                        .Where(x => x.Id.Equals(id))
                        .Select(x => new AdminBlogResponse
                        {
                            BlogId = x.Id,
                            Title = x.Title,
                            Date = x.CreatedOn,
                            Description = x.Text,
                            ImageUrl = x.FileUrl,
                            ImageThumbnailUrl = x.FileThumbnailUrl,
                            IsEnabled = x.IsEnabled,
                            metaTitle = x.MetaTag,
                            metaDescription = x.MetaDiscription

                        })
                        .FirstOrDefault() ?? new AdminBlogResponse();
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public List<AdminBlogResponse> GetBlogs(string SearchFilter, int skip, int take)
        {
            try
            {
                IQueryable<AdminBlogResponse> query;
                using (var db = new EasypeasyDbContext())
                {
                    query = db.Blogs
                        .Select(x => new AdminBlogResponse
                        {
                            BlogId = x.Id,
                            Title = x.Title,
                            Date = x.CreatedOn,
                            ImageUrl = x.FileUrl,
                            ImageThumbnailUrl = x.FileThumbnailUrl,
                            IsEnabled = x.IsEnabled
                        })
                         .AsQueryable();

                    if (!string.IsNullOrEmpty(SearchFilter))
                    {
                        int totalCases = -1;
                        var isNumber = Int32.TryParse(SearchFilter, out totalCases);
                        if (isNumber)
                        {
                            //query = query.Where(
                            //    x => x.TotalCases == totalCases
                            //);
                        }
                        else
                        {
                            query = query.Where(
                            x => x.Title.ToLower().Contains(SearchFilter.ToLower())
                        );
                        }
                    }
                    return query.OrderByDescending(x => x.Date).Skip(skip).Take(take).ToList();
                }

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> SaveAdminBlog(BlogAdminRequest model)
        {
            try
            {
                bool response = false;
                using (var db = new EasypeasyDbContext())
                {
                    using (var transaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            //Save user
                            await db.Blogs.AddAsync(new Blogs
                            {
                                Id = model.Id,
                                FileThumbnailUrl = model.ImageThumbnailUrl,
                                Title = model.Title,
                                FileUrl = model.ImageUrl,
                                Text = model.description,
                                IsEnabled = true,
                                CreatedOn = DateTime.UtcNow,
                                CreatedOnDate = model.PublishDate,
                                CreatedBy = "Admin",
                                MetaDiscription = model.metaDescription,
                                MetaTag = model.metaTitle,
                                SlugUrl = model.slugUrl
                            });
                            await db.SaveChangesAsync();
                            transaction.Commit();

                            response = true;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw ex;
                        }
                    }
                }

                return response;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<bool> PostSatus(int flag, Guid id)
        {

            bool res = false;
            using (var db = new EasypeasyDbContext())
            {
                try
                {
                    var model = db.Blogs.Find(id);
                    switch (flag)
                    {
                        case 1:
                            if (model != null)
                            {
                                model.IsEnabled = model.IsEnabled != true;
                                db.Entry(model).State = EntityState.Modified;
                            }

                            db.SaveChanges();
                            res = true;
                            break;
                        case 2:
                            if (model != null)
                            {
                                model.IsEnabled = model.IsEnabled == false;
                                db.Entry(model).State = EntityState.Modified;
                            }

                            db.SaveChanges();
                            res = true;
                            break;
                    }


                }
                catch (Exception ex)
                {

                    throw ex;
                }

            }
            return res;


        }

        public async Task<bool> EditBlog(BlogAdminRequest model)
        {
            bool res = false;
            using (var db = new EasypeasyDbContext())
            {
                try
                {
                    var getblog = db.Blogs.Find(model.Id);
                    if (model != null)
                    {
                        getblog.Id = model.Id;
                        getblog.IsEnabled = model.isEnabled;
                        getblog.FileThumbnailUrl = model.ImageThumbnailUrl;
                        getblog.FileUrl = model.ImageUrl;
                        getblog.Text = model.description;
                        getblog.UpdatedBy = "Admin";
                        getblog.Title = model.Title;
                        getblog.UpdatedOn = DateTime.UtcNow;
                        getblog.MetaTag = model.metaTitle;
                        getblog.MetaDiscription = model.metaDescription;
                        getblog.SlugUrl = model.slugUrl;
                        db.Entry(getblog).State = EntityState.Modified;
                    }

                    db.SaveChanges();
                    res = true;


                }
                catch (Exception ex)
                {

                    throw ex;
                }

            }
            return res;
        }

        public async Task<bool> DeleteBlog(Guid id)
        {
            bool res = false;
            using (var db = new EasypeasyDbContext())
            {
                try
                {
                    var getblog = db.Blogs.Find(id);
                    if (getblog != null)
                    {
                        db.Entry(getblog).State = EntityState.Deleted;
                    }
                    db.SaveChanges();
                    res = true;


                }
                catch (Exception ex)
                {

                    throw ex;
                }

            }
            return res;
        }
    }
}
