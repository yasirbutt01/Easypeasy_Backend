using EasyPeasy.Common;
using EasyPeasy.Data.Context;
using EasyPeasy.Data.DTOs;
using EasyPeasy.DataViewModels.Requests.admin;
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
    public class CardService : ICardService
    {
        public GenralListResponse<GetCardResponse> GetCards(GenralListResponse<GetCardResponse> page, Guid? categoryId, DateTime? str, DateTime? endd)
        {
            DateTime strt = Convert.ToDateTime(str);
            DateTime end = Convert.ToDateTime(endd).AddDays(1);
            GenralListResponse<GetCardResponse> data = new GenralListResponse<GetCardResponse>();

            using (var db = new EasypeasyDbContext())
            {
                var query2 = db.Cards.AsQueryable();

                if (categoryId != null)
                    query2 = query2.Where(x => x.CardCategories.Any(y => y.CategoryId.Equals(categoryId) && y.IsEnabled));

                var query = query2.Where(x => x.CreatedOn >= strt && x.CreatedOn <= end)
               .Select(x => new GetCardResponse
               {
                   Id = x.Id,
                   StockId = x.TrackNumber,
                   CardImageUrl = x.CardFileUrl,
                   CardImageThumnailUrl = x.CardFileThumbnailUrl,
                   LastUpdatedDate = x.UpdatedOn ?? new DateTime(),
                   IsActive = x.IsEnabled,
                   Categories = x.CardCategories
                   .Where(y => y.IsEnabled)
                   .Select(y => new GetCardCategoryResponse
                   {
                       CategoryId = y.CategoryId,
                       Category = y.Category.Name,
                       Types = y.CardCategoryTypes
                       .Where(z => z.IsEnabled)
                       .Select(z => z.CategoryType.Name)
                       .ToList()
                   }).ToList(),
               }).AsQueryable();



                if (!string.IsNullOrEmpty(page.Search))
                {
                    var date = new DateTime();
                    var sdate = DateTime.TryParse(page.Search, out date);
                    int totalCases = -1;
                    var isNumber = Int32.TryParse(page.Search, out totalCases);
                    if (sdate)
                    {
                        //query = query.Where(
                        //    x => x.LastUpdatedDate.Date == date.Date
                        //);
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
                        x => x.StockId.ToLower().Contains(page.Search.ToLower()));
                    }
                }
                var orderedQuery = query.OrderByDescending(x => x.LastUpdatedDate);
                switch (page.SortIndex)
                {
                    case 0:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.StockId) : query.OrderBy(x => x.StockId);
                        break;
                    case 1:
                        orderedQuery = page.SortBy == "desc" ? query.OrderByDescending(x => x.LastUpdatedDate) : query.OrderBy(x => x.LastUpdatedDate);
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

        public async Task<bool> Save(SaveCardRequest request, string userId)
        {
            try
            {
                bool response = false;

                using (var db = new EasypeasyDbContext())
                {
                    using (var trans = db.Database.BeginTransaction())
                    {
                        try
                        {
                            Guid cardId = Guid.Empty.Equals(request.Id) ? SystemGlobal.GetId() : request.Id;

                            List<CardCategories> cardCategories = new List<CardCategories>();

                            if (Guid.Empty.Equals(request.Id))
                            {
                                await db.Cards.AddAsync(new Cards
                                {
                                    Id = cardId,
                                    TrackNumber = request.StockId,
                                    SearchKeywords = request.KeyWord,
                                    FrontContent = request.FrontContent,
                                    InsideContent = request.InsideContent,
                                    CardFileUrl = request.CardImageUrl,
                                    CardFileThumbnailUrl = request.CardImageThumbnailUrl,
                                    IsEgiftEnabled = request.EGiftCardEnable,
                                    IsEnabled = true,
                                    CreatedOnDate = DateTime.UtcNow,
                                    CreatedBy = userId,
                                    CreatedOn = DateTime.UtcNow,
                                    UpdatedOn = DateTime.UtcNow,
                                    UpdatedBy = userId,
                                });

                                await db.SaveChangesAsync();
                            }
                            else
                            {
                                var card = db.Cards.FirstOrDefault(x => x.Id.Equals(request.Id));

                                if (card == null) throw new ArgumentException("card id doesn't exist");

                                card.TrackNumber = request.StockId;
                                card.SearchKeywords = request.KeyWord;
                                card.FrontContent = request.FrontContent;
                                card.InsideContent = request.InsideContent;
                                card.CardFileUrl = request.CardImageUrl;
                                card.CardFileThumbnailUrl = request.CardImageThumbnailUrl;
                                card.IsEgiftEnabled = request.EGiftCardEnable;
                                card.UpdatedOn = DateTime.UtcNow;
                                card.UpdatedBy = userId;

                                db.Entry(card).State = EntityState.Modified;

                                await db.SaveChangesAsync();
                            }


                            if (request.Categories == null) request.Categories = new List<SaveCardCategory>();

                            //remove 
                            var alreadycardCategories = db.CardCategories.Where(x => x.CardId.Equals(cardId)).ToList();

                            foreach (var existingChild in alreadycardCategories)
                            {

                                if (!request.Categories.Any(c => c.CategoryId == existingChild.CategoryId))
                                {
                                    var cate = db.CardCategories.Find(existingChild.Id);

                                    cate.IsEnabled = false;
                                    cate.DeletedBy = userId;
                                    cate.DeletedOn = DateTime.UtcNow;

                                    db.Entry(cate).State = EntityState.Modified;
                                }
                            }
                            await db.SaveChangesAsync();

                            foreach (var category in request.Categories)
                            {
                                var existingCategory = db.CardCategories.FirstOrDefault(x => x.CategoryId.Equals(category.CategoryId) && x.CardId.Equals(cardId));

                                Guid cardCategoryId = existingCategory == null ? SystemGlobal.GetId() : existingCategory.Id;

                                if (existingCategory == null)
                                {
                                    await db.CardCategories.AddAsync(new CardCategories
                                    {
                                        Id = cardCategoryId,
                                        CardId = cardId,
                                        CategoryId = category.CategoryId,
                                        IsEnabled = true,
                                        CreatedOnDate = DateTime.UtcNow,
                                        CreatedBy = userId,
                                        CreatedOn = DateTime.UtcNow,
                                    });
                                    await db.SaveChangesAsync();
                                }
                                else
                                {
                                    existingCategory.CardId = cardId;
                                    existingCategory.CategoryId = category.CategoryId;
                                    existingCategory.UpdatedOn = DateTime.UtcNow;
                                    existingCategory.UpdatedBy = userId;

                                    await db.SaveChangesAsync();
                                }

                                if (category.Types == null) category.Types = new List<SaveCardCategoryType>();

                                var cardCategoryTypes = db.CardCategoryTypes.Where(x => x.IsEnabled && x.CardCategoryId.Equals(cardCategoryId)).ToList();

                                // Delete children
                                foreach (var existingChild in cardCategoryTypes)
                                {

                                    if (!category.Types.Any(c => c.CategoryTypeId == existingChild.CategoryTypeId))
                                    {
                                        var mapCategory = db.CardCategoryTypes.Find(existingChild.Id);

                                        mapCategory.IsEnabled = false;
                                        mapCategory.DeletedBy = userId;
                                        mapCategory.DeletedOn = DateTime.UtcNow;

                                        db.Entry(mapCategory).State = EntityState.Modified;
                                    }
                                }

                                //Insert children
                                foreach (var childModel in category.Types)
                                {
                                    var existingChild = cardCategoryTypes
                                        .Where(c => c.CategoryTypeId == childModel.CategoryTypeId)
                                        .SingleOrDefault();

                                    if (existingChild != null)
                                    {
                                        // Update child
                                        existingChild.CategoryTypeId = childModel.CategoryTypeId;
                                        existingChild.CardCategoryId = cardCategoryId;
                                        existingChild.UpdatedBy = userId;
                                        existingChild.UpdatedOn = DateTime.UtcNow;

                                        db.Entry(existingChild).State = EntityState.Modified;
                                    }
                                    else
                                    {
                                        // Insert child
                                        var newChild = new CardCategoryTypes
                                        {
                                            Id = SystemGlobal.GetId(),
                                            CategoryTypeId = childModel.CategoryTypeId,
                                            CardCategoryId = cardCategoryId,
                                            IsEnabled = true,
                                            CreatedBy = userId,
                                            CreatedOn = DateTime.UtcNow
                                        };

                                        db.CardCategoryTypes.Add(newChild);
                                    }
                                }

                                await db.SaveChangesAsync();
                            }

                            trans.Commit();
                            response = true;
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();
                            throw ex;
                        }
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public SaveCardRequest GetEdit(Guid id)
        {
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    return db.Cards
                        .Where(x => x.Id.Equals(id))
                        .Select(x => new SaveCardRequest
                        {
                            Id = x.Id,
                            CardImageUrl = x.CardFileUrl,
                            CardImageThumbnailUrl = x.CardFileThumbnailUrl,
                            FrontContent = x.FrontContent,
                            InsideContent = x.InsideContent,
                            StockId = x.TrackNumber,
                            KeyWord = x.SearchKeywords,
                            EGiftCardEnable = x.IsEgiftEnabled,
                            Categories = x.CardCategories
                            .Where(y => y.IsEnabled)
                            .Select(y => new SaveCardCategory
                            {
                                Id = y.Id,
                                CategoryId = y.CategoryId,
                                Types = y.CardCategoryTypes
                                .Where(z => z.IsEnabled)
                                .Select(z => new SaveCardCategoryType
                                {
                                    Id = z.Id,
                                    CategoryTypeId = z.CategoryTypeId
                                }).ToList()
                            }).ToList()
                        }).FirstOrDefault() ?? new SaveCardRequest();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> ControlActivation(Guid id, bool activation, string userId)
        {
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    var card = db.Cards.FirstOrDefault(x => x.Id.Equals(id));

                    if (card == null) throw new ArgumentException("id not found!");

                    card.IsEnabled = activation;
                    card.UpdatedOn = DateTime.UtcNow;
                    card.UpdatedBy = userId;

                    db.Entry(card).State = EntityState.Modified;
                    await db.SaveChangesAsync();

                    return true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
