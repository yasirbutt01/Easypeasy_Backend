using System;
using System.Collections.Generic;
using System.Text;

namespace EasyPeasy.DataViewModels.Requests.v1
{
    public class ContactUpdateGroupRequest
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<ContactCreateGroupDetailRequest> details { get; set; }

        public ContactUpdateGroupRequest()
        {
            details = new List<ContactCreateGroupDetailRequest>();
        }
    }
}
