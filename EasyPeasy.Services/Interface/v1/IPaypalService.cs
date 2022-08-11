using EasyPeasy.DataViewModels.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EasyPeasy.Services.Interface.v1
{
    public interface IPaypalService
    {
        Task<PaypalSubscriptionResponse> CreatePaypalSubscriptionAsync(string planId, string givenName, string surName, string email, DateTime creationTime);
    }
}
