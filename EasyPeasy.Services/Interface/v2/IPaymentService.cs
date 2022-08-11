using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EasyPeasy.DataViewModels.Requests.v1;
using EasyPeasy.DataViewModels.Requests.v2;
using EasyPeasy.DataViewModels.Response.v1;
using EasyPeasy.DataViewModels.Response.v2;
using PaymentAddPaymentMethodResponse = EasyPeasy.DataViewModels.Response.admin.PaymentAddPaymentMethodResponse;
using PaymentSubscribeNowRequest = EasyPeasy.DataViewModels.Requests.v2.PaymentSubscribeNowRequest;

namespace EasyPeasy.Services.Interface.v2
{
    public interface IPaymentService
    {
        Task<DataViewModels.Response.v2.PaymentSubscriptionResponse> GetSubcriptions(Guid userId);
        Task<Tuple<PaymentVerifyCodeResponse, string>> VerifyCouponCode(PaymentVerifyCouponCodeRequest request,
            Guid userId);



        Task<Tuple<AccountUserPackageViewModel, string>> Subscribe(PaymentSubscribeNowRequest request, Guid userId);
    }
}
