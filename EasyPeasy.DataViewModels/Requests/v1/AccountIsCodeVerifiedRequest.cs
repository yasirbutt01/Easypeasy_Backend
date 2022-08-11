using System;
using System.Collections.Generic;
using System.Text;

namespace EasyPeasy.DataViewModels.Requests.v1
{
    public class AccountIsCodeVerifiedRequest
    {
        public string phoneNumber { get; set; }
        public string code { get; set; }
        public bool isChangingPhone { get; set; }
    }
}
