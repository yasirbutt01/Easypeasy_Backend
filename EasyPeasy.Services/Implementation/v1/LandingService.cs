
using EasyPeasy.Data.Context;
using EasyPeasy.Data.DTOs;
using EasyPeasy.DataViewModels.Requests.v1;
using EasyPeasy.DataViewModels.Response.v1;
using EasyPeasy.Services.Interface.v1;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EasyPeasy.Services.Implementation.v1
{
    public class LandingService : ILandingService
    {
        public async Task<bool> ClearAllSearches(Guid userId)
        {
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    db.UserRecentSearches.Where(x => x.IsEnabled && x.UserId == userId).ToList().ForEach(x =>
                    {
                        x.IsEnabled = false;
                        x.DeletedBy = userId.ToString();
                        x.DeletedOn = DateTime.UtcNow;
                    });

                    await db.SaveChangesAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<List<string>> GetRecentSearches(Guid userId)
        {
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    var result = await db.UserRecentSearches.Where(x => x.IsEnabled && x.UserId == userId)
                        .Select(x => x.Keyword).ToListAsync();
                    return result;
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<LandingCategoryResponse> GetCategoryById(Guid categoryId)
        {
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    var x = await db.Categories.FirstOrDefaultAsync(x => x.Id == categoryId);
                    var response = new LandingCategoryResponse()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        GreetingTypeId = x.GreetingTypeId,
                        TotalCards = x.CardCategories.Count(y => y.IsEnabled && y.Card.IsEnabled),
                        GifFileUrl = x.GifFileUrl,
                        IconFileThumbnailUrl = x.IconFileThumbnailUrl,
                        IconFileUrl = x.IconFileUrl,
                        JsonFileUrl = x.JsonFileUrl,
                        WebActiveIconFileThumbnailUrl = x.WebActiveIconFileThumbnailUrl,
                        WebActiveIconFileUrl = x.WebActiveIconFileUrl,
                        WebInactiveIconFileThumbnailUrl = x.WebInactiveIconFileThumbnailUrl,
                        WebInactiveIconFileUrl = x.WebInactiveIconFileUrl,
                        MusicList = x.CategoryMusicFiles.Where(y => y.IsEnabled).OrderBy(z => z.CreatedOn).Select(y => new LandingCardMusicResponse
                        {
                            Id = y.Id,
                            CategoryId = x.Id,
                            Mp3FileUrl = y.Mp3FileUrl,
                            Name = y.Name,
                            WavFileUrl = y.WavFileUrl
                        }).ToList()
                    };

                    return response;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<LandingCategoriesWithCardResponse> GetCategoriesWithCards(LandingGetCategoriesWithCardsRequest request, Guid userId)
        {
            try
            {
                var response = new LandingCategoriesWithCardResponse();
                using (var db = new EasypeasyDbContext())
                {

                    request.CardsCount ??= 6;
                    if (userId != Guid.Empty)
                    {
                        if (!string.IsNullOrWhiteSpace(request.Search))
                        {
                            db.UserRecentSearches.Where(x => x.IsEnabled && x.UserId == userId && x.Keyword.ToLower() == request.Search.ToLower()).ToList().ForEach(x =>
                            {
                                x.IsEnabled = false;
                                x.DeletedBy = userId.ToString();
                                x.DeletedOn = DateTime.UtcNow;
                            });

                            var userSearch = new UserRecentSearches()
                            {
                                Id = Guid.NewGuid(),
                                UserId = userId,
                                CreatedBy = userId.ToString(),
                                CreatedOn = DateTime.UtcNow,
                                CreatedOnDate = DateTime.UtcNow,
                                IsEnabled = true,
                                Keyword = request.Search
                            };

                            await db.UserRecentSearches.AddAsync(userSearch);
                            await db.SaveChangesAsync();
                        }
                    }

                    var nowDate = DateTime.UtcNow;
                    response.TotalCategories = await db.Categories.CountAsync(x => x.IsEnabled && x.CardCategories.Count(z => z.IsEnabled) != 0);
                    response.Categories = db.Categories.Where(x =>
                            x.IsEnabled && x.CardCategories.Count(z => z.IsEnabled) != 0 &&
                            (string.IsNullOrWhiteSpace(request.Search) || x.CardCategories.Any(y =>
                                y.Category.Name.ToLower().Contains(request.Search.ToLower()))))
                        .Select(x => new LandingCategoryResponse
                        {
                            Id = x.Id,
                            Name = x.Name,
                            EventDate = x.EventDate,
                            GreetingTypeId = x.GreetingTypeId,
                            TotalCards = x.CardCategories.Count(y => y.IsEnabled && y.Card.IsEnabled),
                            GifFileUrl = x.GifFileUrl,
                            IconFileThumbnailUrl = x.IconFileThumbnailUrl,
                            IconFileUrl = x.IconFileUrl,
                            JsonFileUrl = x.JsonFileUrl,
                            WebActiveIconFileThumbnailUrl = x.WebActiveIconFileThumbnailUrl,  
                            WebActiveIconFileUrl = x.WebActiveIconFileUrl,
                            WebInactiveIconFileThumbnailUrl = x.WebInactiveIconFileThumbnailUrl,
                            WebInactiveIconFileUrl = x.WebInactiveIconFileUrl,
                            Cards = x.CardCategories.Where(y => y.IsEnabled && y.Card.IsEnabled)
                                .OrderByDescending(z => z.Card.CreatedOn).Select(y => new LandingCardResponse
                                {
                                    Id = y.Card.Id,
                                    CategoryId = x.Id,
                                    CardFileThumbnailUrl = y.Card.CardFileThumbnailUrl,
                                    CardFileUrl = y.Card.CardFileUrl,
                                    InsideContent = y.Card.InsideContent,
                                    IsEgiftEnabled = y.Card.IsEgiftEnabled
                                }).Take((int) request.CardsCount).ToList(),
                            MusicList = x.CategoryMusicFiles.Where(y => y.IsEnabled).OrderBy(z => z.CreatedOn).Select(
                                y => new LandingCardMusicResponse
                                {
                                    Id = y.Id,
                                    CategoryId = x.Id,
                                    Mp3FileUrl = y.Mp3FileUrl,
                                    Name = y.Name,
                                    WavFileUrl = y.WavFileUrl
                                }).ToList()
                        }).ToList();

                    foreach (var x in response.Categories)
                    {
                        if (x.EventDate == null)
                        {
                            x.Nearest = 99999999;
                        }
                        else
                        {
                            var eventDate = x.EventDate;
                            x.Nearest = (new DateTime(x.EventDate.Value.Year, x.EventDate.Value.Month,
                                             x.EventDate.Value.Day) -
                                         new DateTime(x.EventDate.Value.Year, nowDate.Month, nowDate.Day)).Days;
                            while (x.Nearest < 0)
                            {
                                eventDate = eventDate.Value.AddYears(1);
                                x.Nearest = (new DateTime(eventDate.Value.Year, eventDate.Value.Month,
                                                 eventDate.Value.Day) -
                                             new DateTime(nowDate.Year, nowDate.Month, nowDate.Day)).Days;
                            }
                        }
                    }

                    var priorCards = new[] { "Word Art Card" };
                    var priorCards2 = new[] { "Birthday" };

                    response.Categories = response.Categories
                        .OrderByDescending(x => x.GreetingTypeId)
                        .ThenByDescending(c => priorCards.Contains(c.Name))
                        .ThenByDescending(c => priorCards2.Contains(c.Name))
                        .ThenBy(x => x.Nearest).ThenBy(x => x.Name)
                        .Skip(request.Skip).Take(request.Take).ToList();
                }
                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<LandingGetCardResponse> GetCards(int skip, int take, Guid categoryId)
        {
            try
            {
                var response = new LandingGetCardResponse();
                using (var db = new EasypeasyDbContext())
                {
                    response.TotalCards = await db.CardCategories.CountAsync(x => x.IsEnabled && x.CategoryId == categoryId && x.Card.IsEnabled);
                    response.Cards = await db.CardCategories.Where(y => y.IsEnabled && y.CategoryId == categoryId && y.Card.IsEnabled).OrderByDescending(z => z.Card.CreatedOn).Select(y => new LandingCardResponse
                    {
                        Id = y.Card.Id,
                        CategoryId = categoryId,
                        CardFileThumbnailUrl = y.Card.CardFileThumbnailUrl,
                        CardFileUrl = y.Card.CardFileUrl,
                        InsideContent = y.Card.InsideContent,
                        IsEgiftEnabled = y.Card.IsEgiftEnabled
                    }).Skip(skip).Take(take).ToListAsync();
                }
                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}

