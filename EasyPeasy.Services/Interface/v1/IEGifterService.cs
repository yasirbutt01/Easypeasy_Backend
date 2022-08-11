using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using EasyPeasy.DataViewModels.Common;
using EasyPeasy.DataViewModels.Response.v1;
using Newtonsoft.Json;

namespace EasyPeasy.Services.Interface.v1
{
    public interface IEGifterService
    {
        Task<EGifterGetProductsResponse> GetProductsFromDb(int pageIndex, int pageSize, string productName,
            string productDescription);

        Task<bool> SyncProducts();
        Task<EGifterGetProductsResponse> GetProducts(int pageIndex, int pageSize, string productName,
            string productDescription);

        Task<EGifterCreateOrderResponse> CreateOrder(string orderPoNumber, string productId, double cardValue,
            string lineItemExternalId, string toName, string fromName);

        Task<EGifterCreateOrderResponse> GetParticularOrder(string orderId);
    }
}
