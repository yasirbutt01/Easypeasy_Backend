using EasyPeasy.DataViewModels.Requests.v1;
using EasyPeasy.DataViewModels.Response.v1;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EasyPeasy.Services.Interface.v1
{
    public interface IContactService
    {
        Task<bool> Invite(List<ContactInviteRequest> requests, Guid userId);
        Task<List<ContactGetGroupDetailResponse>> GetGroupDetail(Guid groupId);
        Task<bool> UpdateGroup(ContactUpdateGroupRequest request, Guid userId);
        Task<List<ContactGetGroupResponse>> GetGroups(Guid userId);
        Task<bool> CreateGroup(ContactCreateGroupRequest request, Guid userId);
        Task<bool> CreateCalenderEvent(ContactCreateCalenderEventRequest request, Guid userId);
        Task<List<ContactGetEventsResponse>> GetAllEvents(DateTime startDate, DateTime endDate, Guid userId);
    }
}
