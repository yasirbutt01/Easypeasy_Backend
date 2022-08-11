using EasyPeasy.Common;
using EasyPeasy.Data.Context;
using EasyPeasy.Data.DTOs;
using EasyPeasy.DataViewModels.Enum;
using EasyPeasy.DataViewModels.Response.v1;
using EasyPeasy.Services.Interface.v1;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyPeasy.Services.Implementation.v1
{
    public class NotificationService : INotificationService
    {
        private readonly IConfiguration configuration;

        private readonly string ApplicationId;
        private readonly string SenderId;

        public NotificationService(IConfiguration configuration)
        {
            this.ApplicationId = configuration.GetValue<string>("Notification:ApplicationId");
            this.SenderId = configuration.GetValue<string>("Notification:SenderId");
        }

        public async Task<bool> CalenderEventReminder(Guid sentFrom, Guid sentTo, string categoryName, Guid greetingId, DateTime date, EasypeasyDbContext db)
        {

            try
            {

                var profile = await db.UserProfiles.FirstOrDefaultAsync(x => x.IsEnabled && x.UserId == sentFrom);
                var name = profile.FirstName + " " + profile.LastName;

                string message = "Reminder </b>" + name + ", " + categoryName + "</b> on " + date.ToString("dd MMM, yyyy"),
                    simpleMessage = "Reminder " + name + ", " + categoryName + " on " + date.ToString("dd MMM, yyyy");

                bool notify = await SaveNotification(new Notifications
                {
                    Message = message,
                    Type = ENotificationType.CalenderEventReminder.ToString(),
                    Data = greetingId.ToString(),
                    SentFromId = sentFrom,
                    SentToId = sentTo,
                }, db);

                if (notify)
                {
                    //Start Notifying
                    Task.Run(() => StartNotifying(sentTo, "", ENotificationType.CalenderEventReminder.ToString(), simpleMessage, name, null));
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public async Task<bool> PackageSubscribed(Guid sentFrom, Guid sentTo, string packageName, EasypeasyDbContext db)
        {

            try
            {
                var profile = await db.UserProfiles.FirstOrDefaultAsync(x => x.IsEnabled && x.UserId == sentFrom);
                var name = profile.FirstName + " " + profile.LastName;

                string message = "You have successfully subscribed to <b>" + packageName + "</b>",
                    simpleMessage = "You have successfully subscribed to " + packageName + "";

                bool notify = await SaveNotification(new Notifications
                {
                    Message = message,
                    Type = ENotificationType.PackageSubscribed.ToString(),
                    Data = "",
                    SentFromId = sentFrom,
                    SentToId = sentTo,
                }, db);

                if (notify)
                {
                    //Start Notifying
                    Task.Run(() => StartNotifying(sentTo, "", ENotificationType.PackageSubscribed.ToString(), simpleMessage, name, null));
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public async Task<bool> SentYouReminder(Guid sentFrom, Guid sentTo, Guid greetingId, string categoryName, EasypeasyDbContext db)
        {

            try
            {
                {
                    var profile = await db.UserProfiles.FirstOrDefaultAsync(x => x.IsEnabled && x.UserId == sentFrom);
                    var name = profile.FirstName + " " + profile.LastName;

                    string message = "sent you a <b>Reminder</b> for " + categoryName,
                        simpleMessage = name + " sent you a Reminder for " + categoryName + "";

                    bool notify = await SaveNotification(new Notifications
                    {
                        Message = message,
                        Type = ENotificationType.SentYouReminder.ToString(),
                        Data = greetingId.ToString(),
                        SentFromId = sentFrom,
                        SentToId = sentTo,
                    }, db);

                    if (notify)
                    {
                        //Start Notifying
                        Task.Run(() => StartNotifying(sentTo, greetingId.ToString(), ENotificationType.SentYouReminder.ToString(), simpleMessage, name, null));
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public async Task<bool> ScheduledCardNotification(Guid sentFrom, string sentToName, bool isPackageActive, bool isOneHour, Guid greetingId, string categoryName, EasypeasyDbContext db)
        {

            try
            {


                var message = "";
                var simpleMessage = "";
                if (isPackageActive)
                {
                    if (isOneHour)
                    {
                        message = "Your scheduled <b>" + categoryName + "</b> Card to <b>" + sentToName +
                                  "</b> will be sent in 1 hour";
                        simpleMessage = "Your scheduled " + categoryName + " Card to " + sentToName +
                                        " will be sent in 1 hour";
                    }
                    else
                    {
                        message = "Your scheduled <b>" + categoryName + "</b> Card to <b>" + sentToName +
                                  "</b> will be sent in 24 hour";
                        simpleMessage = "Your scheduled " + categoryName + " Card to " + sentToName +
                                        " will be sent in 24 hour";
                    }

                    bool notify = await SaveNotification(new Notifications
                    {
                        Message = message,
                        Type = ENotificationType.ScheduledWillBeSent.ToString(),
                        Data = greetingId.ToString(),
                        SentToId = sentFrom,
                    }, db);

                    if (notify)
                    {
                        //Start Notifying
                        Task.Run(() => StartNotifying(sentFrom, greetingId.ToString(), ENotificationType.ScheduledWillBeSent.ToString(), simpleMessage, sentToName, null));

                    }
                    return true;
                }
                else
                {
                    if (isOneHour)
                    {
                        message = "Your card " + categoryName + " is scheduled to be sent in 1 hour, please activate your package";
                        simpleMessage = "Your card " + categoryName + " is scheduled to be sent in 1 hour, please activate your package";
                    }
                    else
                    {
                        message = "Your card " + categoryName + " is scheduled to be sent in 24 hour, please activate your package";
                        simpleMessage = "Your card " + categoryName + " is scheduled to be sent in 24 hour, please activate your package";
                    }

                    bool notify = await SaveNotification(new Notifications
                    {
                        Message = message,
                        Type = ENotificationType.ScheduledCardButInactivePackage.ToString(),
                        Data = greetingId.ToString(),
                        SentToId = sentFrom,
                    }, db);

                    if (notify)
                    {
                        //Start Notifying
                        Task.Run(() => StartNotifying(sentFrom, greetingId.ToString(), ENotificationType.ScheduledCardButInactivePackage.ToString(), simpleMessage, sentToName, null));

                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public async Task<bool> YourScheduledCardFailed(Guid sentTo, Guid greetingId, string categoryName, EasypeasyDbContext db)
        {

            try
            {

                var profile = await db.UserProfiles.FirstOrDefaultAsync(x => x.IsEnabled && x.UserId == sentTo);
                var name = profile.FirstName + " " + profile.LastName;

                string message = "Your scheduled card <b>" + categoryName + "</b> was failed to sent to <b>" + name +
                                 "</b> because package was inactive",
                    simpleMessage = "Your scheduled card " + categoryName + " was failed to sent to " + name +
                                    " because package was inactive";

                bool notify = await SaveNotification(new Notifications
                {
                    Message = message,
                    Type = ENotificationType.ScheduledCardSentFailed.ToString(),
                    Data = greetingId.ToString(),
                    SentToId = sentTo,
                }, db);

                if (notify)
                {
                    //Start Notifying
                    Task.Run(() => StartNotifying(sentTo, greetingId.ToString(), ENotificationType.ScheduledCardSentFailed.ToString(), simpleMessage, name, null));

                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public async Task<bool> YourScheduledCardSent(Guid sentTo, Guid greetingId, string categoryName, EasypeasyDbContext db)
        {

            try
            {
                var getGreeting = await db.UserCardDetails.FirstOrDefaultAsync(x => x.Id == greetingId);
                var cardSentToId = getGreeting.SentToId;
                var profile = await db.UserProfiles.FirstOrDefaultAsync(x => x.IsEnabled && x.UserId == cardSentToId);
                var name = profile.FirstName + " " + profile.LastName;



                string message = "Your scheduled <b>" + categoryName + "</b> card was sent successfully to <b>" + name + "</b>",
                    simpleMessage = "Your scheduled " + categoryName + " card was sent successfully to " + name + "";

                bool notify = await SaveNotification(new Notifications
                {
                    Message = message,
                    Type = ENotificationType.ScheduledCardSent.ToString(),
                    Data = greetingId.ToString(),
                    SentToId = sentTo,
                }, db);

                if (notify)
                {
                    //Start Notifying
                    Task.Run(() => StartNotifying(sentTo, greetingId.ToString(),
                        ENotificationType.ScheduledCardSent.ToString(), simpleMessage, name, null));

                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public async Task<bool> SentYouCard(Guid sentFrom, Guid sentTo, Guid greetingId, string categoryName, EasypeasyDbContext db)
        {

            try
            {

                var profile = await db.UserProfiles.FirstOrDefaultAsync(x => x.IsEnabled && x.UserId == sentFrom);
                var name = profile.FirstName + " " + profile.LastName;

                string message = "sent you a <b>" + categoryName + "</b> card",
                    simpleMessage = name + " sent you a " + categoryName + " card";

                bool notify = await SaveNotification(new Notifications
                {
                    Message = message,
                    Type = ENotificationType.SentYouCard.ToString(),
                    Data = greetingId.ToString(),
                    SentFromId = sentFrom,
                    SentToId = sentTo,
                }, db);

                if (notify)
                {
                    //Start Notifying
                    Task.Run(() => StartNotifying(sentTo, greetingId.ToString(), ENotificationType.SentYouCard.ToString(), simpleMessage, name, null));
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public async Task<bool> SaveNotification(Notifications model, EasypeasyDbContext db)
        {
            bool res = false;
            try
            {
                {
                    await db.Notifications.Where(x => x.IsEnabled && x.Type.Equals(model.Type) && x.Data.Equals(model.Data) && x.SentTo.Equals(model.SentTo) && x.SentFrom.Equals(model.SentFrom)).ForEachAsync(x => x.IsEnabled = false);
                    await db.SaveChangesAsync();

                    model.Id = SystemGlobal.GetId();
                    model.IsRead = false;
                    model.IsEnabled = true;
                    model.CreatedBy = model.SentFromId.ToString();
                    model.CreatedOn = DateTime.UtcNow;
                    model.CreatedOnDate = DateTime.UtcNow;

                    await db.Notifications.AddAsync(model);
                    await db.SaveChangesAsync();
                    res = true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return res;
        }

        public void StartNotifying(Guid userId, string typeId, string type, string message, string name, object obj)
        {
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    List<string> tokens = db.UserDeviceInformations.Where(x => x.UserId.Equals(userId) && x.IsEnabled).Select(x => x.DeviceToken).ToList();
                    //string sound = database.Users.FirstOrDefault(x => x.Id.Equals(userId))?.SoundName ?? "Enabled";

                    Parallel.ForEach(tokens, token =>
                    {
                        FCMNotification.Sentnotify(this.ApplicationId, this.SenderId, token, message, "", "default", obj ?? new
                        {
                            type = type,
                            typeId = typeId,
                            name = name
                        });
                    });
                }
            }
            catch (Exception ex)
            {
            }
        }

        public List<NotificationListResponse> GetNotificationList(Guid userId, int skip, int take)
        {
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    var list = new List<NotificationListResponse>();
                    list = db.Notifications
                        .Where(x => x.SentToId.Equals(userId) && x.IsEnabled)
                        .OrderByDescending(x => x.CreatedOn)
                        .Select(x => new NotificationListResponse
                        {
                            Id = x.Id,
                            Name = x.SentFrom.UserProfiles.FirstOrDefault().FirstName + " " + x.SentFrom.UserProfiles.FirstOrDefault().LastName,
                            ImageUrl = x.SentFrom.UserProfiles.FirstOrDefault().ImageThumbnailUrl,
                            Text = x.Message,
                            Type = x.Type,
                            IsRead = x.IsRead,
                            Data = x.Data,
                            Date = x.CreatedOn,
                        }).Skip(skip).Take(take).ToList();
                    return list;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool Read(string id)
        {
            bool res = false;

            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    var model = db.Notifications.Find(id);
                    if (model != null)
                    {
                        model.IsRead = true;
                        model.UpdatedOn = DateTime.UtcNow;
                        model.UpdatedBy = model.SentToId.ToString();
                        db.Entry(model).State = EntityState.Modified;
                        db.SaveChanges();
                        res = true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return res;
        }

        public bool Delete(Guid UserId, string id)
        {
            bool res = false;

            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    if (string.IsNullOrEmpty(id))
                    {
                        db.Notifications.Where(x => x.SentTo.Equals(UserId) && x.IsEnabled).ToList().ForEach(x => { x.IsEnabled = false; x.DeletedOn = DateTime.UtcNow; x.DeletedBy = x.SentToId.ToString(); });
                        db.SaveChanges();
                        res = true;
                    }
                    else
                    {
                        var model = db.Notifications.Find(id);
                        if (model != null)
                        {
                            model.IsEnabled = false;
                            model.DeletedOn = DateTime.UtcNow;
                            model.DeletedBy = model.SentTo.ToString();
                            db.Entry(model).State = EntityState.Modified;
                            db.SaveChanges();
                            res = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return res;
        }
    }
}
