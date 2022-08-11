using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EasyPeasy.DataViewModels.Requests
{
    public class ChangePassword
    {
        public Guid ForgotLinkKey { get; set; }
        public string NewPassword { get; set; }
    }
}
