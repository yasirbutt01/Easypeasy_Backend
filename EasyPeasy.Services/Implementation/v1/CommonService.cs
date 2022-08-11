using EasyPeasy.Common;
using EasyPeasy.Data.Context;
using EasyPeasy.Data.DTOs;
using EasyPeasy.DataViewModels.Common;
using EasyPeasy.DataViewModels.Enum;
using EasyPeasy.DataViewModels.Requests;
using EasyPeasy.DataViewModels.Response.v1;
using EasyPeasy.Services.Interface.v1;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EasyPeasy.Services.Implementation.v1
{
    public class CommonService : ICommonService
    {
        //private readonly IConfiguration configuration;
        //public CommonService(IConfiguration configuration)
        //{
        //    this.configuration = configuration;
        //}

        public async Task<bool> IsEmailExists(string email)
        {
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    return await db.UserProfiles.AnyAsync(x => x.Email.Equals(email) && x.IsEnabled);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<List<CommonGetPackagesResponse>> GetPackages(Guid userId)
        {
            try
            {
                var response = new List<CommonGetPackagesResponse>();
                using (var db = new EasypeasyDbContext())
                {
                    var getFreePackageOfUser = await db.UserSubscriptions.OrderByDescending(x => x.CreatedOn).FirstOrDefaultAsync(x => x.UserId == userId && x.PackageId == EPackage.Free1.ToId());
                    response = await db.Packages.Include(x => x.PackagePoints).Where(x => x.IsEnabled).OrderBy(y => y.OrderBy).Select(x => new CommonGetPackagesResponse
                    {
                        Id = x.Id,
                        Description = x.Description,
                        Limit = (getFreePackageOfUser != null && x.Id == getFreePackageOfUser.PackageId) ? getFreePackageOfUser.Limit : x.Limit,
                        Name = x.Name,
                        Price = x.Price,
                        ColorCode = x.ColorCode,
                        Description2 = x.Description2,
                        DurationInDays = x.DurationInDays,
                        IsRecommended = x.IsRecommended,
                        packagePoints = x.PackagePoints.Where(y => y.IsEnabled).OrderBy(y => y.OrderBy).Select(y => new CommonGetPackagePointsResponse
                        {
                            Description = y.Description,
                            Status = y.Status
                        }).ToList()
                    }).ToListAsync();
                    return response;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<List<CommonFaqsResponse>> GetFaqs()
        {
            try
            {
                var response = new List<CommonFaqsResponse>();
                using (var db = new EasypeasyDbContext())
                {
                    response = await db.Faqs.Where(x => x.IsEnabled).Select(x => new CommonFaqsResponse
                    {
                        Title = x.Title,
                        Description = x.Description,
                        Link = x.Link
                    }).ToListAsync();
                    return response;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<List<General<int>>> GetFonts()
        {
            try
            {
                var response = new List<General<int>>();
                using (var db = new EasypeasyDbContext())
                {
                    response = await db.Fonts.Where(x => x.IsEnabled).Select(x => new General<int>
                    {
                        Id = x.Id,
                        Name = x.Name
                    }).ToListAsync();
                    return response;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<bool> AddUserFeedback(Guid userId, FeedBackRequest request)
        {
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    using (var trans = await db.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            var getFeedBackId = SystemGlobal.GetId();
                            var addfeedback = new UserFeedback()
                            {
                                Id = getFeedBackId,
                                ApiVersion = request.ApiVersion,
                                AppVersion = request.AppVersion,
                                CreatedBy = userId.ToString(),
                                CreatedOn = DateTime.UtcNow,
                                CreatedOnDate = DateTime.UtcNow.Date,
                                IsEnabled = true,
                                Message = request.Text,
                                OsType = request.OsType,
                                Rating = request.Rating,
                                UserId = userId
                            };
                            await db.AddAsync(addfeedback);
                            await db.SaveChangesAsync();
                            await trans.CommitAsync();
                            return true;
                        }
                        catch (Exception)
                        {
                            await trans.RollbackAsync();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

         public async Task<StaticContentDataResponse> GetStaticContent(int type)
        {
            // 1 about
            // 2 privacy Polcy
            try
            {
                var response = new StaticContentDataResponse();
                using (var db = new EasypeasyDbContext())
                {
                    response = await db.StaticContentData.Where(x => x.IsEnabled == true && x.Type == type).Select(x => new StaticContentDataResponse
                    {
                        Content = x.ContentText,
                        Version = x.Version.GetValueOrDefault()
                    }).FirstOrDefaultAsync();
                    return response;
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }
    }
}
