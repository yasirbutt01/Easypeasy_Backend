using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyPeasy.Data.Context;
using EasyPeasy.DataViewModels.Enum;
using EasyPeasy.DataViewModels.Response.v1;
using EasyPeasy.DataViewModels.Response.v2;
using EasyPeasy.Services.Interface.v1;
using Microsoft.EntityFrameworkCore;
using GetCardImageResponse = EasyPeasy.DataViewModels.Response.v2.GetCardImageResponse;
using GetCardResponse = EasyPeasy.DataViewModels.Response.v2.GetCardResponse;
using GetCardSendeGiftPaymentMethodResponse = EasyPeasy.DataViewModels.Response.v2.GetCardSendeGiftPaymentMethodResponse;
using GetCardSendeGiftResponse = EasyPeasy.DataViewModels.Response.v2.GetCardSendeGiftResponse;
using GetMyCardResponse = EasyPeasy.DataViewModels.Response.v2.GetMyCardResponse;
using ICardService = EasyPeasy.Services.Interface.v2.ICardService;

namespace EasyPeasy.Services.Implementation.v2
{
    public class CardService: ICardService
    {
        private readonly INotificationService notificationService;
        private readonly ISmsService smsService;

        public CardService(INotificationService notificationService, ISmsService smsService)
        {
            this.notificationService = notificationService;
            this.smsService = smsService;
        }

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
                        Name = x.UserCardMaster.IsSentInGroup ? (x.UserGroup?.Name ?? "") :
                            (x.SentToId != null ? (db.UserProfiles.FirstOrDefault(y => y.IsEnabled && y.UserId.Equals(x.SentToId))?.FirstName ?? "") + " " + (db.UserProfiles.FirstOrDefault(y => y.IsEnabled && y.UserId.Equals(x.SentToId))?.LastName ?? "") : x.SentToName),
                        PhoneNumber = x.SentTo?.PhoneNumber ?? x.SentToPhoneNumber,
                        GroupCount = x.UserGroup?.UserGroupUsers?.Count ?? 0,
                        ContactCount = db.UserCardDetails.Count(y => y.IsEnabled && y.UserCardMasterId == x.UserCardMasterId),
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
                        GreetingMasterId = x.UserCardMasterId,
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

        public async Task<bool> CardAction(List<Guid> greetingIds, ECardAction action, Guid userId)
        {
            using (var db = new EasypeasyDbContext())
            {
                try
                {
                    foreach (var greetingId in greetingIds)
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

        public async Task<List<CardRecipientListResponse>> GetCardRecipients(Guid greetingMasterId, Guid userId)
        {
            using (var db = new EasypeasyDbContext())
            {
                try
                {
                    var response = new List<CardRecipientListResponse>();

                    response = await db.UserCardDetails
                        .Where(x => x.IsEnabled && x.UserCardMasterId == greetingMasterId).Select(
                            x => new CardRecipientListResponse
                            {
                                Id = x.Id,
                                PhoneNumber = x.SentTo.PhoneNumber ?? x.SentToPhoneNumber,
                                IsOpen = x.IsOpen,
                                IsOpenDate = x.IsOpenDate,
                                Name = x.SentToId != null ? (db.UserProfiles.FirstOrDefault(y => y.IsEnabled && y.UserId.Equals(x.SentToId)).FirstName ?? "") + " " + (db.UserProfiles.FirstOrDefault(y => y.IsEnabled && y.UserId.Equals(x.SentToId)).LastName ?? "") : x.SentToName,
                            }).ToListAsync();

                    return response;
                }
                catch (Exception e)
                {
                    throw;
                }

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
                  GreetingMasterId = x.Id,
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
                  ContactCount = x.UserCardDetails.Count(),
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
    }
}
