using EasyPeasy.Data.Context;
using EasyPeasy.DataViewModels.Common;
using EasyPeasy.DataViewModels.Response.admin;
using EasyPeasy.Services.Interface.admin;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyPeasy.Services.Implementation.admin
{
    public class UserService : IUserService
    {
        public GenralListResponse<GetUserResponse> GetUsers(GenralListResponse<GetUserResponse> page, DateTime? str, DateTime? endd, List<Guid> packages)
        {
            DateTime strt = Convert.ToDateTime(str);
            DateTime end = Convert.ToDateTime(endd).AddDays(1);
            GenralListResponse<GetUserResponse> data = new GenralListResponse<GetUserResponse>();

            using (var db = new EasypeasyDbContext())
            {
                var query = db.Users.Where(x => x.CreatedOn >= strt && x.CreatedOn <= end && !x.IsGuest)
               .Select(x => new GetUserResponse
               {
                   UserId = x.Id,
                   PhoneNumber = x.PhoneNumber,
                   IsGuest = x.IsGuest,
                   SignUp = x.CreatedOn,
                   Email = x.UserProfiles.FirstOrDefault(y => y.IsEnabled).Email ?? "",
                   Name = x.UserProfiles.Where(y => y.IsEnabled).Select(y => y.FirstName + " " + y.LastName).FirstOrDefault(),
                   Gender = x.UserProfiles.Where(y => y.IsEnabled).Select(y => y.Gender.Name).FirstOrDefault(),
                   PackageId = x.UserSubscriptions.Where(y => y.IsEnabled).OrderByDescending(x => x.CreatedOn).FirstOrDefault().PackageId,
                   PackageName = x.UserSubscriptions.Where(y => y.IsEnabled).OrderByDescending(x => x.CreatedOn).FirstOrDefault().Package.Name ?? "N/A",
                   IsActive = x.IsBlocked
               }).AsQueryable();

                if (packages.Count > 0)
                {
                    query = query.Where(x => packages.Contains(x.PackageId));
                }

                if (!string.IsNullOrEmpty(page.Search))
                {
                    var date = new DateTime();
                    var sdate = DateTime.TryParse(page.Search, out date);
                    int totalCases = -1;
                    var isNumber = Int32.TryParse(page.Search, out totalCases);
                    if (sdate)
                    {
                        query = query.Where(
                            x => x.SignUp.Date == date.Date
                        );
                    }
                    //else if (isNumber)
                    //{
                    //    //query = query.Where(
                    //    //    x => x.StockId == totalCases
                    //    //);
                    //}
                    else
                    {
                        query = query.Where(
                        x => x.Name.ToLower().Contains(page.Search.ToLower())
                        || x.Email.ToLower().Contains(page.Search.ToLower())
                        || x.PhoneNumber.ToLower().Contains(page.Search.ToLower())
                        || x.Gender.ToLower().Contains(page.Search.ToLower())
                        || x.PackageName.ToLower().Contains(page.Search.ToLower())
                        );
                    }
                }
                var orderedQuery = query.OrderByDescending(x => x.SignUp);
                switch (page.SortIndex)
                {
                    case 0:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name);
                        break;
                    case 1:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.PhoneNumber) : query.OrderBy(x => x.PhoneNumber);
                        break;
                    case 2:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.Email) : query.OrderBy(x => x.Email);
                        break;
                    case 3:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.Gender) : query.OrderBy(x => x.Gender);
                        break;
                    case 4:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.IsGuest) : query.OrderBy(x => x.IsGuest);
                        break;
                    case 5:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.PackageName) : query.OrderBy(x => x.PackageName);
                        break;
                    case 6:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.SignUp) : query.OrderBy(x => x.SignUp);
                        break;
                    case 7:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.IsActive) : query.OrderBy(x => x.IsActive);
                        break;
                }


                data.Page = page.Page;
                data.PageSize = page.PageSize;
                data.TotalRecords = orderedQuery.Count();
                data.Draw = page.Draw;
                data.Entities = orderedQuery.Skip(page.Page).Take(page.PageSize).ToList();
            }

            return data;
        }

        public async Task<bool> ControlActivation(Guid id, bool activation, string userId)
        {
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    var user = db.Users.FirstOrDefault(x => x.Id.Equals(id));

                    if (user == null) throw new ArgumentException("id not found!");

                    user.IsBlocked = activation;
                    user.UpdatedOn = DateTime.UtcNow;
                    user.UpdatedBy = userId;

                    db.Entry(user).State = EntityState.Modified;
                    await db.SaveChangesAsync();

                    return true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<UserDetailResponse> UserDetail(Guid userId)
        {
            try
            {
                UserDetailResponse response = new UserDetailResponse();
                var currentDate = DateTime.UtcNow;

                using (var db = new EasypeasyDbContext())
                {
                    response.Id = userId;

                    response.Profile = await db.UserProfiles
                        .Where(x => x.IsEnabled && x.UserId.Equals(userId))
                        .Select(x => new GetUserProfile
                        {
                            Email = x.Email,
                            FirstName = x.FirstName,
                            LastName = x.LastName,
                            Gender = x.Gender.Name,
                            DateOfBirth = x.DateOfBirth,
                            ImageUrl = x.ImageUrl,
                            ImageThumbnailUrl = x.ImageThumbnailUrl,
                            CropImageUrl = x.CropImageUrl,
                            CropImageThumbnailUrl = x.CropImageThumbnailUrl,
                        })
                        .FirstOrDefaultAsync();

                    response.ReciveCardList = await db.UserCardDetails
                        .Where(x => x.SentToId.Equals(userId) && !x.IsDeletedFromTo && x.UserCardMaster.IsCardSent == true && x.UserCardMaster.ScheduledDate <= currentDate && x.IsEnabled == true)
                        .OrderByDescending(x => x.UserCardMaster.ScheduledDate)
                    .Select(x => new GetUserCardResponse
                    {
                        Id = x.UserCardMasterId,
                        Date = x.UserCardMaster.ScheduledDate,
                        Description = x.UserCardMaster.Description,
                        IsAchived = x.IsArchiveFromFrom,
                        IsScheduled = x.UserCardMaster.IsScheduled,
                        IsEGiftAttached = x.IseGiftAttached ?? false,
                        IsOpen = x.IsOpen,
                        Name = x.SentFrom.UserProfiles.FirstOrDefault(y => y.IsEnabled).FirstName + " " + x.SentFrom.UserProfiles.FirstOrDefault(y => y.IsEnabled).LastName,
                        CategoryName = x.UserCardMaster.CardCategory.Category.Name,
                        GreetingTypeId = x.UserCardMaster.CardCategory.Category.GreetingTypeId,
                        ThumbnailUrl = x.UserCardMaster.Card.CardFileUrl,
                        VideoUrl = x.UserCardMaster.VideoUrl,
                        VideoThumbnailUrl = x.UserCardMaster.VideoThumbUrl,
                        FontId = x.UserCardMaster.FontId,
                        MusicUrl = x.UserCardMaster.CategoryMusicFile.Mp3FileUrl,
                        Images = x.UserCardMaster.UserCardMasterImages.Select(y => new FileUrlResponce
                        {
                            ThumbUrl = y.ImageFileThumbnailUrl,
                            URL = y.ImageFileUrl
                        }).ToList()
                    }).ToListAsync();

                    response.SentCardList = await db.UserCardMaster
                        .Where(x => x.SentFromId.Equals(userId) && x.IsEnabled == true)
                        .OrderByDescending(x => x.CreatedOn)
                    .Select(x => new GetUserCardResponse
                    {
                        Id = x.Id,
                        Date = x.ScheduledDate,
                        Description = x.Description,
                        IsAchived = x.UserCardDetails.FirstOrDefault(x => x.IsEnabled).IsArchiveFromTo,
                        IsScheduled = x.IsScheduled,
                        IsEGiftAttached = x.UserCardDetails.FirstOrDefault(x => x.IsEnabled).IseGiftAttached ?? false,
                        IsOpen = x.UserCardDetails.FirstOrDefault(x => x.IsEnabled).IsOpen,
                        Names = x.UserCardDetails.Where(x => x.IsEnabled).Select(y => y.SentTo == null ? y.SentToName : y.SentTo.UserProfiles.FirstOrDefault(z => z.IsEnabled).FirstName + " " + y.SentTo.UserProfiles.FirstOrDefault(z => z.IsEnabled).LastName).ToList(),
                        CategoryName = x.CardCategory.Category.Name,
                        GreetingTypeId = x.CardCategory.Category.GreetingTypeId,
                        ThumbnailUrl = x.Card.CardFileUrl,
                        VideoUrl = x.VideoUrl,
                        VideoThumbnailUrl = x.VideoThumbUrl,
                        FontId = x.FontId,
                        MusicUrl = x.CategoryMusicFile.Mp3FileUrl,
                        Images = x.UserCardMasterImages.Select(y => new FileUrlResponce
                        {
                            ThumbUrl = y.ImageFileThumbnailUrl,
                            URL = y.ImageFileUrl
                        }).ToList()
                    }).ToListAsync();

                    response.SubscriptionHistory.Add(await db.UserSubscriptions.Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedOn).Select(x => new PaymentSubscriptionItemResponse
                    {
                        Id = x.Id,
                        CreationDate = x.CreatedOn,
                        PackageId = x.PackageId,
                        ExpiryDate = x.PsNextBillingDate,
                        Limit = x.Limit ?? -1,
                        Price = x.Price,
                        PackageName = x.Package.Name,
                        PackageColor = x.Package.ColorCode,
                        Status = x.PsStatus ?? "",
                        Code = x.PsSubscriptionId,
                        IsCrruentPackage = true,
                        PaymentMethod = new PaymentAddPaymentMethodResponse
                        {
                            Id = x.UserPaymentMethod.Id,
                            CardMaskedCardNumber = x.UserPaymentMethod.CardMaskedNumber,
                            CardType = x.UserPaymentMethod.CardType,
                            ExpiryDate = x.UserPaymentMethod.ExpiryDate,
                            PaymentMethodType = x.UserPaymentMethod.PaymentMethodTypeId,
                            PaypalEmail = x.UserPaymentMethod.Email ?? "",
                            IsDefault = x.UserPaymentMethod.IsDefault ?? false,
                            PaymentMethodImageUrl = x.UserPaymentMethod.PaymentMethodImageUrl,

                            CreationDate = x.CreatedOn,
                        }
                    }).FirstOrDefaultAsync());

                    response.SubscriptionHistory.AddRange(await db.UserInvoices.Where(x => x.IsEnabled && x.UserId == userId).OrderByDescending(x => x.CreatedOn).Skip(1).Select(x => new PaymentSubscriptionItemResponse
                    {
                        Id = x.UserSubscription.Id,
                        CreationDate = x.CreatedOn,
                        ExpiryDate = x.UserSubscription.PsNextBillingDate,
                        Limit = x.UserSubscription.Limit,
                        Price = x.Price,
                        PackageId = x.UserSubscription.PackageId,
                        PackageName = x.UserSubscription.Package.Name,
                        PackageColor = x.UserSubscription.Package.ColorCode,
                        Status = x.UserSubscription.PsStatus ?? "",
                        Code = x.TransactionId,
                        IsCrruentPackage = false,
                        PaymentMethod = new PaymentAddPaymentMethodResponse
                        {
                            Id = x.UserSubscription.UserPaymentMethod.Id,
                            CardMaskedCardNumber = x.UserSubscription.UserPaymentMethod.CardMaskedNumber,
                            CardType = x.UserSubscription.UserPaymentMethod.CardType,
                            ExpiryDate = x.UserSubscription.UserPaymentMethod.ExpiryDate,
                            PaymentMethodType = x.UserSubscription.UserPaymentMethod.PaymentMethodTypeId,
                            PaypalEmail = x.UserSubscription.UserPaymentMethod.Email ?? "",
                            IsDefault = x.UserSubscription.UserPaymentMethod.IsDefault ?? false,
                            PaymentMethodImageUrl = x.UserSubscription.UserPaymentMethod.PaymentMethodImageUrl,
                            CreationDate = x.CreatedOn,
                        }
                    }).ToListAsync());
                }

                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<General<Guid>> GetPackages()
        {
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    return db.Packages.Where(x => x.IsEnabled).Select(x => new General<Guid>
                    {
                        Id = x.Id,
                        Name = x.Name
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public EGiftDetail GetEGiftDetail(Guid id)
        {
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    return db.UserCardMaster
                        .Where(x => x.Id.Equals(id))
                    .Select(x => new EGiftDetail
                    {
                        Id = x.Id,
                        IsSendToNonUser = x.UserCardDetails.FirstOrDefault(y => y.IsEnabled).SentToId == null ? true : false,
                        IsGroup = x.IsSentInGroup,
                        SentToIds = x.UserCardDetails.Where(y => y.IsEnabled).Select(y => y.SentToId).ToList(),
                        SentToNames = x.UserCardDetails.Where(y => y.IsEnabled).Select(y => y.SentToId == null ? y.SentToName + "(" + y.SentToPhoneNumber + ")" : y.SentTo.UserProfiles.Where(z => z.IsEnabled).Select(z => z.FirstName + " " + z.LastName + "(" + z.User.PhoneNumber + ")").FirstOrDefault()).ToList(),
                        SentFromId = x.SentFromId,
                        SentFromName = x.SentFrom.UserProfiles.Where(y => y.IsEnabled).Select(y => y.FirstName + " " + y.LastName + "(" + y.User.PhoneNumber + ")").FirstOrDefault(),
                        EGifterId = (x.UserCardDetails.FirstOrDefault(y => y.IsEnabled).IseGiftAttached ?? false) ? x.UserCardDetails.FirstOrDefault(y => y.IsEnabled).UserCardDetaileGifts.FirstOrDefault(y => y.IsEnabled).OrderId : null,
                        CardName = (x.UserCardDetails.FirstOrDefault(y => y.IsEnabled).IseGiftAttached ?? false) ? x.UserCardDetails.FirstOrDefault(y => y.IsEnabled).UserCardDetaileGifts.Where(y => y.IsEnabled).Select(y => y.UserCardDetaileGiftLineItems.FirstOrDefault(z => z.IsEnabled).BrandName).FirstOrDefault() : null,
                        Price = (x.UserCardDetails.FirstOrDefault(y => y.IsEnabled).IseGiftAttached ?? false) ? x.UserCardDetails.FirstOrDefault(x => x.IsEnabled).UserCardDetaileGifts.Where(y => y.IsEnabled).Select(y => y.UserCardDetaileGiftLineItems.FirstOrDefault(z => z.IsEnabled).ValueAmount).FirstOrDefault() : null,
                        PaymentMethod = (x.UserCardDetails.FirstOrDefault(y => y.IsEnabled).IseGiftAttached ?? false) ? x.UserCardDetails.FirstOrDefault(y => y.IsEnabled).PaymentMethod.PaymentMethodType.Name : null,
                        PaymentDetail = (x.UserCardDetails.FirstOrDefault(y => y.IsEnabled).IseGiftAttached ?? false) ? x.UserCardDetails.FirstOrDefault(y => y.IsEnabled).PaymentMethod.Email ?? x.UserCardDetails.FirstOrDefault(y => y.IsEnabled).PaymentMethod.CardMaskedNumber : null,
                        CardType = (x.UserCardDetails.FirstOrDefault(y => y.IsEnabled).IseGiftAttached ?? false) ? x.UserCardDetails.FirstOrDefault(y => y.IsEnabled).PaymentMethod.CardType ?? "" : null,
                        Expiry = (x.UserCardDetails.FirstOrDefault(y => y.IsEnabled).IseGiftAttached ?? false) ? x.UserCardDetails.FirstOrDefault(y => y.IsEnabled).PaymentMethod.ExpiryDate ?? "" : null,
                        SendOn = x.ScheduledDate.ToString("dd/MMM/yyyy hh:mm:ss a"),
                        CreatedOn = x.CreatedOn.ToString("dd/MMM/yyyy hh:mm:ss a"),
                    }).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
