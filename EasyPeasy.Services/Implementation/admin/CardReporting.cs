using EasyPeasy.Data.Context;
using EasyPeasy.DataViewModels.Response.admin;
using EasyPeasy.Services.Interface.admin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EasyPeasy.Services.Implementation.admin
{
    public class CardReporting : ICardReporting
    {
        public GenralListResponse<SendingCard> GetSendingCards(GenralListResponse<SendingCard> page, DateTime? str, DateTime? endd)
        {
            DateTime strt = Convert.ToDateTime(str);
            DateTime end = Convert.ToDateTime(endd).AddDays(1);
            GenralListResponse<SendingCard> data = new GenralListResponse<SendingCard>();

            using (var db = new EasypeasyDbContext())
            {
                var query = db.UserCardMaster.Where(x => x.CreatedOn >= strt && x.CreatedOn <= end)
               .Select(x => new SendingCard
               {
                   Id = x.Id,
                   IsSendToNonUser = x.UserCardDetails.FirstOrDefault(y => y.IsEnabled).SentToId == null ? true : false,
                   IsGroup = x.IsSentInGroup,
                   SentToIds = x.UserCardDetails.Where(y => y.IsEnabled).Select(y => y.SentToId).ToList(),
                   SentToNames = x.UserCardDetails.Where(y => y.IsEnabled).Select(y => y.SentToId == null ? y.SentToName + "(" + y.SentToPhoneNumber + " - NonApp User)" : y.SentTo.UserProfiles.Where(z => z.IsEnabled).Select(z => z.FirstName + " " + z.LastName + "(" + z.User.PhoneNumber + " - App User)").FirstOrDefault()).ToList(),
                   SentFromId = x.SentFromId,
                   SentFromName = x.SentFrom.UserProfiles.Where(y => y.IsEnabled).Select(y => y.FirstName + " " + y.LastName + "(" + y.User.PhoneNumber + ")").FirstOrDefault(),
                   EGifterId = (x.UserCardDetails.FirstOrDefault(y => y.IsEnabled).IseGiftAttached ?? false) ? x.UserCardDetails.FirstOrDefault(y => y.IsEnabled).UserCardDetaileGifts.FirstOrDefault(y => y.IsEnabled).OrderId : null,
                   CardName = (x.UserCardDetails.FirstOrDefault(y => y.IsEnabled).IseGiftAttached ?? false) ? x.UserCardDetails.FirstOrDefault(y => y.IsEnabled).UserCardDetaileGifts.Where(y => y.IsEnabled).Select(y => y.UserCardDetaileGiftLineItems.FirstOrDefault(z => z.IsEnabled).BrandName).FirstOrDefault() : null,
                   Price = (x.UserCardDetails.FirstOrDefault(y => y.IsEnabled).IseGiftAttached ?? false) ? x.UserCardDetails.FirstOrDefault(x => x.IsEnabled).UserCardDetaileGifts.Where(y => y.IsEnabled).Select(y => y.UserCardDetaileGiftLineItems.FirstOrDefault(z => z.IsEnabled).ValueAmount).FirstOrDefault() : null,
                   PaymentMethod = (x.UserCardDetails.FirstOrDefault(y => y.IsEnabled).IseGiftAttached ?? false) ? x.UserCardDetails.FirstOrDefault(y => y.IsEnabled).PaymentMethod.PaymentMethodType.Name : null,
                   PaymentDetail = (x.UserCardDetails.FirstOrDefault(y => y.IsEnabled).IseGiftAttached ?? false) ? x.UserCardDetails.FirstOrDefault(y => y.IsEnabled).PaymentMethod.Email ?? x.UserCardDetails.FirstOrDefault(y => y.IsEnabled).PaymentMethod.CardMaskedNumber : null,
                   CardType = (x.UserCardDetails.FirstOrDefault(y => y.IsEnabled).IseGiftAttached ?? false) ? x.UserCardDetails.FirstOrDefault(y => y.IsEnabled).PaymentMethod.CardType ?? "" : null,
                   Expiry = (x.UserCardDetails.FirstOrDefault(y => y.IsEnabled).IseGiftAttached ?? false) ? x.UserCardDetails.FirstOrDefault(y => y.IsEnabled).PaymentMethod.ExpiryDate ?? "" : null,
                   SendOn = (x.IsCardSent ?? false) ? (DateTime?)null : x.ScheduledDate,
                   CreatedOn = x.CreatedOn,
               }).AsQueryable();

                if (!string.IsNullOrEmpty(page.Search))
                {
                    var date = new DateTime();
                    var sdate = DateTime.TryParse(page.Search, out date);
                    int totalCases = -1;
                    var isNumber = Int32.TryParse(page.Search, out totalCases);
                    if (sdate)
                    {
                        query = query.Where(
                            x => x.CreatedOn.Date == date.Date
                            || x.SendOn == date
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
                        x => x.SentFromName.ToLower().Contains(page.Search.ToLower())
                        || x.CardName.ToLower().Contains(page.Search.ToLower())
                        || x.EGifterId.ToLower().Contains(page.Search.ToLower())
                        || x.PaymentDetail.ToLower().Contains(page.Search.ToLower())
                        || x.PaymentMethod.ToLower().Contains(page.Search.ToLower())
                        || x.CardType.ToLower().Contains(page.Search.ToLower())
                        || x.Price.ToString().ToLower().Contains(page.Search.ToLower())
                        || x.CardName.ToLower().Contains(page.Search.ToLower())

                        );
                    }
                }
                var orderedQuery = query.OrderByDescending(x => x.CreatedOn);
                switch (page.SortIndex)
                {
                    case 0:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.CreatedOn) : query.OrderBy(x => x.CreatedOn);
                        break;
                    case 1:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.SendOn) : query.OrderBy(x => x.SendOn);
                        break;
                    case 2:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.IsSendToNonUser) : query.OrderBy(x => x.IsSendToNonUser);
                        break;
                    case 3:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.IsGroup) : query.OrderBy(x => x.IsGroup);
                        break;
                    case 4:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.SentFromName) : query.OrderBy(x => x.SentFromName);
                        break;
                    case 5:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.CardName) : query.OrderBy(x => x.CardName);
                        break;
                    case 6:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.Price) : query.OrderBy(x => x.Price);
                        break;
                    case 7:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.PaymentMethod) : query.OrderBy(x => x.PaymentMethod);
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
    }
}
