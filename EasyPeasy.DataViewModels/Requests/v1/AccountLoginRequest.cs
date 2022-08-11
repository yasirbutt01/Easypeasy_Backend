using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EasyPeasy.DataViewModels.Requests.v1
{
    public class AccountLoginRequest
    {
        public bool IsGuest { get; set; }
        public string CountryCode { get; set; }
        public string PhoneNumber { get; set; }

        public string Password { get; set; }

        [Required]
        public string DeviceToken { get; set; }

        [Required]
        public string DeviceModel { get; set; }

        [Required]
        public string OS { get; set; }

        [Required]
        public string Version { get; set; }

        [Required]
        public int DeviceType { get; set; }
    }
}
