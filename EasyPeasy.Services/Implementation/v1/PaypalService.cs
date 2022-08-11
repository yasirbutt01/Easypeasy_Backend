using EasyPeasy.DataViewModels.Common;
using EasyPeasy.Services.Interface.v1;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace EasyPeasy.Services.Implementation.v1
{
    public class PaypalService : IPaypalService
    {
        private readonly IConfiguration configuration;
        private readonly string payPalClientId;
        private readonly string payPalSecret;
        private readonly string payPalBaseUrl;

        public PaypalService(IConfiguration configuration)
        {
            payPalClientId = configuration.GetValue<string>("PayPalClientId");
            payPalSecret = configuration.GetValue<string>("PayPalSecret");
            payPalBaseUrl = configuration.GetValue<string>("PayPalBaseUrl");
        }

        public async Task<PaypalSubscriptionResponse> CreatePaypalSubscriptionAsync(string planId, string givenName, string surName, string email, DateTime creationTime)
        {
            HttpClient http = GetPaypalHttpClient();
            var accessToken = await GetPayPalAccessTokenAsync(http);
            var request = new HttpRequestMessage(HttpMethod.Post, "v1/billing/subscriptions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.access_token);

            var payment = JObject.FromObject(new PaypalSubscriptionRequest
            {
                plan_id = planId,
                start_time = creationTime,
                application_context = new PaypalSubscriptionApplicationContextRequest
                {
                    brand_name = "EasyPeasy",
                    locale = "en-US",
                    shipping_preference = "NO_SHIPPING",
                    return_url = "https://example.com/returnUrl",
                    cancel_url = "https://example.com/cancelUrl",
                    user_action = "SUBSCRIBE_NOW",
                    payment_method = new PaypalSubscriptionPaymentMethodRequest
                    {
                        payee_preferred = "IMMEDIATE_PAYMENT_REQUIRED",
                        payer_selected = "PAYPAL"
                    }
                },
                subscriber = new PaypalSubscriptionSubscriberRequest
                {
                    name = new PaypalSubscriptionNameRequest
                    {
                        given_name = givenName,
                        surname = surName
                    },
                    email_address = email
                }
            });

            request.Content = new StringContent(JsonConvert.SerializeObject(payment), Encoding.UTF8, "application/json");

            var response = await http.SendAsync(request);

            string content = await response.Content.ReadAsStringAsync();
            var paypalPaymentCreated = JsonConvert.DeserializeObject<PaypalSubscriptionResponse>(content);
            return paypalPaymentCreated;
        }

        private async Task<PayPalAccessToken> GetPayPalAccessTokenAsync(HttpClient http)
        {
            var clientId = payPalClientId;
            var secret = payPalSecret;

            byte[] bytes = Encoding.GetEncoding("iso-8859-1").GetBytes($"{clientId}:{secret}");

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/v1/oauth2/token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(bytes));

            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials"
            };

            request.Content = new FormUrlEncodedContent(form);

            HttpResponseMessage response = await http.SendAsync(request);

            string content = await response.Content.ReadAsStringAsync();
            PayPalAccessToken accessToken = JsonConvert.DeserializeObject<PayPalAccessToken>(content);
            return accessToken;
        }

        private HttpClient GetPaypalHttpClient()
        {
            var http = new HttpClient
            {
                BaseAddress = new Uri(payPalBaseUrl),
                Timeout = TimeSpan.FromSeconds(30),
            };

            return http;
        }
    }
}
