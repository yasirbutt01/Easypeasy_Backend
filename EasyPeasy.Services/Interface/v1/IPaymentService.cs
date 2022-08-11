using EasyPeasy.DataViewModels.Requests.v1;
using EasyPeasy.DataViewModels.Response.v1;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;

namespace EasyPeasy.Services.Interface.v1
{
    public interface IPaymentService
    {
        Task<PaymentSubscriptionResponse> GetSubcriptions(Guid userId);
        Task<bool> DeletePaymentMethod(Guid paymentMethodId, Guid userId);
        Task<bool> PaymentMethodMakeDefault(Guid paymentMethodId, Guid userId);
        Task<bool> CancelSubscription(Guid subscriptionId, Guid userId);
        Task<bool> BrainTreeWebHook(IFormCollection formCollection);
        Task<Tuple<AccountUserPackageViewModel, string>> Subscribe(PaymentSubscribeNowRequest request, Guid userId);
        Task<PaymentMethodResponse> GetPaymentMethods(Guid userId);
        Task<Tuple<PaymentAddPaymentMethodResponse, string>> AddPaymentMethod(PaymentAddPaymentMethodRequest request, Guid userId);
        Task<string> GenerateBrainTreeTokenAsync();
        Task<string> SubscribePaypal(Guid packageId, Guid userId);
        Task<Tuple<AccountUserPackageViewModel, string>> SubscribeM(Guid packageId, Guid userId);
    }
}
