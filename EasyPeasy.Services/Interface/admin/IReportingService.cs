using EasyPeasy.DataViewModels.Common;
using EasyPeasy.DataViewModels.Response.AdminResponse;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasyPeasy.Services.Interface.admin
{
   public interface IReportingService
    {

        GenralReportListRequest<AdminSubscribeResponse> GetSubscribeList(GenralReportListRequest<AdminSubscribeResponse> page, DateTime? str, DateTime? endd);

        public List<AdminSubscribeResponse> ExportToCsv();
    }
}
