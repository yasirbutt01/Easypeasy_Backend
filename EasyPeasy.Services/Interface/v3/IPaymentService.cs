using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EasyPeasy.DataViewModels.Requests.v3;
using EasyPeasy.DataViewModels.Response.v3;

namespace EasyPeasy.Services.Interface.v3
{
    public interface IPaymentService
    {
        Task<Tuple<PaymentVerifyPaymentMethodResponse, string>> VerifyPaymentMethod(
            PaymentVerifyPaymentMethodRequest request, Guid userId);
        Task<Tuple<PaymentAddPaymentMethodResponse, string>> AddPaymentMethod(PaymentAddPaymentMethodRequest request, Guid userId);

        Task<bool> DeletePaymentMethod(Guid paymentMethodId, Guid userId);

    }
}
