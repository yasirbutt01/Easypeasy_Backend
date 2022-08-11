using System;
using System.Collections.Generic;
using System.Text;

namespace EasyPeasy.DataViewModels.Common
{
    class PaypalViewModels
    {
    }
    public class PayPalAccessToken
    {
        public string scope { get; set; }
        public string access_token { get; set; }
        public string token_type { get; set; }
        public string app_id { get; set; }
        public int expires_in { get; set; }
        public string nonce { get; set; }
    }


    public class PaypalSubscriptionNameRequest
    {
        public string given_name { get; set; }
        public string surname { get; set; }
    }

    public class PaypalSubscriptionSubscriberRequest
    {
        public PaypalSubscriptionNameRequest name { get; set; }
        public string email_address { get; set; }
    }

    public class PaypalSubscriptionPaymentMethodRequest
    {
        public string payer_selected { get; set; }
        public string payee_preferred { get; set; }
    }

    public class PaypalSubscriptionApplicationContextRequest
    {
        public string brand_name { get; set; }
        public string locale { get; set; }
        public string shipping_preference { get; set; }
        public string user_action { get; set; }
        public PaypalSubscriptionPaymentMethodRequest payment_method { get; set; }
        public string return_url { get; set; }
        public string cancel_url { get; set; }
    }

    public class PaypalSubscriptionRequest
    {
        public string plan_id { get; set; }
        public DateTime start_time { get; set; }
        public PaypalSubscriptionSubscriberRequest subscriber { get; set; }
        public PaypalSubscriptionApplicationContextRequest application_context { get; set; }
    }



    public class PaypalSubscriptionLinkResponse
    {
        public string href { get; set; }
        public string rel { get; set; }
        public string method { get; set; }
    }

    public class PaypalSubscriptionResponse
    {
        public string status { get; set; }
        public string id { get; set; }
        public DateTime create_time { get; set; }
        public List<PaypalSubscriptionLinkResponse> links { get; set; }
    }
}
