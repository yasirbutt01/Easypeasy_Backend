using EasyPeasy.Data.Context;
using EasyPeasy.DataViewModels.Response.v1;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EasyPeasy.Services.Interface.v1
{
    public interface INotificationService
    {
        bool Read(string id);
        bool Delete(Guid UserId, string id);
        List<NotificationListResponse> GetNotificationList(Guid userId, int skip, int take);
        Task<bool> CalenderEventReminder(Guid sentFrom, Guid sentTo, string categoryName, Guid greetingId, DateTime date, EasypeasyDbContext db);
        Task<bool> PackageSubscribed(Guid sentFrom, Guid sentTo, string packageName, EasypeasyDbContext db);
        Task<bool> SentYouReminder(Guid sentFrom, Guid sentTo, Guid greetingId, string categoryName, EasypeasyDbContext db);
        Task<bool> YourScheduledCardSent(Guid sentTo, Guid greetingId, string categoryName, EasypeasyDbContext db);
        Task<bool> YourScheduledCardFailed(Guid sentTo, Guid greetingId, string categoryName, EasypeasyDbContext db);

        Task<bool> ScheduledCardNotification(Guid sentFrom, string sentToName, bool isPackageActive, bool isOneHour, Guid greetingId, string categoryName, EasypeasyDbContext db);
        Task<bool> SentYouCard(Guid sentFrom, Guid sentTo, Guid greetingId, string categoryName, EasypeasyDbContext db);
    }
}
