using EasyPeasy.Common;
using EasyPeasy.Data.Context;
using EasyPeasy.Data.DTOs;
using EasyPeasy.DataViewModels.Enum;
using EasyPeasy.DataViewModels.Requests.v1;
using EasyPeasy.DataViewModels.Response.v1;
using EasyPeasy.Services.Interface.v1;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EasyPeasy.Services.Implementation.v1
{
    public class CardService : ICardService
    {
        private readonly INotificationService notificationService;
        private readonly IBrainTreeService brainTree;
        private readonly IEGifterService _eGifterService;
        private readonly ISmsService smsService;
        private readonly IEmailTemplateService _emailTemplateService;

        public CardService(INotificationService notificationService, IBrainTreeService brainTree, IEGifterService _eGifterService, ISmsService smsService, IEmailTemplateService _emailTemplateService)
        {
            this.notificationService = notificationService;
            this.brainTree = brainTree;
            this._eGifterService = _eGifterService;
            this.smsService = smsService;
            this._emailTemplateService = _emailTemplateService;
        }
        public async Task<bool> CardAction(Guid greetingId, ECardAction action, Guid userId)
        {
            using (var db = new EasypeasyDbContext())
            {
                try
                {
                    var getGreeting = await db.UserCardDetails.FirstOrDefaultAsync(x => x.Id == greetingId);
                    switch (action)
                    {
                        case ECardAction.Archive:
                            if (getGreeting.SentFromId == userId)
                            {
                                getGreeting.IsArchiveFromFrom = true;
                            }
                            if (getGreeting.SentToId == userId)
                            {
                                getGreeting.IsArchiveFromTo = true;
                            }
                            break;
                        case ECardAction.UnArchive:
                            if (getGreeting.SentFromId == userId)
                            {
                                getGreeting.IsArchiveFromFrom = false;
                            }
                            if (getGreeting.SentToId == userId)
                            {
                                getGreeting.IsArchiveFromTo = false;
                            }
                            break;
                        case ECardAction.Delete:
                            if (getGreeting.SentFromId == userId)
                            {
                                getGreeting.IsDeletedFromFrom = true;
                            }
                            if (getGreeting.SentToId == userId)
                            {
                                getGreeting.IsDeletedFromTo = true;
                            }
                            break;
                        case ECardAction.CardOpened:
                            if (getGreeting.SentToId == userId)
                            {
                                getGreeting.IsOpen = true;
                                getGreeting.IsOpenDate = DateTime.UtcNow;
                            }
                            break;
                        case ECardAction.SendReminder:
                            var profile =
                                db.UserProfiles.FirstOrDefault(x => x.IsEnabled && x.UserId == userId);
                            var name = profile.FirstName + " " + profile.LastName;



                            var egiftString = getGreeting.IseGiftAttached == true ? "giftcard" : "greeting";

                            if (getGreeting.SentToId != null)
                            {
                                var sentToProfile =
                                    db.UserProfiles.FirstOrDefault(y => y.IsEnabled && y.UserId == getGreeting.SentToId);
                                var sentToName = sentToProfile.FirstName + " " + sentToProfile.LastName;
                                var sentToPhoneNumber = sentToProfile.User.PhoneNumber;
                                var res = await notificationService.SentYouReminder(userId, (Guid)getGreeting.SentToId, getGreeting.Id, getGreeting.UserCardMaster.CardCategory.Category.Name, db);
                                _ = Task.Run(() =>
                                {
                                    _ = smsService.SendSmsToUser(
                                        "Reminder, You received an EasyPeasy " + egiftString + " from " + name + ". https://easypeasycards.com/go?id=" + getGreeting.Id.ToString(), sentToPhoneNumber, false);
                                });
                            }
                            else
                            if (!string.IsNullOrWhiteSpace(getGreeting.SentToPhoneNumber))
                            {

                                var sentToPhoneNumber = getGreeting.SentToPhoneNumber;
                                _ = Task.Run(() =>
                                {
                                    _ = smsService.SendSmsToUser(
                                        "Reminder, You received an EasyPeasy " + egiftString + " from " + name + ". https://easypeasycards.com/go?id=" + getGreeting.Id.ToString(), sentToPhoneNumber, false);
                                });
                            }

                            //if (getGreeting.SentToId != null)
                            //{
                            //    var res = await notificationService.SentYouReminder(userId, (Guid)getGreeting.SentToId, getGreeting.Id, getGreeting.UserCardMaster.CardCategory.Category.Name, db);
                            //}


                            break;
                        default:
                            break;
                    }
                    await db.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }

        public async Task<List<CardArchivedCategoryCountResponse>> ArchivedCategoryCount(Guid userId, int skip, int take)
        {
            using (var db = new EasypeasyDbContext())
            {
                var currentDate = DateTime.UtcNow;
                var response = new List<CardArchivedCategoryCountResponse>();

                response = await db.UserCardDetails.Where(x => x.IsEnabled && !x.IsDeletedFromTo && (x.SentFromId.Equals(userId) && x.IsArchiveFromFrom || x.SentToId.Equals(userId) && x.IsArchiveFromTo))
                    .GroupBy(x => new { x.UserCardMaster.CardCategory.Category.Name, x.UserCardMaster.CardCategory.CategoryId })
                    .Select(x => new CardArchivedCategoryCountResponse
                    {
                        CategoryId = x.Key.CategoryId,
                        Category = x.Key.Name,
                        ArchivedCount = x.Count()
                    }).Skip(skip).Take(take).ToListAsync();

                return response;
            }
        }

        //public Tuple<bool, 
        public async Task<Tuple<string, GetMyCardResponse>> CardDetailById(Guid cardId, Guid userId)
        {
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    var x = await db.UserCardDetails.FirstOrDefaultAsync(y => y.Id == cardId);

                    if (db.Users.FirstOrDefault(x => x.Id == userId).IsGuest)
                    {
                        return Tuple.Create("You are currently logged-in with a different user. To view greeting, please login to your other account. Thank you!", new GetMyCardResponse());
                    }

                    if (x == null)
                    {
                        return Tuple.Create("You are currently logged-in with a different user. To view greeting, please login to your other account. Thank you!", new GetMyCardResponse());
                    }

                    if (x.SentToId != userId && x.SentFromId != userId)
                    {
                        return Tuple.Create("You are currently logged-in with a different user. To view greeting, please login to your other account. Thank you!", new GetMyCardResponse());
                    }

                    if (x.SentToId == userId)
                    {
                        x.IsOpen = true;
                        await db.SaveChangesAsync();
                    }

                    var isCardSent = x.SentFromId == userId;
                    var response = new GetMyCardResponse()
                    {
                        Id = x.Id,
                        UserCropImageThumbnailUrl = isCardSent ? x.SentTo?.UserProfiles?.FirstOrDefault(y => x.IsEnabled)?
                            .CropImageThumbnailUrl : x.SentFrom?.UserProfiles?.FirstOrDefault(y => x.IsEnabled)?
                            .CropImageThumbnailUrl,
                        Date = x.UserCardMaster?.ScheduledDate,
                        Description = x.UserCardMaster?.Description,
                        IsAchived = x.IsArchiveFromFrom,
                        IsOpen = x.IsOpen,
                        IsSentInGroup = x.UserCardMaster?.IsSentInGroup ?? false,
                        Name = (x.UserCardMaster?.IsSentInGroup ?? false)
                            ? x.UserGroup?.Name
                            : (isCardSent ? x.SentTo?.UserProfiles?.FirstOrDefault(y => y.IsEnabled)?.FirstName + " " +
                                            x.SentTo?.UserProfiles?.FirstOrDefault(y => y.IsEnabled)?.LastName : x.SentFrom?.UserProfiles?.FirstOrDefault(y => y.IsEnabled)?.FirstName + " " +
                                x.SentFrom?.UserProfiles?.FirstOrDefault(y => y.IsEnabled)?.LastName),
                        PhoneNumber = isCardSent ? x.SentTo?.PhoneNumber : x.SentFrom?.PhoneNumber,
                        GroupCount = x.UserGroup?.UserGroupUsers?.Count() ?? 0,
                        CategoryName = x.UserCardMaster?.CardCategory?.Category?.Name,
                        CategoryGifFileUrl = x.UserCardMaster?.CardCategory?.Category?.GifFileUrl,
                        CategoryIconFileUrl = x.UserCardMaster?.CardCategory?.Category?.IconFileUrl,
                        CategoryJsonFileUrl = x.UserCardMaster?.CardCategory?.Category?.JsonFileUrl,
                        CategoryIconFileThumbnailUrl = x.UserCardMaster?.CardCategory?.Category?.IconFileThumbnailUrl,
                        GreetingTypeId = x.UserCardMaster?.CardCategory?.Category?.GreetingTypeId,
                        ThumbnailUrl = x.UserCardMaster?.Card.CardFileUrl,
                        VideoUrl = x.UserCardMaster?.VideoUrl,
                        VideoThumbnailUrl = x.UserCardMaster?.VideoThumbUrl,
                        FontId = x.UserCardMaster?.FontId,
                        MusicName = x.UserCardMaster?.CategoryMusicFile?.Name,
                        MusicUrl = x.UserCardMaster?.CategoryMusicFile?.Mp3FileUrl,
                        Images = x.UserCardMaster?.UserCardMasterImages?.Select(y => new GetCardImageResponse
                        {
                            ImageFileThumbnailUrl = y.ImageFileThumbnailUrl,
                            ImageFileUrl = y.ImageFileUrl
                        }).ToList(),
                        IseGiftAttached = x.IseGiftAttached ?? false,
                        EGiftDetail = (x.IseGiftAttached == true)
                            ? new GetCardSendeGiftResponse()
                            {
                                CardValue = x.UserCardDetaileGifts?.FirstOrDefault()?.UserCardDetaileGiftLineItems
                                    .FirstOrDefault()?.ValueAmount ?? 0,
                                BrandName = x.UserCardDetaileGifts?.FirstOrDefault()?.UserCardDetaileGiftLineItems
                                    .FirstOrDefault()?.BrandName,
                                ClaimLink = x.UserCardDetaileGifts?.FirstOrDefault()?.UserCardDetaileGiftLineItems
                                    .FirstOrDefault()?.ClaimLink,
                                CardImageUrl = x.UserCardDetaileGifts?.FirstOrDefault()?.UserCardDetaileGiftLineItems
                                    .FirstOrDefault()?.CardImageUrl,
                                ClaimLinkAnswer = x.UserCardDetaileGifts?.FirstOrDefault()?.UserCardDetaileGiftLineItems
                                    .FirstOrDefault()?.ClaimLinkAnswer,
                                ProductId = x.UserCardDetaileGifts?.FirstOrDefault()?.UserCardDetaileGiftLineItems
                                    .FirstOrDefault()?.ProductId,
                                Status = x.UserCardDetaileGifts?.FirstOrDefault()?.UserCardDetaileGiftLineItems
                                    .FirstOrDefault()?.Status,
                                PrimaryColor = x.UserCardDetaileGifts?.FirstOrDefault()?.UserCardDetaileGiftLineItems
                                    .FirstOrDefault()?.PrimaryColor,
                                SecondaryColor = x.UserCardDetaileGifts?.FirstOrDefault()?.UserCardDetaileGiftLineItems
                                    .FirstOrDefault()?.SecondaryColor,
                                PaymentDetails = new GetCardSendeGiftPaymentMethodResponse()
                                {
                                    CardMaskedCardNumber = x.PaymentMethod?.CardMaskedNumber,
                                    ExpiryDate = x.PaymentMethod?.ExpiryDate,
                                    PaymentMethodImageUrl = x.PaymentMethod?.PaymentMethodImageUrl,
                                    PaymentMethodType = x.PaymentMethod?.PaymentMethodTypeId,
                                    PaypalEmail = x.PaymentMethod?.Email,
                                    CardValue = x.ValueAmount ?? 0,
                                    TotalAmount = x.TotalAmount ?? 0,
                                    FeeAmount = x.FeeAmount ?? 0
                                }
                            } : new GetCardSendeGiftResponse()

                    };

                    return Tuple.Create("", response);
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public async Task<GetCardResponse> GetMyReceivedCards(Guid userId, int skip, int take, bool isArchive, Guid? categoryId, bool iseGiftAttached, bool isScheduled)
        {
            using (var db = new EasypeasyDbContext())
            {
                var currentDate = DateTime.UtcNow;
                var response = new GetCardResponse();

                response.ArchiveCount = await db.UserCardDetails.CountAsync(x => x.IsEnabled && !x.IsDeletedFromTo && (x.SentFromId.Equals(userId) && x.IsArchiveFromFrom || x.SentToId.Equals(userId) && x.IsArchiveFromTo));
                response.CardList = await db.UserCardDetails.Where(x => x.SentToId.Equals(userId) && !x.IsDeletedFromTo && x.UserCardMaster.IsCardSent == true && x.UserCardMaster.ScheduledDate <= currentDate && x.IsEnabled == true &&
                (categoryId == null || x.UserCardMaster.CardCategory.CategoryId == categoryId) &&
                (iseGiftAttached == true || x.IsArchiveFromTo == isArchive) && (iseGiftAttached == false || (x.IseGiftAttached ?? false) == iseGiftAttached) && !x.UserCardMaster.IsCardSent == isScheduled).OrderByDescending(x => x.UserCardMaster.ScheduledDate)
                    .Select(x => new GetMyCardResponse
                    {
                        Id = x.Id,
                        UserCropImageThumbnailUrl = x.SentFrom.UserProfiles.FirstOrDefault(y => y.IsEnabled).CropImageThumbnailUrl,
                        Date = x.UserCardMaster.ScheduledDate,
                        IgnoreThis = x.UserCardMaster.CreatedOn,
                        Description = x.UserCardMaster.Description,
                        IsAchived = x.IsArchiveFromFrom,
                        IsOpen = x.IsOpen,
                        IsSentInGroup = x.UserCardMaster.IsSentInGroup,
                        Name = x.SentFrom.UserProfiles.FirstOrDefault(y => y.IsEnabled).FirstName + " " + x.SentFrom.UserProfiles.FirstOrDefault(y => y.IsEnabled).LastName,
                        PhoneNumber = x.SentFrom.PhoneNumber,
                        GroupCount = x.UserGroup.UserGroupUsers.Count(),
                        CategoryName = x.UserCardMaster.CardCategory.Category.Name,
                        CategoryGifFileUrl = x.UserCardMaster.CardCategory.Category.GifFileUrl,
                        CategoryIconFileUrl = x.UserCardMaster.CardCategory.Category.IconFileUrl,
                        CategoryJsonFileUrl = x.UserCardMaster.CardCategory.Category.JsonFileUrl,
                        CategoryIconFileThumbnailUrl = x.UserCardMaster.CardCategory.Category.IconFileThumbnailUrl,
                        GreetingTypeId = x.UserCardMaster.CardCategory.Category.GreetingTypeId,
                        ThumbnailUrl = x.UserCardMaster.Card.CardFileUrl,
                        VideoUrl = x.UserCardMaster.VideoUrl,
                        VideoThumbnailUrl = x.UserCardMaster.VideoThumbUrl,
                        FontId = x.UserCardMaster.FontId,
                        MusicName = x.UserCardMaster.CategoryMusicFile.Name,
                        MusicUrl = x.UserCardMaster.CategoryMusicFile.Mp3FileUrl,
                        Images = x.UserCardMaster.UserCardMasterImages.Select(y => new GetCardImageResponse
                        {
                            ImageFileThumbnailUrl = y.ImageFileThumbnailUrl,
                            ImageFileUrl = y.ImageFileUrl
                        }).ToList(),
                        IseGiftAttached = x.IseGiftAttached ?? false,
                        EGiftDetail = (x.IseGiftAttached == true) ? new GetCardSendeGiftResponse()
                        {
                            CardValue = x.UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().ValueAmount ?? 0,
                            BrandName = x.UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().BrandName,
                            ClaimLink = x.UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().ClaimLink,
                            CardImageUrl = x.UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().CardImageUrl,
                            ClaimLinkAnswer = x.UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().ClaimLinkAnswer,
                            ProductId = x.UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().ProductId,
                            Status = x.UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().Status,
                            PrimaryColor = x.UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().PrimaryColor,
                            SecondaryColor = x.UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().SecondaryColor,
                            PaymentDetails = new GetCardSendeGiftPaymentMethodResponse()
                            {
                                CardMaskedCardNumber = x.PaymentMethod.CardMaskedNumber,
                                ExpiryDate = x.PaymentMethod.ExpiryDate,
                                PaymentMethodImageUrl = x.PaymentMethod.PaymentMethodImageUrl,
                                PaymentMethodType = x.PaymentMethod.PaymentMethodTypeId,
                                PaypalEmail = x.PaymentMethod.Email,
                                CardValue = (double)x.ValueAmount,
                                TotalAmount = (decimal)x.TotalAmount,
                                FeeAmount = (double)x.FeeAmount
                            }
                        } : new GetCardSendeGiftResponse(),
                    }).Skip(skip).Take(take).ToListAsync();

                return response;
            }
        }

        public async Task<GetCardResponse> GetMySentCards(Guid userId, int skip, int take, bool isArchive, Guid? categoryId, bool iseGiftAttached, bool isScheduled)
        {
            using (var db = new EasypeasyDbContext())
            {
                var currentDate = DateTime.UtcNow;
                var response = new GetCardResponse();

                response.ArchiveCount = await db.UserCardDetails.CountAsync(x => x.IsEnabled && !x.IsDeletedFromFrom && (x.SentFromId.Equals(userId) && x.IsArchiveFromFrom || x.SentToId.Equals(userId) && x.IsArchiveFromTo));
                response.CardList = await db.UserCardMaster
                    .Include(x => x.UserCardDetails).ThenInclude(x => x.UserCardDetaileGifts).ThenInclude(x => x.UserCardDetaileGiftLineItems)
                    .Include(x => x.UserCardDetails).ThenInclude(x => x.SentTo).ThenInclude(x => x.UserProfiles)
                    .Include(x => x.UserCardDetails).ThenInclude(x => x.UserGroup)

                    .Where(x => x.SentFromId.Equals(userId) && !x.UserCardDetails.Any(y => y.IsDeletedFromFrom) && x.IsEnabled == true &&
          (categoryId == null || x.CardCategory.CategoryId == categoryId) &&
          (iseGiftAttached || (isArchive == false && x.UserCardDetails.All(y => y.IsArchiveFromFrom == isArchive) || isArchive == true && x.UserCardDetails.Any(y => y.IsArchiveFromFrom == isArchive))) && (iseGiftAttached == false || (x.UserCardDetails.FirstOrDefault().IseGiftAttached ?? false) == iseGiftAttached) && !x.IsCardSent == isScheduled)
              .OrderByDescending(x => x.CreatedOn)
              .Select(x => new GetMyCardResponse
              {
                  Id = x.UserCardDetails.FirstOrDefault().Id,
                  UserCropImageThumbnailUrl = db.UserProfiles.FirstOrDefault(y => y.IsEnabled && y.UserId.Equals(x.UserCardDetails.FirstOrDefault().SentToId)).CropImageThumbnailUrl ?? "",
                  Date = x.ScheduledDate,
                  IgnoreThis = x.CreatedOn,
                  Description = x.Description,
                  IsAchived = x.UserCardDetails.FirstOrDefault().IsArchiveFromTo,
                  IsOpen = x.UserCardDetails.FirstOrDefault().IsOpen,
                  IsSentInGroup = x.IsSentInGroup,
                  Name = x.IsSentInGroup ? (x.UserCardDetails.FirstOrDefault().UserGroup.Name ?? "") :
                      (x.UserCardDetails.FirstOrDefault().SentToId != null ? ((db.UserProfiles.FirstOrDefault(y => y.IsEnabled && y.UserId.Equals(x.UserCardDetails.FirstOrDefault().SentToId)).FirstName ?? "") + " " + (db.UserProfiles.FirstOrDefault(y => y.IsEnabled && y.UserId.Equals(x.UserCardDetails.FirstOrDefault().SentToId)).LastName ?? "")) : x.UserCardDetails.FirstOrDefault().SentToName),
                  PhoneNumber = x.UserCardDetails.FirstOrDefault().SentTo.PhoneNumber ?? x.UserCardDetails.FirstOrDefault().SentToPhoneNumber,
                  GroupCount = x.UserCardDetails.FirstOrDefault().UserGroup.UserGroupUsers.Count,
                  CategoryName = x.CardCategory.Category.Name,
                  CategoryGifFileUrl = x.CardCategory.Category.GifFileUrl,
                  CategoryIconFileUrl = x.CardCategory.Category.IconFileUrl,
                  CategoryJsonFileUrl = x.CardCategory.Category.JsonFileUrl,
                  CategoryIconFileThumbnailUrl = x.CardCategory.Category.IconFileThumbnailUrl,
                  GreetingTypeId = x.CardCategory.Category.GreetingTypeId,
                  ThumbnailUrl = x.Card.CardFileUrl,
                  VideoUrl = x.VideoUrl,
                  VideoThumbnailUrl = x.VideoThumbUrl,
                  FontId = x.FontId,
                  MusicName = x.CategoryMusicFile.Name,
                  MusicUrl = x.CategoryMusicFile.Mp3FileUrl,
                  Images = x.UserCardMasterImages.Select(y => new GetCardImageResponse
                  {
                      ImageFileThumbnailUrl = y.ImageFileThumbnailUrl,
                      ImageFileUrl = y.ImageFileUrl
                  }).ToList(),
                  IseGiftAttached = x.UserCardDetails.FirstOrDefault().IseGiftAttached ?? false,
                  EGiftDetail = new GetCardSendeGiftResponse()
                  {
                      CardValue = x.UserCardDetails.FirstOrDefault().UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().ValueAmount ?? 0,
                      BrandName = x.UserCardDetails.FirstOrDefault().UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().BrandName,
                      ClaimLink = x.UserCardDetails.FirstOrDefault().UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().ClaimLink,
                      CardImageUrl = x.UserCardDetails.FirstOrDefault().UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().CardImageUrl,
                      ClaimLinkAnswer = x.UserCardDetails.FirstOrDefault().UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().ClaimLinkAnswer,
                      ProductId = x.UserCardDetails.FirstOrDefault().UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().ProductId,
                      Status = x.UserCardDetails.FirstOrDefault().UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().Status,
                      PrimaryColor = x.UserCardDetails.FirstOrDefault().UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().PrimaryColor,
                      SecondaryColor = x.UserCardDetails.FirstOrDefault().UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().SecondaryColor,
                      PaymentDetails = new GetCardSendeGiftPaymentMethodResponse()
                      {
                          CardMaskedCardNumber = x.UserCardDetails.FirstOrDefault().PaymentMethod.CardMaskedNumber,
                          ExpiryDate = x.UserCardDetails.FirstOrDefault().PaymentMethod.ExpiryDate,
                          PaymentMethodImageUrl = x.UserCardDetails.FirstOrDefault().PaymentMethod.PaymentMethodImageUrl,
                          PaymentMethodType = x.UserCardDetails.FirstOrDefault().PaymentMethod.PaymentMethodTypeId,
                          PaypalEmail = x.UserCardDetails.FirstOrDefault().PaymentMethod.Email,
                          CardValue = x.UserCardDetails.FirstOrDefault().ValueAmount ?? 0,
                          TotalAmount = x.UserCardDetails.FirstOrDefault().TotalAmount ?? 0,
                          FeeAmount = x.UserCardDetails.FirstOrDefault().FeeAmount ?? 0
                      }
                  }
              })
              .Skip(skip).Take(take).ToListAsync();

                return response;
            }
        }

        public async Task<List<UserCardDetails>> Double1Fix()
        {
            using (var db = new EasypeasyDbContext())
            {
                using (var trans = await db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        //db.ChangeTracker.LazyLoadingEnabled = false;
                        //                        var userCardDetails = await db.UserCardDetails.Include(x => x.UserCardMaster).Where(x => (x.UserCardMaster.IsCardSent ?? false) && x.SentToPhoneNumber.StartsWith("+11"))
                        //.ToListAsync();
                        var userCardDetails = await db.UserCardDetails.Include(x => x.UserCardMaster).Where(x => x.UpdatedBy == "loop")
                            .ToListAsync();

                        userCardDetails.ForEach(x =>
                        {
                            //var regex = new Regex(Regex.Escape("+11"));
                            //x.SentToPhoneNumber = regex.Replace(x.SentToPhoneNumber, "+1", 1);
                            //x.UpdatedBy = "loop";
                            x.UpdatedOn = DateTime.UtcNow;
                            if (x.SentToId == null)
                            {
                                x.SentToId = db.Users.FirstOrDefault(y =>
                                    y.IsEnabled && !y.IsGuest && y.PhoneNumber == x.SentToPhoneNumber)?.Id;
                            }

                            try
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
                                    var res = notificationService.SentYouCard(x.SentFromId, (Guid)x.SentToId, x.Id, x.UserCardMaster.CardCategory.Category.Name, db).Result;
                                    _ = Task.Run(() =>
                                    {
                                        _ = smsService.SendSmsToUser(
                                            "You received an EasyPeasy " + egiftString + " from " + name + ". https://easypeasycards.com/go?id=" + x.Id.ToString(), sentToPhoneNumber, false);
                                    });
                                }
                                else if (!string.IsNullOrWhiteSpace(x.SentToPhoneNumber))
                                {

                                    //var userCategoryName = x.UserCardMaster.CardCategory.Category.Name;
                                    var sentToPhoneNumber = x.SentToPhoneNumber;
                                    _ = Task.Run(() =>
                                    {
                                        _ = smsService.SendSmsToUser(
                                            "You received an EasyPeasy " + egiftString + " from " + name + ". https://easypeasycards.com/go?id=" + x.Id.ToString(), sentToPhoneNumber, false);
                                    });
                                }


                            }
                            catch (Exception e)
                            {
                                // ignored
                            }

                        });
                        await db.SaveChangesAsync();
                        await trans.CommitAsync();
                        return userCardDetails;
                    }
                    catch (Exception ex)
                    {
                        await trans.RollbackAsync();
                        throw;
                    }
                }
            }
        }


        public async Task<bool> Send(CardSendRequest request, Guid userId)
        {
            using (var db = new EasypeasyDbContext())
            {
                using (var trans = await db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var getLastSentCardDate = db.UserCardMaster
                            .OrderByDescending(x => x.CreatedOn).FirstOrDefault(x => x.IsEnabled && x.SentFromId == userId)?.CreatedOn;

                        if (getLastSentCardDate.HasValue)
                        {
                            var nowDate = DateTime.UtcNow;
                            if ((nowDate - getLastSentCardDate.Value).TotalSeconds < 15)
                            {
                                await trans.RollbackAsync();
                                return false;
                            }
                        }

                        foreach (var x in request.details)
                        {
                            if (x.SentToId == null && string.IsNullOrWhiteSpace(x.SentToPhoneNumber))
                            {
                                await trans.RollbackAsync();
                                return false;
                            }
                        }

                        var getUserSubscription = await db.UserSubscriptions.Include(x => x.Package).FirstOrDefaultAsync(x => x.IsEnabled && x.UserId == userId);
                        var getPackage = getUserSubscription.Package;

                        if (getPackage.Id == EPackage.Free1.ToId())
                        {
                            if (getUserSubscription.Limit < 1)
                            {
                                return false;
                            }
                            else
                            {
                                getUserSubscription.Limit -= 1;
                            }
                        }
                        else if (getPackage.Id == EPackage.Freelancer.ToId())
                        {
                            if (getUserSubscription.Limit < 1)
                            {
                                var getFreePackageSubscription = await db.UserSubscriptions.Include(x => x.Package).FirstOrDefaultAsync(x => x.UserId == userId && x.PackageId == EPackage.Free1.ToId());

                                if (getFreePackageSubscription.Limit < 1)
                                {
                                    return false;
                                }
                                else
                                {
                                    getFreePackageSubscription.Limit -= 1;
                                }
                            }
                            else
                            {
                                getUserSubscription.Limit -= 1;
                            }
                        }
                        else if (getPackage.Id == EPackage.Correspondent.ToId() 
                                 || getPackage.Id == EPackage.Screenwriter.ToId() 
                                 || getPackage.Id == EPackage.InkSlinger.ToId() 
                                 || getPackage.Id == EPackage.EasyPeasyPremium.ToId()
                                 || getPackage.Id == EPackage.Ultimate.ToId())
                        {
                            if (getUserSubscription.PsNextBillingDate < DateTime.UtcNow.Date)
                            {
                                var getFreePackageSubscription = await db.UserSubscriptions.Include(x => x.Package).FirstOrDefaultAsync(x => x.UserId == userId && x.PackageId == EPackage.Free1.ToId());

                                if (getFreePackageSubscription.Limit < 1)
                                {
                                    return false;
                                }
                                else
                                {
                                    getFreePackageSubscription.Limit -= 1;
                                }
                            }
                        }

                        // App User did not sync
                        request.details.ForEach(x =>
                        {
                            if (!string.IsNullOrEmpty(x.SentToPhoneNumber) && x.SentToPhoneNumber.StartsWith("+11"))
                            {
                                var regex = new Regex(Regex.Escape("+11"));
                                x.SentToPhoneNumber = regex.Replace(x.SentToPhoneNumber, "+1", 1);
                            }

                            if (x.SentToId == null)
                            {
                                x.SentToId = db.Users.FirstOrDefault(y =>
                                    y.IsEnabled && !y.IsGuest && y.PhoneNumber == x.SentToPhoneNumber)?.Id;
                            }
                        });

                        var cardMasterId = SystemGlobal.GetId();
                        var cardCategoryId = db.CardCategories.FirstOrDefaultAsync(x => x.IsEnabled && x.CardId == request.CardId && x.CategoryId == request.CategoryId).Result.Id;
                        var cardMaster = new UserCardMaster()
                        {
                            CardId = request.CardId,
                            FontId = request.FontId,
                            CategoryMusicFileId = request.CategoryMusicFileId,
                            CreatedBy = userId.ToString(),
                            CreatedOn = DateTime.UtcNow,
                            IsEnabled = true,
                            Description = request.Description,
                            CreatedOnDate = DateTime.UtcNow,
                            Id = cardMasterId,
                            IsScheduled = request.IsScheduled,
                            ScheduledDate = request.IsScheduled ? (DateTime)request.ScheduledDate : DateTime.UtcNow,
                            IsCardSentFailed = false,
                            IsCardSent = !request.IsScheduled,
                            // EndTime =
                            IsSentInGroup = request.IsSentInGroup,
                            SentFromId = userId,
                            VideoThumbUrl = request.VideoThumbUrl,
                            VideoUrl = request.VideoUrl,
                            IsReminder = request.IsReminder,
                            CardCategoryId = cardCategoryId
                        };



                        var cardDetails = new List<UserCardDetails>();
                        if (cardMaster.IsSentInGroup)
                        {
                            var getGroup = await db.UserGroups.Include(x => x.UserGroupUsers).FirstOrDefaultAsync(x => x.Id == request.UserGroupId);
                            cardDetails.AddRange(getGroup.UserGroupUsers.Select(x => new UserCardDetails
                            {
                                Id = Guid.NewGuid(),
                                CreatedBy = userId.ToString(),
                                CreatedOn = DateTime.UtcNow,
                                IsEnabled = true,
                                SentFromId = userId,
                                SentToId = x.UserId,
                                CreatedOnDate = DateTime.UtcNow,
                                UserCardMasterId = cardMasterId,
                                UserGroupId = request.UserGroupId,
                                IseGiftAttached = request.IseGiftAttached,
                                SentToPhoneNumber = x.CountryCode + x.PhoneNumber,
                                SentToName = x.ContactFirstName + x.ContactLastName,
                            }).ToList());
                        }
                        else
                        {
                            cardDetails.AddRange(request.details.Select(x => new UserCardDetails
                            {
                                Id = Guid.NewGuid(),
                                CreatedBy = userId.ToString(),
                                CreatedOn = DateTime.UtcNow,
                                IsEnabled = true,
                                SentFromId = userId,
                                SentToId = x.SentToId,
                                SentToPhoneNumber = x.SentToPhoneNumber,
                                SentToName = x.SentToName,
                                CreatedOnDate = DateTime.UtcNow,
                                UserCardMasterId = cardMasterId,
                                IseGiftAttached = request.IseGiftAttached
                            }).ToList());
                        }

                        var cardImages = new List<UserCardMasterImages>();
                        cardImages.AddRange(request.Images.Select(x => new UserCardMasterImages
                        {
                            Id = Guid.NewGuid(),
                            CreatedBy = userId.ToString(),
                            CreatedOn = DateTime.UtcNow,
                            IsEnabled = true,
                            CreatedOnDate = DateTime.UtcNow,
                            UserCardMasterId = cardMasterId,
                            ImageFileThumbnailUrl = x.ImageFileThumbnailUrl,
                            ImageFileUrl = x.ImageFileUrl
                        }).ToList());

                        cardMaster.UserCardMasterImages = cardImages;
                        cardMaster.UserCardDetails = cardDetails;
                        await db.AddAsync(cardMaster);


                        if (request.IseGiftAttached)
                        {


                            var getFirstSentToUserDetail = cardDetails.FirstOrDefault();
                            getFirstSentToUserDetail.FeeAmount = request.EGiftDetail.FeeAmount;
                            getFirstSentToUserDetail.ValueAmount = request.EGiftDetail.CardValue;
                            getFirstSentToUserDetail.TotalAmount = request.EGiftDetail.TotalAmount;
                            getFirstSentToUserDetail.PaymentMethodId = request.EGiftDetail.PaymentMethodId;

                            var eGifterOrderNumber = Guid.NewGuid();

                            var userDetailGift = new UserCardDetaileGifts()
                            {
                                Id = Guid.NewGuid(),
                                CreatedOn = DateTime.UtcNow,
                                IsEnabled = true,
                                CreatedBy = userId.ToString(),
                                CreatedOnDate = DateTime.UtcNow,
                                OrderId = "",
                                Status = "",
                                Type = "",
                                UserCardDetailId = getFirstSentToUserDetail.Id
                            };
                            await db.UserCardDetaileGifts.AddAsync(userDetailGift);

                            var invoice = new UserInvoices()
                            {
                                Id = Guid.NewGuid(),
                                InvoiceType = 2,
                                IsEnabled = true,
                                CreatedBy = userId.ToString(),
                                CreatedOn = DateTime.UtcNow,
                                CreatedOnDate = DateTime.UtcNow,
                                UserId = userId,
                                Price = request.EGiftDetail.TotalAmount
                            };
                            await db.UserInvoices.AddAsync(invoice);
                            getFirstSentToUserDetail.InvoiceId = invoice.Id;

                            var getPaymentMethod = await db.UserPaymentMethods.FirstOrDefaultAsync(x =>
                                x.IsEnabled && x.Id == request.EGiftDetail.PaymentMethodId);

                            if (getPaymentMethod.IsVerified != true)
                            {
                                await trans.RollbackAsync();
                                return false;
                            }

                            var toUser = await db.UserProfiles.Include(x => x.User).FirstOrDefaultAsync(x =>
                                x.UserId == getFirstSentToUserDetail.SentToId);
                            var toUserName = toUser != null ? (toUser.FirstName + " " + toUser.LastName) : getFirstSentToUserDetail.SentToName;
                            var fromUser = await db.UserProfiles.Include(x => x.User).FirstOrDefaultAsync(x =>
                                x.UserId == getFirstSentToUserDetail.SentFromId);

                            var descriptionString = fromUser?.FirstName + " " + fromUser?.LastName + " (" + fromUser?.User?.PhoneNumber + ") sent EGift card " +
                                                    request?.EGiftDetail?.BrandName + " to " + toUserName + " (" + (string.IsNullOrWhiteSpace(toUser?.User?.PhoneNumber) ? getFirstSentToUserDetail?.SentToPhoneNumber : toUser?.User?.PhoneNumber) + ")" +
                                                    (request.IsScheduled ? " on schedule." : "");

                            var braintreeTransaction = brainTree.CreateTransaction(userId.ToString(),
                                request.EGiftDetail.TotalAmount, invoice.Id.ToString(), getPaymentMethod.Token, descriptionString);
                            //invoice.CostAmount = eGifterResponse.LineItems.Sum(x => x.Cost);
                            //invoice.EgiftOrderId = eGifterResponse.Id;
                            invoice.FeeAmount = request.EGiftDetail.FeeAmount;
                            invoice.ValueAmount = request.EGiftDetail.CardValue;

                            if (braintreeTransaction.IsSuccess())
                            {
                                invoice.TransactionId = braintreeTransaction.Target.Id;
                            }
                            else
                            {
                                await trans.RollbackAsync();
                                return false;
                            }

                            if (!request.IsScheduled)
                            {

                                var eGifterResponse = await _eGifterService.CreateOrder(eGifterOrderNumber.ToString(),
                                    request.EGiftDetail.ProductId, request.EGiftDetail.CardValue, userDetailGift.Id.ToString(), toUserName, fromUser.FirstName + " " + fromUser.LastName);

                                getFirstSentToUserDetail.CostAmount = eGifterResponse.LineItems.Sum(x => x.Cost);

                                userDetailGift.OrderId = eGifterResponse.Id;
                                userDetailGift.Status = eGifterResponse.Status;
                                userDetailGift.Type = eGifterResponse.Type;

                                var lineItems = eGifterResponse.LineItems.Select(x => new UserCardDetaileGiftLineItems
                                {
                                    Id = Guid.NewGuid(),
                                    CreatedOn = DateTime.UtcNow,
                                    IsEnabled = true,
                                    CreatedBy = userId.ToString(),
                                    CreatedOnDate = DateTime.UtcNow,
                                    BarCodePath = x.ClaimData.FirstOrDefault()?.BarcodePath,
                                    BarCodeType = x.ClaimData.FirstOrDefault()?.BarcodeType,
                                    ClaimLink = x.ClaimData.FirstOrDefault()?.ClaimLink,
                                    ClaimLinkAnswer = x.ClaimData.FirstOrDefault()?.ClaimLinkChallengeAnswer,
                                    LineItemId = x.ClaimData.FirstOrDefault()?.Id,
                                    Status = x.Status,
                                    DenominationType = request.EGiftDetail.DenominationType,
                                    ProductId = x.ProductId,
                                    CostAmount = x.Cost,
                                    UserCardDetaileGiftId = userDetailGift.Id,
                                    ValueAmount = request.EGiftDetail.CardValue,
                                    BrandName = request.EGiftDetail.BrandName,
                                    CardImageUrl = request.EGiftDetail.CardImageUrl,
                                    PrimaryColor = request.EGiftDetail.PrimaryColor,
                                    SecondaryColor = request.EGiftDetail.SecondaryColor
                                }).ToList();
                                await db.UserCardDetaileGiftLineItems.AddRangeAsync(lineItems);
                                await db.SaveChangesAsync();

                                try
                                {
                                    if (toUser != null)
                                    {
                                        var getFromUser = await db.Users.Include(x => x.UserProfiles).FirstOrDefaultAsync(x => x.Id == userId);
                                        var fromName = getFromUser.UserProfiles.FirstOrDefault()?.FirstName + " " +
                                                       getFromUser.UserProfiles.FirstOrDefault()?.LastName;
                                        var getEgifterProduct = await db.EgifterProducts.FirstOrDefaultAsync(x =>
                                            x.IsEnabled && x.ProductId == request.EGiftDetail.ProductId);

                                        _ = _emailTemplateService.CongratulationsCardReceived(
                                                toUser.Email,
                                                (decimal)request.EGiftDetail.CardValue,
                                                fromName, request.EGiftDetail.BrandName,
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
                            else
                            {
                                var lineItem = new UserCardDetaileGiftLineItems
                                {
                                    Id = Guid.NewGuid(),
                                    CreatedOn = DateTime.UtcNow,
                                    IsEnabled = true,
                                    CreatedBy = userId.ToString(),
                                    CreatedOnDate = DateTime.UtcNow,
                                    DenominationType = request.EGiftDetail.DenominationType,
                                    ProductId = request.EGiftDetail.ProductId,
                                    CostAmount = request.EGiftDetail.CardValue,
                                    UserCardDetaileGiftId = userDetailGift.Id,
                                    ValueAmount = request.EGiftDetail.CardValue,
                                    BrandName = request.EGiftDetail.BrandName,
                                    CardImageUrl = request.EGiftDetail.CardImageUrl,
                                    PrimaryColor = request.EGiftDetail.PrimaryColor,
                                    SecondaryColor = request.EGiftDetail.SecondaryColor
                                };
                                await db.UserCardDetaileGiftLineItems.AddAsync(lineItem);
                                await db.SaveChangesAsync();
                            }
                        }

                        if (!cardMaster.IsScheduled)
                        {
                            cardDetails.ForEach(x =>
                            {
                                try
                                {
                                    var profile =
                                        db.UserProfiles.FirstOrDefault(y => y.IsEnabled && y.UserId == userId);
                                    var name = profile.FirstName + " " + profile.LastName;

                                    var egiftString = x.IseGiftAttached == true ? "giftcard" : "greeting";

                                    if (x.SentToId != null)
                                    {
                                        var sentToProfile =
                                            db.UserProfiles.FirstOrDefault(y => y.IsEnabled && y.UserId == x.SentToId);
                                        var sentToName = sentToProfile.FirstName + " " + sentToProfile.LastName;
                                        var sentToPhoneNumber = sentToProfile.User.PhoneNumber;
                                        var res = notificationService.SentYouCard(userId, (Guid)x.SentToId, x.Id, x.UserCardMaster.CardCategory.Category.Name, db).Result;
                                        _ = Task.Run(() =>
                                        {
                                            _ = smsService.SendSmsToUser(
                                                "You received an EasyPeasy " + egiftString + " from " + name + ". https://easypeasycards.com/go?id=" + x.Id.ToString(), sentToPhoneNumber, false);
                                        });
                                    }
                                    else if (!string.IsNullOrWhiteSpace(x.SentToPhoneNumber))
                                    {

                                        //var userCategoryName = x.UserCardMaster.CardCategory.Category.Name;
                                        var sentToPhoneNumber = x.SentToPhoneNumber;
                                        _ = Task.Run(() =>
                                        {
                                            _ = smsService.SendSmsToUser(
                                                "You received an EasyPeasy " + egiftString + " from " + name + ". https://easypeasycards.com/go?id=" + x.Id.ToString(), sentToPhoneNumber, false);
                                        });
                                    }


                                }
                                catch (Exception e)
                                {
                                    // ignored
                                }
                            });
                        }

                        await db.SaveChangesAsync();
                        await trans.CommitAsync();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        await trans.RollbackAsync();
                        throw;
                    }
                }
            }
        }
    }
}
