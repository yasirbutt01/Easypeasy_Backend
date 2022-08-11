using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyPeasy.Data.Context;
using EasyPeasy.DataViewModels.Response.admin;
using EasyPeasy.Services.Interface.admin;
using Microsoft.EntityFrameworkCore;

namespace EasyPeasy.Services.Implementation.admin
{
    public class HomeService : IHomeService
    {
        public async Task<HomeCountsResponse> GetHomeCounts()
        {
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    var today = DateTime.UtcNow.Date;
                    var response = new HomeCountsResponse();
                    response.TodayCards = await db.UserCardDetails.CountAsync(x => x.IsEnabled && x.CreatedOn >= today);
                    response.ThisMonthCards = await db.UserCardDetails.CountAsync(x => x.IsEnabled && x.CreatedOn.Year == today.Year && x.CreatedOn.Month == today.Month);
                    response.AveragePerDayCards =
                        db.UserCardDetails.Where(x => x.IsEnabled && x.CreatedOn.Year == today.Year && x.CreatedOn.Month == today.Month).Count() > 0 ?
                        Math.Round(await db.UserCardDetails.Where(x => x.IsEnabled && x.CreatedOn.Year == today.Year && x.CreatedOn.Month == today.Month)
                        .GroupBy(x => x.CreatedOn.Day)
                        .AverageAsync(x => x.Count()), 2) : 0;

                    response.TodaySubscriptions = await db.UserSubscriptions.CountAsync(x => x.IsEnabled && x.CreatedOn >= today);
                    response.ThisMonthSubscriptions = await db.UserSubscriptions.CountAsync(x => x.IsEnabled && x.CreatedOn.Year == today.Year && x.CreatedOn.Month == today.Month);
                    response.AveragePerDaySubscriptions =
                        db.UserSubscriptions.Where(x => x.IsEnabled && x.CreatedOn.Year == today.Year && x.CreatedOn.Month == today.Month).Count() > 0 ?
                        Math.Round(await db.UserSubscriptions.Where(x => x.IsEnabled && x.CreatedOn.Year == today.Year && x.CreatedOn.Month == today.Month)
                        .GroupBy(x => x.CreatedOn.Day)
                        .AverageAsync(x => x.Count()), 2) : 0;

                    response.TodayRevenue = await db.UserInvoices.Where(x => x.IsEnabled && x.CreatedOn >= today).SumAsync(x => x.Price);
                    response.ThisMonthRevenue = await db.UserInvoices.Where(x => x.IsEnabled && x.CreatedOn.Year == today.Year && x.CreatedOn.Month == today.Month).SumAsync(x => x.Price);
                    response.AveragePerDayRevenue =
                        db.UserInvoices.Where(x => x.IsEnabled && x.CreatedOn.Year == today.Year && x.CreatedOn.Month == today.Month).Count() > 0 ?
                        Math.Round(await db.UserInvoices.Where(x => x.IsEnabled && x.CreatedOn.Year == today.Year && x.CreatedOn.Month == today.Month)
                        .GroupBy(x => x.CreatedOn.Day)
                        .AverageAsync(x => x.Sum(x => x.Price)), 2) : 0;

                    response.PackageCounts = await db.UserSubscriptions.Where(x => x.IsEnabled).GroupBy(x => x.Package.Name).Select(x =>
                        new HomePackageCountsResponse
                        {
                            Name = x.Key,
                            Count = x.Count()
                        }).ToListAsync();

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
