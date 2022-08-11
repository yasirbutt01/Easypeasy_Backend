using System;
using System.Collections.Generic;
using System.Text;

namespace EasyPeasy.DataViewModels.Common
{
    public class AuthToken
    {
        public Guid UserId { get; set; }
        public string DeviceToken { get; set; }
    }
}
