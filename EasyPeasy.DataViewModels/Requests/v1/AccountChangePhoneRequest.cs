using System;
using System.Collections.Generic;
using System.Text;

namespace EasyPeasy.DataViewModels.Requests.v1
{
    public class AccountChangePhoneRequest
    {
        public PhoneNumberViewModel OldPhone { get; set; }
        public PhoneNumberViewModel NewPhone { get; set; }

    }
}
