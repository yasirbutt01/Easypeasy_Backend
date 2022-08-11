using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EasyPeasy.DataViewModels.Requests.v1;

namespace EasyPeasy.Services.Interface.v2
{
    public interface IContactService
    {
        Task<List<DataViewModels.Response.v2.ContactGetEventsResponse>> GetAllEvents(DateTime startDate,
            DateTime endDate, Guid userId);
        Task<List<ContactCreateGroupDetailRequest>> GetGroupDetail(Guid groupId);
    }
}
