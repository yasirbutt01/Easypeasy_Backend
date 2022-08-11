using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EasyPeasy.DataViewModels.Requests.v1
{
    public class ContactInviteRequestSh
    {
        public List<ContactInviteRequest> Numbers { get; set; }
    }
    public class ContactInviteRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
    }
}
