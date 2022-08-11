using System;
using System.Collections.Generic;
using System.Text;

namespace EasyPeasy.DataViewModels.Requests.v1
{
    public class ContactCreateCalenderEventRequest
    {
        public Guid? ContactUserId { get; set; }
        public Guid? ContactId { get; set; }
        public bool IsGroup { get; set; }
        public Guid? UserGroupId { get; set; }
        public Guid? CategoryId { get; set; }
        public DateTime Date { get; set; }
        public bool IsReminder { get; set; }
    }
}
