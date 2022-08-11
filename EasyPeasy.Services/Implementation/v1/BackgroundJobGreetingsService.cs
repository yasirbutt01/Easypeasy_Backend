using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyPeasy.Common;
using EasyPeasy.Data.Context;
using EasyPeasy.DataViewModels.Enum;
using EasyPeasy.Services.Interface.v1;
using Microsoft.EntityFrameworkCore;

namespace EasyPeasy.Services.Implementation.v1
{
    public class BackgroundJobGreetingsService: IBackgroundJobGreetingsService
    {
        private readonly INotificationService _notificationService;
        private readonly IEGifterService _eGifterService;
        private readonly ISmsService smsService;
        private readonly IEmailTemplateService _emailTemplateService;

        public BackgroundJobGreetingsService(INotificationService _notificationService, IEGifterService _eGifterService, ISmsService smsService, IEmailTemplateService _emailTemplateService)
        {
            this._notificationService = _notificationService;
            this._eGifterService = _eGifterService;
            this.smsService = smsService;
            this._emailTemplateService = _emailTemplateService;
        }
        public void RouteSchedulingGreetingJob(string rootpath)
        {
            if (!SystemGlobal.isMethodGreetingsInUse)
            {
                SystemGlobal.isMethodGreetingsInUse = true;
            }
            else return;

            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    var currentDate = DateTime.UtcNow;
                    var getUnscheduledCards = db.UserCardMaster
                        .Where(x => x.IsEnabled && x.IsScheduled && x.IsCardSent == false && x.IsCardSentFailed == false && x.ScheduledDate <= currentDate).Take(2).ToList();

                    foreach (var cardMaster in getUnscheduledCards)
                    {
                        if (cardMaster.ScheduledDate > currentDate) continue;
                        var getUserSubscription = db.UserSubscriptions.Include(x => x.Package)
                            .FirstOrDefault(x => x.IsEnabled && x.UserId == cardMaster.SentFromId);

                        if (getUserSubscription.PackageId == EPackage.Freelancer.ToId() || getUserSubscription.PsNextBillingDate != null &&
                            getUserSubscription.PsNextBillingDate > DateTime.UtcNow.Date)
                        {
                            cardMaster.IsCardSent = true;
                            cardMaster.UpdatedBy = "Scheduler Started";
                            cardMaster.UpdatedOn = DateTime.UtcNow;
                            db.SaveChanges();
                            var cardDetails = cardMaster.UserCardDetails.ToList();

                            var getFirstSentToUserDetail = cardDetails.FirstOrDefault();
                            if (getFirstSentToUserDetail.IseGiftAttached == true)
                            {
                                var getFirstSentToUserDetailGift = cardDetails.FirstOrDefault().UserCardDetaileGifts.FirstOrDefault();
                                var getFirstSentToUserDetailGiftLineItem = cardDetails.FirstOrDefault().UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault();

                                var invoice = getFirstSentToUserDetail.Invoice;

                                var toUser = db.UserProfiles.FirstOrDefault(x =>
                                    x.UserId == getFirstSentToUserDetail.SentToId);
                                var toUserName = toUser != null ? (toUser.FirstName + " " + toUser.LastName) : getFirstSentToUserDetail.SentToName;
                                var fromUser = db.UserProfiles.FirstOrDefault(x =>
                                    x.UserId == getFirstSentToUserDetail.SentFromId);

                                var eGifterOrderNumber = Guid.NewGuid();
                                var eGifterResponse = _eGifterService.CreateOrder(eGifterOrderNumber.ToString(),
                                    getFirstSentToUserDetailGiftLineItem.ProductId, (double)getFirstSentToUserDetailGiftLineItem.ValueAmount, getFirstSentToUserDetailGiftLineItem.Id.ToString(), toUserName, fromUser.FirstName + " " + fromUser.LastName).Result;
                                invoice.CostAmount = eGifterResponse.LineItems.Sum(x => x.Cost);
                                invoice.EgiftOrderId = eGifterResponse.Id;
                                invoice.FeeAmount = getFirstSentToUserDetail.FeeAmount;
                                invoice.ValueAmount = eGifterResponse.LineItems.Sum(x => x.Value);
                                getFirstSentToUserDetail.InvoiceId = invoice.Id;
                                getFirstSentToUserDetail.CostAmount = eGifterResponse.LineItems.Sum(x => x.Cost);

                                getFirstSentToUserDetailGift.OrderId = eGifterResponse.Id;
                                getFirstSentToUserDetailGift.Status = eGifterResponse.Status;
                                getFirstSentToUserDetailGift.Type = eGifterResponse.Type;

                                var eGifterResponseLineItems = eGifterResponse.LineItems.FirstOrDefault();

                                getFirstSentToUserDetailGiftLineItem.LineItemId =
                                    eGifterResponseLineItems?.ClaimData?.FirstOrDefault()?.Id;
                                getFirstSentToUserDetailGiftLineItem.Status =
                                    eGifterResponseLineItems?.Status;
                                getFirstSentToUserDetailGiftLineItem.BarCodePath =
                                    eGifterResponseLineItems?.ClaimData?.FirstOrDefault()?.BarcodePath;
                                getFirstSentToUserDetailGiftLineItem.BarCodeType =
                                    eGifterResponseLineItems?.ClaimData?.FirstOrDefault()?.BarcodeType;
                                getFirstSentToUserDetailGiftLineItem.ClaimLink =
                                    eGifterResponseLineItems?.ClaimData?.FirstOrDefault()?.ClaimLink;
                                getFirstSentToUserDetailGiftLineItem.ClaimLinkAnswer =
                                    eGifterResponseLineItems?.ClaimData?.FirstOrDefault()?.ClaimLinkChallengeAnswer;

                                getFirstSentToUserDetailGiftLineItem.CostAmount =
                                    eGifterResponseLineItems?.Cost;
                                getFirstSentToUserDetailGiftLineItem.ValueAmount =
                                    eGifterResponseLineItems?.Value;

                                getFirstSentToUserDetailGiftLineItem.LineItemId = eGifterResponseLineItems?.ClaimData?.FirstOrDefault()?.Id;
                                getFirstSentToUserDetailGiftLineItem.Status = eGifterResponseLineItems?.Status;

                                db.SaveChanges();

                                try
                                {
                                    if (getFirstSentToUserDetail.SentToId != null)
                                    {
                                        var getFromUser = db.Users.Include(y => y.UserProfiles).FirstOrDefault(y => y.Id == getFirstSentToUserDetail.SentFromId);
                                        var fromName = getFromUser.UserProfiles.FirstOrDefault()?.FirstName + "" +
                                                       getFromUser.UserProfiles.FirstOrDefault()?.LastName;
                                        var getEgifterProduct = db.EgifterProducts.FirstOrDefault(x =>
                                            x.IsEnabled && x.ProductId == getFirstSentToUserDetailGiftLineItem.ProductId);

                                        _ = _emailTemplateService.CongratulationsCardReceived(
                                                toUser.Email, 
                                                (decimal)getFirstSentToUserDetailGiftLineItem.ValueAmount, 
                                                fromName, 
                                                getEgifterProduct.Name,
                                                getEgifterProduct?.EgifterProductFaceplates?.FirstOrDefault()?.Path,
                                                getFirstSentToUserDetail.Id.ToString(), 
                                                getEgifterProduct.SecondaryColor
                                            );

                                    }
                                }
                                catch (Exception e)
                                {
                                    //ignore
                                }
                            }

                            cardDetails.ForEach(x =>
                            {
                                var profile =
                                    db.UserProfiles.FirstOrDefault(y => y.IsEnabled && y.UserId == x.SentFromId);
                                var name = profile.FirstName + " " + profile.LastName;
                                var egiftString = x.IseGiftAttached == true ? "giftcard" : "greeting";




                                if (x.SentToId != null)
                                {
                                    var sentToProfile =
                                        db.UserProfiles.FirstOrDefault(y => y.IsEnabled && y.UserId == x.SentToId);
                                    var sentToName = sentToProfile.FirstName + " " + sentToProfile.LastName;
                                    var sentToPhoneNumber = sentToProfile.User.PhoneNumber;
                                    var res = _notificationService.SentYouCard(x.SentFromId, (Guid)x.SentToId, x.Id, x.UserCardMaster.CardCategory.Category.Name, db).Result;
                                    _ = Task.Run(() =>
                                    {
                                        _ = smsService.SendSmsToUser(
                                            "You received an EasyPeasy " + egiftString + " from " + name + ". https://easypeasycards.com/go?id=" + x.Id.ToString(), sentToPhoneNumber, false);
                                    });
                                } else
                                if (!string.IsNullOrWhiteSpace(x.SentToPhoneNumber))
                                {

                                    //var userCategoryName = x.UserCardMaster.CardCategory.Category.Name;
                                    var sentToPhoneNumber = x.SentToPhoneNumber;
                                    _ = Task.Run(() =>
                                    {
                                        _ = smsService.SendSmsToUser(
                                            "You received an EasyPeasy " + egiftString + " from " + name + ". https://easypeasycards.com/go?id=" + x.Id.ToString(), sentToPhoneNumber, false);
                                    });
                                }


                                var res2 = _notificationService.YourScheduledCardSent(x.SentFromId, x.Id,
                                    x.UserCardMaster.CardCategory.Category.Name, db).Result;
                                x.UpdatedBy = "Scheduler";
                                x.UpdatedOn = DateTime.UtcNow;

                            });
                            cardMaster.UpdatedBy = "Scheduler Completed";
                            db.SaveChanges();
                        }
                        else
                        {
                            cardMaster.IsCardSent = false;
                            cardMaster.IsCardSentFailed = true;
                            cardMaster.FailedReason = "Package was Inactive";
                            cardMaster.UpdatedBy = "Scheduler Completed";
                            cardMaster.UpdatedOn = DateTime.UtcNow;
                            var res = _notificationService.YourScheduledCardFailed(cardMaster.SentFromId,
                                cardMaster.Id, cardMaster.CardCategory.Category.Name, db).Result;
                        }
                    }

                   
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                SystemGlobal.isMethodGreetingsInUse = false;
                MakeLog Err = new MakeLog();
                Err.ErrorLog(rootpath, "/BackgroundJob/RouteSchedulingJob.txt", "Error: " + ex.Message ?? "" + "Inner Exception => " + ex?.InnerException?.Message ?? "");
                throw ex;
            }
            SystemGlobal.isMethodGreetingsInUse = false;
        }
    }
}
