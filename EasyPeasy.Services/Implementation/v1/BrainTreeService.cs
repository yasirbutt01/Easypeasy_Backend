using Braintree;
using Braintree.Exceptions;
using EasyPeasy.Data.Context;
using EasyPeasy.Data.DTOs;
using EasyPeasy.Services.Interface.v1;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyPeasy.Services.Implementation.v1
{
    public class BrainTreeService : IBrainTreeService
    {
        private IBraintreeGateway gateway;
        public BrainTreeService(IConfiguration configuration)
        {
            //var environment = (Braintree.Environment)Enum.Parse(typeof(Braintree.Environment), configuration["Braintree:Environment"].ToString());
            var Environment = configuration["Braintree:Environment"];
            var MerchantId = configuration["Braintree:MerchantId"];
            var PublicKey = configuration["Braintree:PublicKey"];
            var PrivateKey = configuration["Braintree:PrivateKey"];
            gateway = new BraintreeGateway(Environment, MerchantId, PublicKey, PrivateKey);
        }

        public WebhookNotification WebHookNotificationParse(IFormCollection formCollection)
        {
            return gateway.WebhookNotification.Parse(
                        formCollection["bt_signature"],
                        formCollection["bt_payload"]
                    );
        }

        public WebhookNotification WebHookNotificationParse(Dictionary<string, string> sample)
        {
            return gateway.WebhookNotification.Parse(
                        sample["bt_signature"],
                        sample["bt_payload"]
                    );
        }

        public Dictionary<string, string> SampleNotification()
        {
            return gateway.WebhookTesting.SampleNotification(
      WebhookKind.SUBSCRIPTION_WENT_PAST_DUE, "my_id"
  );
        }

        public Result<Customer> CreateCustomerIfDoesntExist(Users user)
        {
            var getProfile = user.UserProfiles.FirstOrDefault(x => x.IsEnabled);
            var result = gateway.Customer.Create(new CustomerRequest
            {
                Id = user.Id.ToString(),
                FirstName = getProfile.FirstName,
                LastName = getProfile.LastName,
                Email = getProfile.Email,
                Phone = user.PhoneNumber
            });
            return result;
        }

        public async Task<Result<PaymentMethod>> CreatePaymentMethod(string paymentMethodNonce, Guid userId)
        {
            using (var db = new EasypeasyDbContext())
            {
                var getUser = await db.Users.Include(x => x.UserProfiles).FirstOrDefaultAsync(x => x.Id == userId);
                if (!DoesCustomerExist(userId.ToString()))
                {
                    CreateCustomerIfDoesntExist(getUser);
                }
                var result = gateway.PaymentMethod.Create(new PaymentMethodRequest
                {
                    CustomerId = userId.ToString(),
                    PaymentMethodNonce = paymentMethodNonce,
                    Options = new PaymentMethodOptionsRequest
                    {
                        VerifyCard = true
                    }
                });
                return result;
            }
        }

        public bool DeletePaymentMethod(string customerId, string token)
        {
            if (DoesCustomerHavePaymentMethod(customerId, token))
            {
                gateway.PaymentMethod.Delete(token);
                return true;
            }

            return false;
        }

        public Result<Subscription> CancelSubscription(string subscriptionId)
        {
            var result = gateway.Subscription.Cancel(subscriptionId);
            return result;
        }

        public Result<Subscription> CreateSubscription(string planId, string token, decimal discount)
        {
            var request = new SubscriptionRequest
            {
                PaymentMethodToken = token,
                PlanId = planId,
                Options = new SubscriptionOptionsRequest
                {
                    StartImmediately = true
                }
            };

            if (discount > 0)
            {
                request.Discounts = new DiscountsRequest
                {
                    Add = new AddDiscountRequest[]
                    {
                        new AddDiscountRequest
                        {
                            InheritedFromId = "s9vm",
                            Amount = discount
                        }
                    }
                };
            }
            var result = gateway.Subscription.Create(request);
            return result;
        }

        public Result<Transaction> CreateTransaction(string customerid, decimal amount, string orderId, string token, string description)
        {
            var request = new TransactionRequest
            {
                Amount = amount,
                CustomerId = customerid,
                PaymentMethodToken = token,
                Options = new TransactionOptionsRequest
                {
                    SubmitForSettlement = true
                },
                OrderId = orderId,
                CustomFields = new Dictionary<string, string>()
                {
                    { "description", description }
                }
            };

            var result = gateway.Transaction.Sale(request);

            return result;
        }

        public Transaction FindTransaction(string transactionId)
        {

            var result = gateway.Transaction.Find(transactionId);

            return result;
        }

        public Result<Transaction> RefundTransaction(string transactionId)
        {

            var result = gateway.Transaction.Refund(transactionId);

            return result;
        }

        public Result<Transaction> VoidTransaction(string transactionId)
        {

            var result = gateway.Transaction.Void(transactionId);

            return result;
        }

        public bool PaymentMethodMakeDefault(string customerId, string token)
        {
            if (DoesCustomerHavePaymentMethod(customerId, token))
            {
                var updateRequest = new PaymentMethodRequest
                {
                    Options = new PaymentMethodOptionsRequest
                    {
                        MakeDefault = true,
                        VerifyCard = false
                    }
                };

                var res = gateway.PaymentMethod.Update(token, updateRequest);

                return true;
            }

            return false;
        }

        public PaymentMethod FindPaymentMethod(string token)
        {
            try
            {
                var paymentMethod = gateway.PaymentMethod.Find(token);
                return paymentMethod;
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public Customer FindCustomerId(string customerId)
        {
            try
            {
                var customer = gateway.Customer.Find(customerId);
                return customer;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private bool DoesCustomerHavePaymentMethod(string customerId, string token)
        {
            try
            {
                var paymentMethod = gateway.PaymentMethod.Find(token);
                return paymentMethod.CustomerId == customerId;
            }
            catch (NotFoundException)
            {
                return true;
            }
        }


        private bool DoesCustomerExist(string customerId)
        {
            try
            {
                gateway.Customer.Find(customerId);
                return true;
            }
            catch (NotFoundException)
            {
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GenerateBrainTreeTokenAsync()
        {
            var clientToken = await gateway.ClientToken.GenerateAsync();
            return clientToken;
        }
    }
}
