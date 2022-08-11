using System;
using System.Collections.Generic;
using System.Text;
using EasyPeasy.DataViewModels.Enum;

namespace EasyPeasy.DataViewModels.Requests.v1
{
    public class GetMyCardsRequest
    {
        public int Skip { get; set; }
        public int Take { get; set; }
        public bool IsArchive { get; set; }
        public Guid? CategoryId { get; set; }
        public bool IseGiftAttached { get; set; }
        public bool IsScheduled { get; set; }
    }
}
