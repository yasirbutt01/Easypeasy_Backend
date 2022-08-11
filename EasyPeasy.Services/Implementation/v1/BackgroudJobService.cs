using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyPeasy.Common;
using EasyPeasy.Data.Context;
using EasyPeasy.Data.DTOs;
using EasyPeasy.DataViewModels.Enum;
using EasyPeasy.Services.Interface.v1;
using Microsoft.EntityFrameworkCore;

namespace EasyPeasy.Services.Implementation.v1
{
    public class BackgroudJobService : IBackgroundJobService
    {
        private readonly INotificationService _notificationService;

        public BackgroudJobService(INotificationService _notificationService)
        {
            this._notificationService = _notificationService;
        }
        public void RouteSchedulingJob(string rootpath)
        {
            if (!SystemGlobal.isMethodInUse)
            {
                SystemGlobal.isMethodInUse = true;
            }
            else return;

            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    var currentDate = DateTime.UtcNow;

                    var getPendingNotifications = db.UserCardDetails.Where(x =>
                        x.UserCardMaster.IsEnabled && x.UserCardMaster.IsScheduled &&
                        x.UserCardMaster.IsCardSent == false && x.IsNotify == false).ToList();

                    foreach (var pendingNotification in getPendingNotifications)
                    {
                        var getUserSubscription = db.UserSubscriptions.Include(x => x.Package).FirstOrDefault(x => x.IsEnabled && x.UserId == pendingNotification.SentFromId);

                        bool isPackageActive = getUserSubscription.PsNextBillingDate != null &&
                                               getUserSubscription.PsNextBillingDate > DateTime.UtcNow.Date;
                        var isOneHour = false;

                        var profile = db.UserProfiles.FirstOrDefault(x => x.IsEnabled && x.UserId == pendingNotification.SentToId);
                        var name = profile != null ? (profile.FirstName + " " + profile.LastName) : pendingNotification.SentToName;

                        switch ((int)(pendingNotification.UserCardMaster.ScheduledDate - currentDate).TotalHours)
                        {
                            case 24:
                                {
                                    isOneHour = false;
                                    var res = _notificationService.ScheduledCardNotification(pendingNotification.SentFromId, name, isPackageActive, isOneHour, pendingNotification.Id, pendingNotification.UserCardMaster.CardCategory.Category.Name, db).Result;
                                    pendingNotification.IsNotify = true;
                                    break;
                                }
                            case 1:
                                {
                                    isOneHour = true;
                                    var res = _notificationService.ScheduledCardNotification(pendingNotification.SentFromId, name, isPackageActive, isOneHour, pendingNotification.Id, pendingNotification.UserCardMaster.CardCategory.Category.Name, db).Result;
                                    pendingNotification.IsNotify = true;
                                    break;
                                }
                            default:
                                continue;
                                break;
                        }
                    }
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                SystemGlobal.isMethodInUse = false;
                MakeLog Err = new MakeLog();
                Err.ErrorLog(rootpath, "/BackgroundJob/RouteSchedulingJob.txt", "Error: " + ex.Message ?? "" + "Inner Exception => " + ex?.InnerException?.Message ?? "");
                throw ex;
            }
            SystemGlobal.isMethodInUse = false;
        }
    }
}
