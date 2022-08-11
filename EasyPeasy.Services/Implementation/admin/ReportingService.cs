using EasyPeasy.Data.Context;
using EasyPeasy.DataViewModels.Common;
using EasyPeasy.DataViewModels.Response.AdminResponse;
using EasyPeasy.Services.Interface.admin;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EasyPeasy.Services.Implementation.admin
{
    public class ReportingService : IReportingService
    {
        public List<AdminSubscribeResponse> ExportToCsv()
        {
            using (var db = new EasypeasyDbContext())
            {
                var query = db.Subscribes.Where(b => b.IsActive == true)
               .Select(x => new AdminSubscribeResponse
               {
                   Id = x.Id,
                   EmailAddress = x.EmailAddress,
                   CreatedDtg=x.CreatedDtg.Value.Date
               }).OrderBy(c => c.CreatedDtg).ToList();

                return query;

            }


           
        }

        public GenralReportListRequest<AdminSubscribeResponse> GetSubscribeList(GenralReportListRequest<AdminSubscribeResponse> page, DateTime? str, DateTime? endd)
        {
            GenralReportListRequest<AdminSubscribeResponse> data = new GenralReportListRequest<AdminSubscribeResponse>();

            using (var db = new EasypeasyDbContext())
            {
                var query = db.Subscribes.Where(b => b.IsActive == true)
               .Select(x => new AdminSubscribeResponse
               {
                   Id = x.Id,
                   EmailAddress=x.EmailAddress,
                   CreatedDtg=x.CreatedDtg
               }).OrderBy(c => c.CreatedDtg).AsQueryable();

                if (!string.IsNullOrEmpty(page.Search))
                {
                    var date = new DateTime();
                    var sdate = DateTime.TryParse(page.Search, out date);
                    int totalCases = -1;
                    var isNumber = Int32.TryParse(page.Search, out totalCases);
                    if (sdate)
                        query = query.Where(
                            x => x.CreatedDtg.Value.Date == date.Date
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
                        x => x.EmailAddress.ToLower().Contains(page.Search.ToLower())
                    );
                    }
                }
                var orderedQuery = query.OrderByDescending(x => x.CreatedDtg);
                switch (page.SortIndex)
                {
                    case 0:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.CreatedDtg.Value.Date) : query.OrderBy(x => x.CreatedDtg.Value.Date);
                        break;
                    case 1:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.EmailAddress) : query.OrderBy(x => x.EmailAddress);
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
    }
}
