using EasyPeasy.Common;
using EasyPeasy.Data.Context;
using EasyPeasy.Data.DTOs;
using EasyPeasy.DataViewModels.Common;
using EasyPeasy.DataViewModels.Requests.admin;
using EasyPeasy.DataViewModels.Response.admin;
using EasyPeasy.Services.Interface.admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyPeasy.Services.Implementation.admin
{
    public class CategoryService : ICategoryService
    {

        public async Task<bool> SaveMasterCategory(Guid? id, string name, string UserId)
        {
            try
            {
                bool response = false;

                using (var db = new EasypeasyDbContext())
                {
                    if (db.MasterCategories.Any(x => x.IsEnabled && x.Name.ToLower().Equals(name.ToLower()))) throw new ArgumentException("Category Name Already Exists");

                    if (id == null)
                    {
                        await db.MasterCategories.AddAsync(new MasterCategories
                        {
                            Id = SystemGlobal.GetId(),
                            Name = name,
                            IsEnabled = true,
                            CreatedOnDate = DateTime.UtcNow,
                            CreatedBy = UserId.ToString(),
                            CreatedOn = DateTime.UtcNow,
                            UpdatedOn = DateTime.UtcNow,
                            UpdatedBy = UserId.ToString(),
                        });

                        await db.SaveChangesAsync();

                        response = true;
                    }
                    else
                    {
                        var category = db.MasterCategories.FirstOrDefault(x => x.Id.Equals(id));

                        if (category == null) throw new ArgumentException("Category id doesn't exist");

                        category.Name = name;
                        category.UpdatedOn = DateTime.UtcNow;
                        category.UpdatedBy = UserId.ToString();

                        db.Entry(category).State = EntityState.Modified;
                        await db.SaveChangesAsync();

                        response = true;
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public GenralListResponse<GeneralCategoryResponse> GetMasterCategories(GenralListResponse<GeneralCategoryResponse> page, DateTime? str, DateTime? endd)
        {
            DateTime strt = Convert.ToDateTime(str);
            DateTime end = Convert.ToDateTime(endd).AddDays(1);
            GenralListResponse<GeneralCategoryResponse> data = new GenralListResponse<GeneralCategoryResponse>();

            using (var db = new EasypeasyDbContext())
            {
                var query = db.MasterCategories.Where(x => x.CreatedOn >= strt && x.CreatedOn <= end && x.IsEnabled)
               .Select(x => new GeneralCategoryResponse
               {
                   Id = x.Id,
                   Name = x.Name,
                   LastUpdatedDate = x.UpdatedOn ?? DateTime.UtcNow,
               }).AsQueryable();

                if (!string.IsNullOrEmpty(page.Search))
                {
                    var date = new DateTime();
                    var sdate = DateTime.TryParse(page.Search, out date);
                    int totalCases = -1;
                    var isNumber = Int32.TryParse(page.Search, out totalCases);
                    if (sdate)
                        query = query.Where(
                            x => x.LastUpdatedDate.Date == date.Date
                        );
                    else if (isNumber)
                    {
                        //query = query.Where(
                        //    x => x.TotalCases == totalCases
                        //);
                    }
                    else
                    {
                        query = query.Where(
                        x => x.Name.ToLower().Contains(page.Search.ToLower())
                    );
                    }
                }
                var orderedQuery = query.OrderByDescending(x => x.Name);
                switch (page.SortIndex)
                {
                    case 0:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name);
                        break;
                    case 1:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.LastUpdatedDate) : query.OrderBy(x => x.LastUpdatedDate);
                        break;
                }


                data.Page = page.Page;
                data.PageSize = page.PageSize;
                data.TotalRecords = orderedQuery.Count();
                data.Draw = page.Draw;
                data.Entities = orderedQuery.Skip(page.Page).Take(page.PageSize).ToList();
            }

            return data;
        }

        public async Task<bool> SaveCategoryType(Guid? id, string name, string UserId)
        {
            try
            {
                bool response = false;

                using (var db = new EasypeasyDbContext())
                {
                    if (db.CategoryTypes.Any(x => x.IsEnabled && x.Name.ToLower().Equals(name.ToLower()))) throw new ArgumentException("Category Name Already Exists");

                    if (id == null)
                    {
                        await db.CategoryTypes.AddAsync(new CategoryTypes
                        {
                            Id = SystemGlobal.GetId(),
                            Name = name,
                            IsEnabled = true,
                            CreatedOnDate = DateTime.UtcNow,
                            CreatedBy = UserId.ToString(),
                            CreatedOn = DateTime.UtcNow,
                            UpdatedOn = DateTime.UtcNow,
                            UpdatedBy = UserId.ToString(),
                        });

                        await db.SaveChangesAsync();

                        response = true;
                    }
                    else
                    {
                        var category = db.CategoryTypes.FirstOrDefault(x => x.Id.Equals(id));

                        if (category == null) throw new ArgumentException("Category id doesn't exist");

                        category.Name = name;
                        category.UpdatedOn = DateTime.UtcNow;
                        category.UpdatedBy = UserId.ToString();

                        db.Entry(category).State = EntityState.Modified;
                        await db.SaveChangesAsync();

                        response = true;
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public GenralListResponse<GeneralCategoryResponse> GetCategoryTypes(GenralListResponse<GeneralCategoryResponse> page, DateTime? str, DateTime? endd)
        {
            DateTime strt = Convert.ToDateTime(str);
            DateTime end = Convert.ToDateTime(endd).AddDays(1);
            GenralListResponse<GeneralCategoryResponse> data = new GenralListResponse<GeneralCategoryResponse>();

            using (var db = new EasypeasyDbContext())
            {
                var query = db.CategoryTypes.Where(x => x.CreatedOn >= strt && x.CreatedOn <= end && x.IsEnabled)
               .Select(x => new GeneralCategoryResponse
               {
                   Id = x.Id,
                   Name = x.Name,
                   LastUpdatedDate = x.UpdatedOn ?? DateTime.UtcNow,
               }).AsQueryable();

                if (!string.IsNullOrEmpty(page.Search))
                {
                    var date = new DateTime();
                    var sdate = DateTime.TryParse(page.Search, out date);
                    int totalCases = -1;
                    var isNumber = Int32.TryParse(page.Search, out totalCases);
                    if (sdate)
                        query = query.Where(
                            x => x.LastUpdatedDate.Date == date.Date
                        );
                    else if (isNumber)
                    {
                        //query = query.Where(
                        //    x => x.TotalCases == totalCases
                        //);
                    }
                    else
                    {
                        query = query.Where(
                        x => x.Name.ToLower().Contains(page.Search.ToLower())
                    );
                    }
                }
                var orderedQuery = query.OrderByDescending(x => x.Name);
                switch (page.SortIndex)
                {
                    case 0:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name);
                        break;
                    case 1:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.LastUpdatedDate) : query.OrderBy(x => x.LastUpdatedDate);
                        break;
                }


                data.Page = page.Page;
                data.PageSize = page.PageSize;
                data.TotalRecords = orderedQuery.Count();
                data.Draw = page.Draw;
                data.Entities = orderedQuery.Skip(page.Page).Take(page.PageSize).ToList();
            }

            return data;
        }

        public GenralListResponse<GeneralCategoryResponse> GetCategoryTypes(Guid categoryId)
        {
            GenralListResponse<GeneralCategoryResponse> data = new GenralListResponse<GeneralCategoryResponse>();

            using (var db = new EasypeasyDbContext())
            {
                var query = db.MapCategoryCategoryTypes.Where(x => x.IsEnabled && x.CategoryId.Equals(categoryId))
               .Select(x => new GeneralCategoryResponse
               {
                   Id = x.CategoryTypeId,
                   Name = x.CategoryType.Name,
                   LastUpdatedDate = x.CategoryType.UpdatedOn ?? DateTime.UtcNow,
               }).AsQueryable();

                data.Page = 0;
                data.PageSize = 50000;
                data.TotalRecords = query.Count();
                data.Draw = -1;
                data.Entities = query.OrderByDescending(x => x.Name).ToList();
            }

            return data;
        }

        public GenralListResponse<GeneralCategoryTypesResponse> GetCategoryTypes(List<Guid> categoryIds)
        {
            GenralListResponse<GeneralCategoryTypesResponse> data = new GenralListResponse<GeneralCategoryTypesResponse>();

            using (var db = new EasypeasyDbContext())
            {
                var query = db.MapCategoryCategoryTypes.Where(x => x.IsEnabled && categoryIds.Contains(x.CategoryId))
               .Select(x => new GeneralCategoryTypesResponse
               {
                   Id = x.CategoryTypeId,
                   CategoryId = x.CategoryId,
                   Name = x.CategoryType.Name,
                   LastUpdatedDate = x.CategoryType.UpdatedOn ?? DateTime.UtcNow,
               }).AsQueryable();

                data.Page = 0;
                data.PageSize = 50000;
                data.TotalRecords = query.Count();
                data.Draw = -1;
                data.Entities = query.OrderByDescending(x => x.Name).ToList();
            }

            return data;
        }

        public async Task<bool> SaveCategory(CreateCategoryRequest request, string UserId)
        {
            try
            {
                bool response = false;
                List<CategoryMusicFiles> categoryMusicFiles = new List<CategoryMusicFiles>();

                foreach (var music in request.MusicFiles)
                {
                    categoryMusicFiles.Add(new CategoryMusicFiles
                    {
                        Id = SystemGlobal.GetId(),
                        Name = music.Name,
                        Mp3FileUrl = music.Mp3FileUrl,
                        WavFileUrl = string.IsNullOrEmpty(music.WavFileUrl) ? "" : music.WavFileUrl,
                        IsEnabled = true,
                        CreatedOnDate = DateTime.UtcNow,
                        CreatedBy = UserId.ToString(),
                        CreatedOn = DateTime.UtcNow,
                    });
                }

                using (var db = new EasypeasyDbContext())
                {
                    using (var trans = db.Database.BeginTransaction())
                    {
                        try
                        {
                            if (Guid.Empty.Equals(request.Id))
                            {
                                if (db.Categories.Any(x => x.IsEnabled && x.Name.ToLower().Equals(request.Name.ToLower()))) throw new ArgumentException("Category Name Already Exists");
                                await db.Categories.AddAsync(new Categories
                                {
                                    Id = SystemGlobal.GetId(),
                                    Name = request.Name,
                                    Animation = request.Animation,
                                    Icon = request.Icon,
                                    EventDate = request.EventDate,
                                    IconFileUrl = request.IconFileUrl,
                                    IconFileThumbnailUrl = request.IconFileThumbnailUrl,
                                    WebActiveIconFileUrl = request.WebActiveIconFileUrl,
                                    WebActiveIconFileThumbnailUrl = request.WebActiveIconFileThumbnailUrl,
                                    WebInactiveIconFileThumbnailUrl = request.WebInactiveIconFileThumbnailUrl,
                                    WebInactiveIconFileUrl = request.WebInactiveIconFileUrl,
                                    SealColor = request.SealColor,
                                    GifFileUrl = request.GifFileUrl,
                                    JsonFileUrl = request.JsonFileUrl,
                                    CategoryMusicFiles = categoryMusicFiles,
                                    IsEnabled = true,
                                    CreatedOnDate = DateTime.UtcNow,
                                    CreatedBy = UserId.ToString(),
                                    CreatedOn = DateTime.UtcNow,
                                    UpdatedOn = DateTime.UtcNow,
                                    UpdatedBy = UserId.ToString(),
                                });

                                await db.SaveChangesAsync();

                                response = true;
                            }
                            else
                            {
                                var category = db.Categories.FirstOrDefault(x => x.Id.Equals(request.Id));

                                if (category == null) throw new ArgumentException("Category id doesn't exist");

                                //RemoveOlder
                                await db.CategoryMusicFiles.Where(x => x.IsEnabled && x.CategoryId.Equals(request.Id)).ForEachAsync(x => { x.IsEnabled = false; x.DeletedOn = DateTime.UtcNow; x.DeletedBy = UserId.ToString(); });
                                await db.SaveChangesAsync();

                                category.Name = request.Name;
                                category.Animation = request.Animation;
                                category.EventDate = request.EventDate;
                                category.Icon = request.Icon;
                                category.IconFileUrl = request.IconFileUrl;
                                category.IconFileThumbnailUrl = request.IconFileThumbnailUrl;
                                category.SealColor = request.SealColor;
                                category.GifFileUrl = request.GifFileUrl;
                                category.JsonFileUrl = request.JsonFileUrl;
                                category.WebActiveIconFileUrl = request.WebActiveIconFileUrl;
                                category.WebActiveIconFileThumbnailUrl = request.WebActiveIconFileThumbnailUrl;
                                category.WebInactiveIconFileThumbnailUrl = request.WebInactiveIconFileThumbnailUrl;
                                category.WebInactiveIconFileUrl = request.WebInactiveIconFileUrl;
                                category.UpdatedOn = DateTime.UtcNow;
                                category.UpdatedBy = UserId.ToString();

                                db.Entry(category).State = EntityState.Modified;
                                await db.SaveChangesAsync();

                                categoryMusicFiles.ForEach(x => x.CategoryId = request.Id);

                                db.CategoryMusicFiles.AddRange(categoryMusicFiles);
                                await db.SaveChangesAsync();

                                response = true;
                            }

                            trans.Commit();
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();
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

        public CreateCategoryRequest GetEditCategory(Guid id)
        {
            using (var db = new EasypeasyDbContext())
            {
                return db.Categories.Where(x => x.Id.Equals(id))
               .Select(x => new CreateCategoryRequest
               {
                   Id = x.Id,
                   Name = x.Name,
                   Icon = x.Icon,
                   EventDate = x.EventDate,
                   SealColor = x.SealColor,
                   Animation = x.Animation,
                   GifFileUrl = x.GifFileUrl,
                   IconFileUrl = x.IconFileUrl,
                   IconFileThumbnailUrl = x.IconFileThumbnailUrl,
                   WebActiveIconFileUrl = x.WebActiveIconFileUrl,
                   WebActiveIconFileThumbnailUrl = x.WebActiveIconFileThumbnailUrl,
                   WebInactiveIconFileUrl = x.WebInactiveIconFileUrl,
                   WebInactiveIconFileThumbnailUrl = x.WebInactiveIconFileThumbnailUrl,
                   JsonFileUrl = x.JsonFileUrl,
                   MusicFiles = x.CategoryMusicFiles.Where(y => y.IsEnabled).OrderBy(x => x.Name).Select(y => new MusicFile
                   {
                       Id = y.Id,
                       Name = y.Name,
                       Mp3FileUrl = y.Mp3FileUrl,
                       WavFileUrl = y.WavFileUrl
                   }).ToList()
               }).FirstOrDefault();
            }

        }

        public GenralListResponse<GetCategoryResponse> GetCategories(GenralListResponse<GetCategoryResponse> page, DateTime? str, DateTime? endd)
        {
            DateTime strt = Convert.ToDateTime(str);
            DateTime end = Convert.ToDateTime(endd).AddDays(1);
            GenralListResponse<GetCategoryResponse> data = new GenralListResponse<GetCategoryResponse>();

            using (var db = new EasypeasyDbContext())
            {
                var query = db.Categories.Where(x => x.CreatedOn >= strt && x.CreatedOn <= end && x.IsEnabled)
               .Select(x => new GetCategoryResponse
               {
                   Id = x.Id,
                   Name = x.Name,
                   Icon = x.Icon,
                   SealColor = x.SealColor,
                   Animation = x.Animation,
                   MusicCount = x.CategoryMusicFiles.Count(y => y.IsEnabled),
                   LastUpdatedDate = x.UpdatedOn ?? new DateTime(),
               }).AsQueryable();

                if (!string.IsNullOrEmpty(page.Search))
                {
                    var date = new DateTime();
                    var sdate = DateTime.TryParse(page.Search, out date);
                    int totalCases = -1;
                    var isNumber = Int32.TryParse(page.Search, out totalCases);
                    if (sdate)
                        query = query.Where(
                            x => x.LastUpdatedDate.Date == date.Date
                        );
                    else if (isNumber)
                    {
                        //query = query.Where(
                        //    x => x.TotalCases == totalCases
                        //);
                    }
                    else
                    {
                        query = query.Where(
                        x => x.Name.ToLower().Contains(page.Search.ToLower())
                    );
                    }
                }
                var orderedQuery = query.OrderByDescending(x => x.Name);
                switch (page.SortIndex)
                {
                    case 0:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name);
                        break;
                    case 1:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.SealColor) : query.OrderBy(x => x.SealColor);
                        break;
                    case 2:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.Icon) : query.OrderBy(x => x.Icon);
                        break;
                    case 3:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.Animation) : query.OrderBy(x => x.Animation);
                        break;
                    case 4:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.MusicCount) : query.OrderBy(x => x.MusicCount);
                        break;
                    case 5:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.LastUpdatedDate) : query.OrderBy(x => x.LastUpdatedDate);
                        break;
                }


                data.Page = page.Page;
                data.PageSize = page.PageSize;
                data.TotalRecords = orderedQuery.Count();
                data.Draw = page.Draw;
                data.Entities = orderedQuery.Skip(page.Page).Take(page.PageSize).ToList();
            }

            return data;
        }

        public GenralListResponse<GetMappedCategory> GetMasterToCategories(GenralListResponse<GetMappedCategory> page, DateTime? str, DateTime? endd)
        {
            DateTime strt = Convert.ToDateTime(str);
            DateTime end = Convert.ToDateTime(endd).AddDays(1);
            GenralListResponse<GetMappedCategory> data = new GenralListResponse<GetMappedCategory>();

            using (var db = new EasypeasyDbContext())
            {
                var query = db.MasterCategories.Where(x => x.CreatedOn >= strt && x.CreatedOn <= end && x.IsEnabled)
               .Select(x => new GetMappedCategory
               {
                   Id = x.Id,
                   Name = x.Name,
                   LastUpdatedDate = x.UpdatedOn ?? DateTime.UtcNow,
                   Mapped = x.CategoryGroups.Where(y => y.IsEnabled).Select(y => new GetMapped
                   {
                       Id = y.Id,
                       MasterId = y.CategoryId,
                       Name = y.Category.Name
                   }).ToList()
               }).AsQueryable();

                if (!string.IsNullOrEmpty(page.Search))
                {
                    var date = new DateTime();
                    var sdate = DateTime.TryParse(page.Search, out date);
                    int totalCases = -1;
                    var isNumber = Int32.TryParse(page.Search, out totalCases);
                    if (sdate)
                        query = query.Where(
                            x => x.LastUpdatedDate.Date == date.Date
                        );
                    else if (isNumber)
                    {
                        //query = query.Where(
                        //    x => x.TotalCases == totalCases
                        //);
                    }
                    else
                    {
                        query = query.Where(
                        x => x.Name.ToLower().Contains(page.Search.ToLower())
                    );
                    }
                }
                var orderedQuery = query.OrderByDescending(x => x.Name);
                switch (page.SortIndex)
                {
                    case 0:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name);
                        break;
                    case 1:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.LastUpdatedDate) : query.OrderBy(x => x.LastUpdatedDate);
                        break;
                }


                data.Page = page.Page;
                data.PageSize = page.PageSize;
                data.TotalRecords = orderedQuery.Count();
                data.Draw = page.Draw;
                data.Entities = orderedQuery.Skip(page.Page).Take(page.PageSize).ToList();
            }

            return data;
        }

        public async Task<bool> SaveMasterToCategories(GetMappedCategory request, string userId)
        {
            try
            {
                bool response = false;

                using (var db = new EasypeasyDbContext())
                {
                    using (var trans = db.Database.BeginTransaction())
                    {
                        try
                        {
                            if (request.Mapped == null) request.Mapped = new List<GetMapped>();

                            var categoryGroup = db.CategoryGroups.Where(x => x.IsEnabled && x.MasterCategoryId.Equals(request.Id)).ToList();

                            // Delete children
                            foreach (var existingChild in categoryGroup)
                            {

                                if (!request.Mapped.Any(c => c.Id == existingChild.Id))
                                {
                                    var mapCategory = db.CategoryGroups.Find(existingChild.Id);

                                    mapCategory.IsEnabled = false;
                                    mapCategory.DeletedBy = userId;
                                    mapCategory.DeletedOn = DateTime.UtcNow;

                                    db.Entry(mapCategory).State = EntityState.Modified;
                                }
                            }

                            // Update and Insert children
                            foreach (var childModel in request.Mapped)
                            {
                                var existingChild = categoryGroup
                                    .Where(c => c.Id == childModel.Id)
                                    .SingleOrDefault();

                                if (existingChild != null)
                                {
                                    // Update child
                                    existingChild.MasterCategoryId = request.Id;
                                    existingChild.CategoryId = childModel.MasterId;
                                    existingChild.UpdatedBy = userId;
                                    existingChild.UpdatedOn = DateTime.UtcNow;

                                    db.Entry(existingChild).State = EntityState.Modified;
                                }
                                else
                                {
                                    // Insert child
                                    var newChild = new CategoryGroups
                                    {
                                        Id = SystemGlobal.GetId(),
                                        MasterCategoryId = request.Id,
                                        CategoryId = childModel.MasterId,
                                        IsEnabled = true,
                                        CreatedBy = userId,
                                        CreatedOn = DateTime.UtcNow
                                    };

                                    db.CategoryGroups.Add(newChild);
                                }
                            }

                            await db.SaveChangesAsync();

                            trans.Commit();
                            response = true;
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();
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

        public GenralListResponse<GetMappedCategory> GetCategoryToTypes(GenralListResponse<GetMappedCategory> page, DateTime? str, DateTime? endd)
        {
            DateTime strt = Convert.ToDateTime(str);
            DateTime end = Convert.ToDateTime(endd).AddDays(1);
            GenralListResponse<GetMappedCategory> data = new GenralListResponse<GetMappedCategory>();

            using (var db = new EasypeasyDbContext())
            {
                var query = db.Categories.Where(x => x.CreatedOn >= strt && x.CreatedOn <= end && x.IsEnabled)
               .Select(x => new GetMappedCategory
               {
                   Id = x.Id,
                   Name = x.Name,
                   LastUpdatedDate = x.UpdatedOn ?? DateTime.UtcNow,
                   MasterCategory = string.Join(" | ", x.CategoryGroups.Where(y => y.IsEnabled).Select(y => y.MasterCategory.Name).ToList()),
                   Mapped = x.MapCategoryCategoryTypes.Where(y => y.IsEnabled).Select(y => new GetMapped
                   {
                       Id = y.Id,
                       MasterId = y.CategoryTypeId,
                       Name = y.CategoryType.Name
                   }).ToList()
               }).AsQueryable();

                if (!string.IsNullOrEmpty(page.Search))
                {
                    var date = new DateTime();
                    var sdate = DateTime.TryParse(page.Search, out date);
                    int totalCases = -1;
                    var isNumber = Int32.TryParse(page.Search, out totalCases);
                    if (sdate)
                        query = query.Where(
                            x => x.LastUpdatedDate.Date == date.Date
                        );
                    else if (isNumber)
                    {
                        //query = query.Where(
                        //    x => x.TotalCases == totalCases
                        //);
                    }
                    else
                    {
                        query = query.Where(
                        x => x.Name.ToLower().Contains(page.Search.ToLower())
                    );
                    }
                }
                var orderedQuery = query.OrderByDescending(x => x.Name);
                switch (page.SortIndex)
                {
                    case 0:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name);
                        break;
                    case 1:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.LastUpdatedDate) : query.OrderBy(x => x.LastUpdatedDate);
                        break;
                    case 2:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.MasterCategory) : query.OrderBy(x => x.MasterCategory);
                        break;
                }


                data.Page = page.Page;
                data.PageSize = page.PageSize;
                data.TotalRecords = orderedQuery.Count();
                data.Draw = page.Draw;
                data.Entities = orderedQuery.Skip(page.Page).Take(page.PageSize).ToList();
            }

            return data;
        }

        public async Task<bool> SaveCategoryToTypes(GetMappedCategory request, string userId)
        {
            try
            {
                bool response = false;

                using (var db = new EasypeasyDbContext())
                {
                    using (var trans = db.Database.BeginTransaction())
                    {
                        try
                        {
                            if (request.Mapped == null) request.Mapped = new List<GetMapped>();

                            var mapCategories = db.MapCategoryCategoryTypes.Where(x => x.IsEnabled && x.CategoryId.Equals(request.Id)).ToList();

                            // Delete children
                            foreach (var existingChild in mapCategories)
                            {
                                if (!request.Mapped.Any(c => c.Id == existingChild.Id))
                                {
                                    var mapCategory = db.MapCategoryCategoryTypes.Find(existingChild.Id);

                                    mapCategory.IsEnabled = false;
                                    mapCategory.DeletedBy = userId;
                                    mapCategory.DeletedOn = DateTime.UtcNow;

                                    db.Entry(mapCategory).State = EntityState.Modified;
                                }
                            }

                            // Update and Insert children
                            foreach (var childModel in request.Mapped)
                            {
                                var existingChild = mapCategories
                                    .Where(c => c.Id == childModel.Id)
                                    .SingleOrDefault();

                                if (existingChild != null)
                                {
                                    // Update child
                                    existingChild.CategoryId = request.Id;
                                    existingChild.CategoryTypeId = childModel.MasterId;
                                    existingChild.UpdatedBy = userId;
                                    existingChild.UpdatedOn = DateTime.UtcNow;

                                    db.Entry(existingChild).State = EntityState.Modified;
                                }
                                else
                                {
                                    // Insert child
                                    var newChild = new MapCategoryCategoryTypes
                                    {
                                        Id = SystemGlobal.GetId(),
                                        CategoryId = request.Id,
                                        CategoryTypeId = childModel.MasterId,
                                        IsEnabled = true,
                                        CreatedBy = userId,
                                        CreatedOn = DateTime.UtcNow
                                    };

                                    db.MapCategoryCategoryTypes.Add(newChild);
                                }
                            }

                            await db.SaveChangesAsync();

                            trans.Commit();
                            response = true;
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();
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
    }
}
