using Braintree;
using EasyPeasy.Data.DTOs;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;

namespace EasyPeasy.Services.Interface.v1
{
    public interface IBrainTreeService
    {
        Result<Subscription> CancelSubscription(string subscriptionId);
        WebhookNotification WebHookNotificationParse(Dictionary<string, string> sample);
        Dictionary<string, string> SampleNotification();
        WebhookNotification WebHookNotificationParse(IFormCollection formCollection);
        Result<Subscription> CreateSubscription(string planId, string token, decimal discount);
        Customer FindCustomerId(string customerId);
        PaymentMethod FindPaymentMethod(string token);
        Result<Customer> CreateCustomerIfDoesntExist(Users user);
        Task<Result<PaymentMethod>> CreatePaymentMethod(string paymentMethodNonce, Guid userId);
        bool DeletePaymentMethod(string customerId, string token);
        Result<Transaction> CreateTransaction(string customerid, decimal amount, string orderId, string token, string description);
        Transaction FindTransaction(string transactionId);
        Result<Transaction> RefundTransaction(string transactionId);
        Result<Transaction> VoidTransaction(string transactionId);
        bool PaymentMethodMakeDefault(string customerId, string token);
        Task<string> GenerateBrainTreeTokenAsync();
    }
}
