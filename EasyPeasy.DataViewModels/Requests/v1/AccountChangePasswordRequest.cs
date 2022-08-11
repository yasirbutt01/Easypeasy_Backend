using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EasyPeasy.DataViewModels.Requests.v1
{
    public class AccountChangePasswordRequest
    {
        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public string NewPassword { get; set; }
    }

    public class AccountChangePasswordWithCodeRequest
    {
        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public string NewPassword { get; set; }

        [Required]
        public string Code { get; set; }
    }
}
