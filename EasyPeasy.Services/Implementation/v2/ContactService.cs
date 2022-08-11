using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyPeasy.Data.Context;
using EasyPeasy.DataViewModels.Enum;
using EasyPeasy.DataViewModels.Requests.v1;
using EasyPeasy.DataViewModels.Response.v1;
using EasyPeasy.Services.Interface.v2;
using Microsoft.EntityFrameworkCore;

namespace EasyPeasy.Services.Implementation.v2
{
    public class ContactService: IContactService
    {
        public async Task<List<DataViewModels.Response.v2.ContactGetEventsResponse>> GetAllEvents(DateTime startDate, DateTime endDate, Guid userId)
        {
            try
            {
                var response = new List<DataViewModels.Response.v2.ContactGetEventsResponse>();
                using (var db = new EasypeasyDbContext())
                {
                    // Add Created Events From Calender
                    response = await db.UserCalenders
                        .Include(x => x.Category)
                        .Include(x => x.UserGroup)
                        .Include(x => x.UserContact)
                        .Where(x => x.IsEnabled && x.UserId == userId && x.Date >= startDate && x.Date <= endDate)
                        .Select(x => new DataViewModels.Response.v2.ContactGetEventsResponse
                        {
                            AlertBeforeInMinute = x.AlertBeforeInMinute,
                            CategoryText = x.Category.Name ?? "",
                            ContactCount = x.IsGroup ? x.UserGroup.UserGroupUsers.Count(y => y.IsEnabled) : 1,
                            Date = x.Date,
                            EnumEventSource = ECalenderEventSource.EasyPease,
                            IsReminder = x.AlertBeforeInMinute == -1 ? false : true,
                            Title = x.IsGroup ? x.UserGroup.Name ?? "" : x.UserContact.FirstName ?? "" + " " + x.UserContact.LastName ?? "",
                            IsGroup = x.IsGroup
                        }).ToListAsync();

                    // Add Scheduled Cards
                    var getScheduledCards = await db.UserCardMaster.Where(x => x.IsEnabled && x.SentFromId == userId && x.IsScheduled && x.ScheduledDate >= startDate && x.ScheduledDate <= endDate).Select(x => new DataViewModels.Response.v2.ContactGetEventsResponse
                    {
                        AlertBeforeInMinute = x.IsReminder ? 1440 : -1,
                        IsReminder = x.IsReminder,
                        Cards = new List<DataViewModels.Response.v2.ContactGetEventCardsResponse>() { new DataViewModels.Response.v2.ContactGetEventCardsResponse
                        {
                            CardFileThumbnailUrl = x.Card.CardFileThumbnailUrl,
                            CardFileUrl = x.Card.CardFileUrl,
                            CardId = x.CardId,
                            GreetingMasterId = x.Id,
                            EnumCardType = ECardType.GreetingCard,
                            IseGiftAttached = x.UserCardDetails.FirstOrDefault().IseGiftAttached ?? false,
                            EGiftDetail = (x.UserCardDetails.FirstOrDefault().IseGiftAttached == true) ? new DataViewModels.Response.v1.GetCardSendeGiftResponse()
                            {
                                CardValue = x.UserCardDetails.FirstOrDefault().UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().ValueAmount ?? 0,
                                BrandName = x.UserCardDetails.FirstOrDefault().UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().BrandName,
                                ClaimLink = x.UserCardDetails.FirstOrDefault().UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().ClaimLink,
                                CardImageUrl = x.UserCardDetails.FirstOrDefault().UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().CardImageUrl,
                                ClaimLinkAnswer = x.UserCardDetails.FirstOrDefault().UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().ClaimLinkAnswer,
                                ProductId = x.UserCardDetails.FirstOrDefault().UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().ProductId,
                                PrimaryColor = x.UserCardDetails.FirstOrDefault().UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().PrimaryColor,
                                SecondaryColor = x.UserCardDetails.FirstOrDefault().UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().SecondaryColor,
                            } : new DataViewModels.Response.v1.GetCardSendeGiftResponse(),
                            GreetingTypeId = x.CardCategory.Category.GreetingTypeId,
                            Images = x.UserCardMasterImages.Select(y => new DataViewModels.Response.v1.GetCardImageResponse
                            {
                                ImageFileThumbnailUrl = y.ImageFileThumbnailUrl,
                                ImageFileUrl = y.ImageFileUrl
                            }).ToList()
                        }},
                        CategoryText = x.CardCategory.Category.Name ?? "",
                        ContactCount = x.UserCardDetails.Count(y => y.IsEnabled),
                        Date = x.ScheduledDate,
                        Title = "",
                        Contacts = new List<string>(){
                            x.IsSentInGroup ? (x.UserCardDetails.FirstOrDefault().UserGroup.Name ?? "") :
                                (x.UserCardDetails.FirstOrDefault().SentToId != null ? ((db.UserProfiles.FirstOrDefault(y => y.IsEnabled && y.UserId.Equals(x.UserCardDetails.FirstOrDefault().SentToId)).FirstName ?? "") + " " + (db.UserProfiles.FirstOrDefault(y => y.IsEnabled && y.UserId.Equals(x.UserCardDetails.FirstOrDefault().SentToId)).LastName ?? "")) : x.UserCardDetails.FirstOrDefault().SentToName),

                        },
                        EnumEventSource = ECalenderEventSource.EasyPease,
                        IsGroup = x.IsSentInGroup
                    }).ToListAsync();
                    response.AddRange(getScheduledCards);

                    // Add From Synced Contacts
                    var userIdText = userId.ToString();
                    var getSyncedContactEvents = await db.UserContactInformations.Where(x => x.IsEnabled && x.UserContact.IsEnabled && x.CreatedBy == userIdText && ((x.Date >= startDate && x.Date <= endDate && x.Tag.ToLower() != "anniversary" && x.Tag.ToLower() != "birthday") ||
                                                    (x.Tag.ToLower() == "anniversary" && x.Date.Value.Month >= startDate.Month && x.Date.Value.Month <= endDate.Month) ||
                                                    (x.Tag.ToLower() == "birthday" && x.Date.Value.Month >= startDate.Month && x.Date.Value.Month <= endDate.Month))).Select(x => new DataViewModels.Response.v2.ContactGetEventsResponse
                                                    {
                                                        AlertBeforeInMinute = -1,
                                                        CategoryText = x.Tag,
                                                        ContactCount = 1,
                                                        Date = (DateTime)x.Date,
                                                        IsReminder = false,
                                                        Title = x.UserContact.FirstName ?? "" + " " + x.UserContact.LastName ?? "",
                                                        EnumEventSource = ECalenderEventSource.SyncFromCalender,
                                                        IsGroup = false
                                                    }).ToListAsync();
                    response.AddRange(getSyncedContactEvents);

                    return response.OrderByDescending(x => x.Date).ToList();
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<List<ContactCreateGroupDetailRequest>> GetGroupDetail(Guid groupId)
        {
            try
            {
                var response = new List<ContactCreateGroupDetailRequest>();
                using (var db = new EasypeasyDbContext())
                {
                    var getGroupUsers = await db.UserGroupUsers.Where(x => x.IsEnabled && x.UserGroupId == groupId).ToListAsync();

                    getGroupUsers.ForEach(x =>
                    {
                        x.UserId = db.Users.FirstOrDefault(y =>
                            y.IsEnabled && y.PhoneNumber == (x.CountryCode + x.PhoneNumber))?.Id;
                    });
                    await db.SaveChangesAsync();

                    response = getGroupUsers.Select(x => new ContactCreateGroupDetailRequest
                    {
                        UserId = x.UserId,
                        ContactFirstName = x.User != null ? x.User.UserProfiles.FirstOrDefault(y => y.IsEnabled)?.FirstName
                            : x.ContactFirstName,
                        ContactLastName = x.User != null ? x.User.UserProfiles.FirstOrDefault(y => y.IsEnabled)?.LastName
                            : x.ContactLastName,
                        PhoneNumber = x.User != null ? x.User.PhoneNumber.Replace(x.CountryCode, "") :  x.PhoneNumber,
                        CountryCode = x.User != null ? x.User.CountryCode : x.CountryCode
                    }).ToList();
                    return response;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
