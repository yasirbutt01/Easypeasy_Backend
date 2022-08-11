using System;
using System.Collections.Generic;
using System.Text;

namespace EasyPeasy.DataViewModels.Requests.v1
{
    public class EGifterCreateOrderPersonalization
    {
        public string To { get; set; }
        public string FromName { get; set; }
        public DateTime DeliveryDate { get; set; }
    }

    public class EGifterCreateOrderLineItem
    {
        public string ProductId { get; set; }
        public string Value { get; set; }
        public string Quantity { get; set; }
        public string ExternalId { get; set; }
        public string Culture { get; set; }
        public EGifterCreateOrderPersonalization Personalization { get; set; }
    }

    public class EGifterCreateOrderWebhookSettings
    {
        public string FulfillmentComplete { get; set; }
    }

    public class EGifterCreateOrderRequest
    {
        public string Type { get; set; }
        public List<EGifterCreateOrderLineItem> LineItems { get; set; }
        public string PoNumber { get; set; }
        public string Note { get; set; }
        public EGifterCreateOrderWebhookSettings WebhookSettings { get; set; }
    }

}
