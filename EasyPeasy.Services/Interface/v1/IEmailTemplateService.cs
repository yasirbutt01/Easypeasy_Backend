using System;
using System.Collections.Generic;
using System.Text;

namespace EasyPeasy.Services.Interface.v1
{
    public interface IEmailTemplateService
    {
        bool Welcome(string toName, string toEmail);
        bool Subscription(string toEmail, string packageName, decimal price, string cardMaskedNumber, string cardExpiry,
            DateTime invoiceTime, string invoiceId, string paymentMethodLogo, int paymentMethodType);
        bool UnSubscribed(string toEmail, string packageName, DateTime? validTill);
        bool CongratulationsCardReceived(string toEmail, decimal price, string fromName, string brandName, string brandLogo, string greetingId, string secondaryColor);
    }
}
