using System;
using System.Collections.Generic;
using System.Text;

namespace EasyPeasy.Services.Interface.v1
{
    public interface ISmsService
    {
        string SendSmsToUser(string textMessage, string to, bool sendToUser);
    }
}
