using EasyPeasy.Services.Interface.v1;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using EasyPeasy.Data.Context;
using Microsoft.EntityFrameworkCore.Internal;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using System.Linq;

namespace EasyPeasy.Services.Implementation.v1
{
    public class SmsService : ISmsService
    {
        private readonly IConfiguration configuration;
        private readonly string twilioAccountSID;
        private readonly string TwilioAuthToken;
        private readonly string TwilioFromNumber;

        public SmsService(IConfiguration configuration)
        {
            this.configuration = configuration;
            twilioAccountSID = configuration.GetValue<string>("TwilioAccountSID");
            TwilioAuthToken = configuration.GetValue<string>("TwilioAuthToken");
            TwilioFromNumber = configuration.GetValue<string>("TwilioFromNumber");
        }

        public string SendSmsToUser(string textMessage, string to, bool sendToUser)
        {
            try
            {
                using (var db = new EasypeasyDbContext())
                {
                    if (sendToUser == false && db.Users.Any(x => x.PhoneNumber == to && x.IsEnabled)) return "";
                    TwilioClient.Init(twilioAccountSID, TwilioAuthToken);

                    var message = MessageResource.Create(
                        body: textMessage,
                        @from: new Twilio.Types.PhoneNumber(TwilioFromNumber),
                        to: new Twilio.Types.PhoneNumber(to)
                    );
                    return message.ToString();
                }
            }
            catch (Exception e)
            {
                return "";
            }

        }
    }
}
