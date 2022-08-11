using System;
using System.Collections.Generic;
using System.Text;

namespace EasyPeasy.DataViewModels.Requests.v1
{
    public class AccountSignupRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string CountryCode { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public int GenderId { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string CropImageUrl { get; set; }
        public string CropImageThumbnailUrl { get; set; }
        public string ImageUrl { get; set; }
        public string ImageThumbnailUrl { get; set; }
    }
}
