using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using EasyPeasy.Data.Context;
using EasyPeasy.Data.DTOs;
using EasyPeasy.DataViewModels.Common;
using EasyPeasy.DataViewModels.Requests.v1;
using EasyPeasy.DataViewModels.Response.v1;
using EasyPeasy.Services.Interface.v1;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EasyPeasy.Services.Implementation.v1
{
    public class EGifterService : IEGifterService
    {
        private readonly IConfiguration configuration;
        private readonly string BaseUrl;
        private readonly string AccessToken;
        private readonly string Email;
        private readonly string WebhookUrl;

        public EGifterService(IConfiguration configuration)
        {
            BaseUrl = configuration.GetValue<string>("EGifter:BaseUrl");
            AccessToken = configuration.GetValue<string>("EGifter:AccessToken");
            Email = configuration.GetValue<string>("EGifter:Email");
            WebhookUrl = configuration.GetValue<string>("EGifter:WebhookUrl");
        }

        public async Task<EGifterGetProductsResponse> GetProducts(int pageIndex, int pageSize, string productName, string productDescription)
        {
            var http = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(90),
            };

            var accessToken = await GetEGiftyAccessTokenAsync(http, false);
            var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/Products?pageIndex={pageIndex}&productName={productName}&productDescription={productDescription}&productType=Digital&pageSize={pageSize}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.value);

            var response = await http.SendAsync(request);

            string content = await response.Content.ReadAsStringAsync();
            var responseModel = JsonConvert.DeserializeObject<EGifterGetProductsResponse>(content);
            return responseModel;
        }

        public async Task<EGifterGetProductsResponse> GetProductsFromDb(int pageIndex, int pageSize, string productName, string productDescription)
        {
            using (var db = new EasypeasyDbContext())
            {
                var count = await db.EgifterProducts.CountAsync(x => x.IsEnabled);
                if (count == 0)
                {
                    await SyncProducts();
                }

                var response = new EGifterGetProductsResponse();
                response.Page = pageIndex;
                response.PageSize = pageSize;
                response.TotalCount = await db.EgifterProducts.CountAsync(x => x.IsEnabled);



                response.Data = await db.EgifterProducts.Where(x => x.IsEnabled && (string.IsNullOrWhiteSpace(productName) || x.Name.Contains(productName) && (string.IsNullOrWhiteSpace(productDescription) || x.ShortDescription.Contains(productDescription)))).OrderBy(z => z.Name).Select(x => new EGifterGetProductDatum()
                {
                    Id = x.ProductId,
                    Meta = new EGifterGetProductMeta()
                    {
                        Colors = new EGifterGetProductColors()
                        {
                            Primary = x.PrimaryColor,
                            Secondary = x.SecondaryColor,
                            PrimaryText = x.PrimaryText,
                            SecondaryText = x.SecondaryText,
                        },
                        SupportsApiBalanceChecks = x.SupportsApiBalanceChecks ?? false,
                        Website = x.Website
                    },
                    Media = new EGifterGetProductMedia()
                    {
                        Logo = x.Logo,
                        Faceplates = x.EgifterProductFaceplates.Select(y => new EGifterGetProductFaceplate()
                        {
                            Name = y.Name,
                            Path = y.Path
                        }).ToList()
                    },
                    Name = x.Name,
                    DenominationType = x.DenominationType,
                    Disclaimer = x.Disclaimer,
                    Denominations = x.EgifterProductDenominations.OrderBy(x => x.Denomination).Select(y => y.Denomination ?? 0).ToList(),
                    LongDescription = x.LongDescription,
                    RedemptionNote = x.RedemptionNote,
                    ShortDescription = x.ShortDescription,
                    Terms = x.Terms,
                    Type = x.Type
                }).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();

                return response;

            }
        }

        public async Task<bool> SyncProducts()
        {
            using (var db = new EasypeasyDbContext())
            {
                var response = await GetProducts(1, 500, "", "");

                var getExistingDenominations = await db.EgifterProductDenominations.Where(x => x.IsEnabled).ToListAsync();
                getExistingDenominations.ForEach(x =>
                {
                    x.IsEnabled = false;
                    x.DeletedBy = "";
                    x.DeletedOn = DateTime.UtcNow;
                });
                var getExistingFacePlates = await db.EgifterProductFaceplates.Where(x => x.IsEnabled).ToListAsync();
                getExistingFacePlates.ForEach(x =>
                {
                    x.IsEnabled = false;
                    x.DeletedBy = "";
                    x.DeletedOn = DateTime.UtcNow;
                });
                var getExistingProducts = await db.EgifterProducts.Where(x => x.IsEnabled).ToListAsync();
                getExistingProducts.ForEach(x =>
                {
                    x.IsEnabled = false;
                    x.DeletedBy = "";
                    x.DeletedOn = DateTime.UtcNow;
                });
                await db.SaveChangesAsync();

                var productList = new List<EgifterProducts>();

                foreach (var x in response.Data)
                {
                    var uuid = Guid.NewGuid();
                    var item = new EgifterProducts()
                    {
                        Id = uuid,
                        ProductId = x.Id,
                        CreatedBy = "",
                        CreatedOn = DateTime.UtcNow,
                        CreatedOnDate = DateTime.UtcNow,
                        DenominationType = x.DenominationType,
                        Disclaimer = x.Disclaimer,
                        IsEnabled = true,
                        Logo = x.Media?.Logo,
                        LongDescription = x.LongDescription,
                        Name = x.Name,
                        PrimaryColor = x.Meta?.Colors?.Primary,
                        SecondaryColor = x.Meta?.Colors?.Secondary,
                        PrimaryText = x.Meta?.Colors?.PrimaryText,
                        SecondaryText = x.Meta?.Colors?.SecondaryText,
                        RedemptionNote = x.RedemptionNote,
                        ShortDescription = x.ShortDescription,
                        Terms = x.Terms,
                        SupportsApiBalanceChecks = x.Meta?.SupportsApiBalanceChecks,
                        Type = x.Type,
                        Website = x.Meta?.Website,
                        EgifterProductDenominations = x.Denominations.Select(y => new EgifterProductDenominations()
                        {
                            Id = Guid.NewGuid(),
                            CreatedBy = "",
                            CreatedOn = DateTime.UtcNow,
                            CreatedOnDate = DateTime.UtcNow,
                            IsEnabled = true,
                            Denomination = y,
                            EgifterProductId = uuid
                        }).ToList(),
                        EgifterProductFaceplates = x.Media?.Faceplates?.Select(y => new EgifterProductFaceplates()
                        {
                            Id = Guid.NewGuid(),
                            CreatedBy = "",
                            CreatedOn = DateTime.UtcNow,
                            CreatedOnDate = DateTime.UtcNow,
                            IsEnabled = true,
                            EgifterProductId = uuid,
                            Name = y.Name,
                            Path = y.Path
                        }).ToList()
                    };
                    productList.Add(item);
                }

                await db.EgifterProducts.AddRangeAsync(productList);
                await db.SaveChangesAsync();
            }
            return true;
        }

        public async Task<EGifterCreateOrderResponse> CreateOrder(string orderPoNumber, string productId, double cardValue, string lineItemExternalId, string toName, string fromName)
        {
            var http = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(30),
            };

            var accessToken = await GetEGiftyAccessTokenAsync(http, false);
            var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/Orders");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.value);

            var requestModel = JObject.FromObject(new EGifterCreateOrderRequest
            {
                Type = "Links",
                PoNumber = orderPoNumber,
                LineItems = new List<EGifterCreateOrderLineItem>(){
                    new EGifterCreateOrderLineItem
                    {
                            ProductId = productId,
                            Value = cardValue.ToString(),
                            Quantity = "1",
                            ExternalId = lineItemExternalId,
                            Culture = "en-US",
                            Personalization = new EGifterCreateOrderPersonalization()
                            {
                                DeliveryDate = DateTime.UtcNow,
                                To = toName,
                                FromName = fromName
                            }
                    }
                }.ToList(),
                WebhookSettings = new EGifterCreateOrderWebhookSettings()
                {
                    FulfillmentComplete = WebhookUrl
                }
            });

            request.Content = new StringContent(JsonConvert.SerializeObject(requestModel), Encoding.UTF8, "application/json");

            var response = await http.SendAsync(request);

            var content = await response.Content.ReadAsStringAsync();
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                accessToken = await GetEGiftyAccessTokenAsync(http, true);
            }
            var responseModel = JsonConvert.DeserializeObject<EGifterCreateOrderResponse>(content);
            return responseModel;
        }

        public async Task<EGifterCreateOrderResponse> GetParticularOrder(string orderId)
        {
            var http = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(30),
            };

            var accessToken = await GetEGiftyAccessTokenAsync(http, false);
            var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/Orders/{orderId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.value);

            var response = await http.SendAsync(request);

            var content = await response.Content.ReadAsStringAsync();
            var responseModel = JsonConvert.DeserializeObject<EGifterCreateOrderResponse>(content);
            return responseModel;
        }

        private async Task<EGifterAccessTokenResponse> GetEGiftyAccessTokenAsync(HttpClient http, bool isNew)
        {
            using (var db = new EasypeasyDbContext())
            {
                var dateNow = DateTime.UtcNow;
                var getToken = await db.EgifterTokens.FirstOrDefaultAsync(x => x.IsEnabled && x.ExpiryDate >= dateNow);

                if (getToken != null && isNew == false)
                {
                    var token = new EGifterAccessTokenResponse
                    {
                        expiresIn = getToken.ExpiresIn ?? 0,
                        value = getToken.Token
                    };
                    return token;
                }

                var request = new HttpRequestMessage(HttpMethod.Post, "/v1/Tokens");
                request.Headers.Add("Email", Email);
                request.Headers.Add("AccessToken", AccessToken);

                var response = await http.SendAsync(request);

                var content = await response.Content.ReadAsStringAsync();
                var accessToken = JsonConvert.DeserializeObject<EGifterAccessTokenResponse>(content);

                var dbToken = new EgifterTokens()
                {
                    CreatedBy = "",
                    CreatedOn = DateTime.UtcNow,
                    CreatedOnDate = DateTime.UtcNow,
                    IsEnabled = true,
                    ExpiresIn = accessToken.expiresIn,
                    Id = Guid.NewGuid(),
                    ExpiryDate = DateTime.UtcNow.AddMinutes(accessToken.expiresIn-5),
                    Token = accessToken.value,
                };
                var getOldTokens = await db.EgifterTokens.Where(x => x.IsEnabled).ToListAsync();
                getOldTokens.ForEach(x =>
                {
                    x.IsEnabled = false;
                    x.DeletedBy = "";
                    x.DeletedOn = DateTime.UtcNow;
                });
                await db.SaveChangesAsync();
                await db.EgifterTokens.AddAsync(dbToken);
                await db.SaveChangesAsync();

                return accessToken;
            }
        }
    }
}
