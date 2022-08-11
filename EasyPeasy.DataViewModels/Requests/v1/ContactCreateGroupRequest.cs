using System;
using System.Collections.Generic;
using System.Text;

namespace EasyPeasy.DataViewModels.Requests.v1
{
    public class ContactCreateGroupRequest
    {
        public string Name { get; set; }
        public List<ContactCreateGroupDetailRequest> details { get; set; }

        public ContactCreateGroupRequest()
        {
            details = new List<ContactCreateGroupDetailRequest>();
        }
    }

    public class ContactCreateGroupDetailRequest
    {
        public Guid? UserId { get; set; }
        public string ContactFirstName { get; set; }
        public string ContactLastName { get; set; }

        public string CountryCode { get; set; }
        public string PhoneNumber { get; set; }
    }
}
