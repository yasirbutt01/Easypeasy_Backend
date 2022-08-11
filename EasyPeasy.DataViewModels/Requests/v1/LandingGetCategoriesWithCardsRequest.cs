using System;
using System.Collections.Generic;
using System.Text;

namespace EasyPeasy.DataViewModels.Requests.v1
{
    public class LandingGetCategoriesWithCardsRequest
    {
        public int Skip { get; set; }
        public int Take { get; set; }
        public string Search { get; set; }
        public int? CardsCount { get; set; }
    }
}
