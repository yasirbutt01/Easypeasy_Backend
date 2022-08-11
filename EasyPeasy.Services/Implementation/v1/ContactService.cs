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
using System.Threading.Tasks;

namespace EasyPeasy.Services.Implementation.v1
{
    public class ContactService : IContactService
    {
        private readonly ISmsService smsService;

        public ContactService(ISmsService smsService)
        {
            this.smsService = smsService;
        }
        public async Task<bool> Invite(List<ContactInviteRequest> requests, Guid userId)
        {
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    var inviteDb = new List<UserInvites>();

                    var x = await db.Users.Include(y => y.UserProfiles).FirstOrDefaultAsync(y => y.Id == userId);
                    var name = x.UserProfiles.FirstOrDefault(y => y.IsEnabled)?.FirstName + " " +
                               x.UserProfiles.FirstOrDefault(y => y.IsEnabled)?.LastName;
                    foreach (var request in requests)
                    {
                        var invite = new UserInvites()
                        {
                            Id = Guid.NewGuid(),
                            CreatedBy = userId.ToString(),
                            CreatedOn = DateTime.UtcNow,
                            CreatedOnDate = DateTime.UtcNow,
                            IsEnabled = true,
                            UserId = userId,
                            FirstName = request.FirstName,
                            CountryCode = "",
                            PhoneNumber = request.PhoneNumber,
                            LastName = request.LastName
                        };
                        inviteDb.Add(invite);
                        _ = smsService.SendSmsToUser(
                            $"{name} requested you to install EasyPeasy App https://easypeasycards.com/go", request.PhoneNumber, false);
                    }

                    await db.UserInvites.AddRangeAsync(inviteDb);
                    await db.SaveChangesAsync();

                    return true;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<List<ContactGetGroupResponse>> GetGroups(Guid userId)
        {
            try
            {
                var response = new List<ContactGetGroupResponse>();
                using (var db = new EasypeasyDbContext())
                {
                    response = await db.UserGroups.Where(x => x.IsEnabled && x.UserId == userId).Select(x => new ContactGetGroupResponse
                    {
                        Id = x.Id,
                        GroupName = x.Name,
                        PeopleCount = x.UserGroupUsers.Count(y => y.IsEnabled)
                    }).ToListAsync();
                    return response;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<List<ContactGetGroupDetailResponse>> GetGroupDetail(Guid groupId)
        {
            try
            {
                var response = new List<ContactGetGroupDetailResponse>();
                using (var db = new EasypeasyDbContext())
                {
                    var getGroupUsers = await db.UserGroupUsers.Where(x => x.IsEnabled && x.UserGroupId == groupId).ToListAsync();

                    getGroupUsers.ForEach(x =>
                    {
                        x.UserId = db.Users.FirstOrDefault(y =>
                            y.IsEnabled && y.PhoneNumber == (x.CountryCode + x.PhoneNumber))?.Id;
                    });
                    await db.SaveChangesAsync();

                    response = getGroupUsers.Select(x => new ContactGetGroupDetailResponse
                    {
                        UserId = x.UserId,
                        Name = x.User != null ? x.User.UserProfiles.FirstOrDefault(y => y.IsEnabled)?.FirstName + " " + x.User.UserProfiles.FirstOrDefault(y => y.IsEnabled)?.LastName
                            : x.ContactFirstName + " " + x.ContactLastName,
                        PhoneNumber = x.User != null ? x.User.PhoneNumber : x.CountryCode + x.PhoneNumber
                    }).ToList();
                    return response;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<bool> CreateCalenderEvent(ContactCreateCalenderEventRequest request, Guid userId)
        {
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    var userCalenderId = Guid.NewGuid();
                    var categoryName = db.Categories.FirstOrDefaultAsync(x => x.Id == request.CategoryId)?.Result?.Name;
                    var userCalender = new UserCalenders
                    {
                        Id = userCalenderId,
                        CreatedBy = userId.ToString(),
                        CreatedOn = DateTime.UtcNow,
                        CreatedOnDate = DateTime.UtcNow,
                        IsEnabled = true,
                        UserId = userId,
                        AlertBeforeInMinute = request.IsReminder ? 1440 : -1,
                        CategoryId = request.CategoryId,
                        Date = request.Date,
                        IsGroup = request.IsGroup,
                        Tag = categoryName,
                        UserContactId = request.ContactId,
                        UserContactUserId = request.ContactUserId,
                        UserGroupId = request.UserGroupId
                    };
                    await db.UserCalenders.AddAsync(userCalender);
                    await db.SaveChangesAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<bool> UpdateGroup(ContactUpdateGroupRequest request, Guid userId)
        {
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    var group = await db.UserGroups.FirstOrDefaultAsync(x => x.IsEnabled && x.UserId == userId && x.Id == request.Id);
                    group.Name = request.Name;

                    var groupPeople = group.UserGroupUsers.ToList();
                    groupPeople.ForEach(x =>
                    {
                        x.IsEnabled = false;
                        x.DeletedBy = userId.ToString();
                        x.DeletedOn = DateTime.UtcNow;
                    });

                    var UserGroupUsers = request.details.Select(x => new UserGroupUsers
                    {
                        Id = Guid.NewGuid(),
                        CreatedBy = userId.ToString(),
                        CreatedOn = DateTime.UtcNow,
                        CreatedOnDate = DateTime.UtcNow,
                        IsEnabled = true,
                        UserGroupId = group.Id,
                        UserId = x.UserId ?? db.Users.FirstOrDefault(z => z.IsEnabled && (x.CountryCode + x.PhoneNumber) == x.PhoneNumber)?.Id,
                        PhoneNumber = x.PhoneNumber,
                        ContactFirstName = x.ContactFirstName,
                        ContactLastName = x.ContactLastName,
                        CountryCode = x.CountryCode
                    }).ToList();
                    await db.UserGroupUsers.AddRangeAsync(UserGroupUsers);
                    await db.SaveChangesAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<bool> CreateGroup(ContactCreateGroupRequest request, Guid userId)
        {
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    //if (await db.UserGroups.AnyAsync(x => x.IsEnabled && x.UserId == userId && x.Name == request.Name))
                    //{
                    //    return false;
                    //}

                    foreach (var x in request.details)
                    {
                        if (x.UserId == null && string.IsNullOrWhiteSpace(x.PhoneNumber))
                        {
                            return false;
                        }
                    }

                    var userGroupId = Guid.NewGuid();
                    var userGroup = new UserGroups
                    {
                        Id = userGroupId,
                        CreatedBy = userId.ToString(),
                        CreatedOn = DateTime.UtcNow,
                        CreatedOnDate = DateTime.UtcNow,
                        IsEnabled = true,
                        Name = request.Name,
                        UserId = userId,
                        UserGroupUsers = request.details.Select(x => new UserGroupUsers
                        {
                            Id = Guid.NewGuid(),
                            CreatedBy = userId.ToString(),
                            CreatedOn = DateTime.UtcNow,
                            CreatedOnDate = DateTime.UtcNow,
                            IsEnabled = true,
                            UserGroupId = userGroupId,
                            UserId = x.UserId ?? db.Users.FirstOrDefault(z => z.IsEnabled && (x.CountryCode + x.PhoneNumber) == x.PhoneNumber)?.Id,
                            PhoneNumber = x.PhoneNumber,
                            ContactFirstName = x.ContactFirstName,
                            ContactLastName = x.ContactLastName,
                            CountryCode = x.CountryCode
                        }).ToList()
                    };
                    await db.UserGroups.AddAsync(userGroup);
                    await db.SaveChangesAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<List<ContactGetEventsResponse>> GetAllEvents(DateTime startDate, DateTime endDate, Guid userId)
        {
            try
            {
                var response = new List<ContactGetEventsResponse>();
                using (var db = new EasypeasyDbContext())
                {
                    // Add Created Events From Calender
                    response = await db.UserCalenders
                        .Include(x => x.Category)
                        .Include(x => x.UserGroup)
                        .Include(x => x.UserContact)
                        .Where(x => x.IsEnabled && x.UserId == userId && x.Date >= startDate && x.Date <= endDate)
                        .Select(x => new ContactGetEventsResponse
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
                    var getScheduledCards = await db.UserCardMaster.Where(x => x.IsEnabled && x.SentFromId == userId && x.IsScheduled && x.ScheduledDate >= startDate && x.ScheduledDate <= endDate).Select(x => new ContactGetEventsResponse
                    {
                        AlertBeforeInMinute = x.IsReminder ? 1440 : -1,
                        IsReminder = x.IsReminder,
                        Cards = new List<ContactGetEventCardsResponse>() { new ContactGetEventCardsResponse
                        {
                            CardFileThumbnailUrl = x.Card.CardFileThumbnailUrl,
                            CardFileUrl = x.Card.CardFileUrl,
                            CardId = x.CardId,
                            EnumCardType = ECardType.GreetingCard,
                            IseGiftAttached = x.UserCardDetails.FirstOrDefault().IseGiftAttached ?? false,
                            EGiftDetail = (x.UserCardDetails.FirstOrDefault().IseGiftAttached == true) ? new GetCardSendeGiftResponse()
                            {
                                CardValue = x.UserCardDetails.FirstOrDefault().UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().ValueAmount ?? 0,
                                BrandName = x.UserCardDetails.FirstOrDefault().UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().BrandName,
                                ClaimLink = x.UserCardDetails.FirstOrDefault().UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().ClaimLink,
                                CardImageUrl = x.UserCardDetails.FirstOrDefault().UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().CardImageUrl,
                                ClaimLinkAnswer = x.UserCardDetails.FirstOrDefault().UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().ClaimLinkAnswer,
                                ProductId = x.UserCardDetails.FirstOrDefault().UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().ProductId,
                                PrimaryColor = x.UserCardDetails.FirstOrDefault().UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().PrimaryColor,
                                SecondaryColor = x.UserCardDetails.FirstOrDefault().UserCardDetaileGifts.FirstOrDefault().UserCardDetaileGiftLineItems.FirstOrDefault().SecondaryColor,
                            } : new GetCardSendeGiftResponse(),
                            GreetingTypeId = x.CardCategory.Category.GreetingTypeId,
                            Images = x.UserCardMasterImages.Select(y => new GetCardImageResponse
                            {
                                ImageFileThumbnailUrl = y.ImageFileThumbnailUrl,
                                ImageFileUrl = y.ImageFileUrl
                            }).ToList()
                        }},
                        CategoryText = x.CardCategory.Category.Name ?? "",
                        ContactCount = x.UserCardDetails.Count(y => y.IsEnabled),
                        Date = x.ScheduledDate,
                        Title = "",
                        Contacts = x.IsSentInGroup ? x.UserCardDetails.Where(y => y.IsEnabled).Select(y => y.UserGroup.Name ?? "").ToList() :
                                    x.UserCardDetails.Select(y => (y.SentTo.UserProfiles.FirstOrDefault(z => z.IsEnabled).FirstName + " " + y.SentTo.UserProfiles.FirstOrDefault(z => z.IsEnabled).LastName) ?? "").ToList(),
                        EnumEventSource = ECalenderEventSource.EasyPease,
                        IsGroup = x.IsSentInGroup
                    }).ToListAsync();
                    response.AddRange(getScheduledCards);

                    // Add From Synced Contacts
                    var userIdText = userId.ToString();
                    var getSyncedContactEvents = await db.UserContactInformations.Where(x => x.IsEnabled && x.UserContact.IsEnabled && x.CreatedBy == userIdText && ((x.Date >= startDate && x.Date <= endDate && x.Tag.ToLower() != "anniversary" && x.Tag.ToLower() != "birthday") ||
                                                    (x.Tag.ToLower() == "anniversary" && x.Date.Value.Month >= startDate.Month && x.Date.Value.Month <= endDate.Month) ||
                                                    (x.Tag.ToLower() == "birthday" && x.Date.Value.Month >= startDate.Month && x.Date.Value.Month <= endDate.Month))).Select(x => new ContactGetEventsResponse
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
    }
}
