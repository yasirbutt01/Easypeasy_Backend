using System;
using System.Collections.Generic;
using System.Text;

namespace EasyPeasy.DataViewModels.Requests.admin
{
    public class SaveCardRequest
    {
        public Guid Id { get; set; }
        public string FrontContent { get; set; }
        public string InsideContent { get; set; }
        public string KeyWord { get; set; }
        public string StockId { get; set; }
        public bool EGiftCardEnable { get; set; }
        public string CardImageUrl { get; set; }
        public string CardImageThumbnailUrl { get; set; }
        public List<SaveCardCategory> Categories { get; set; }
    }

    public class SaveCardCategory
    {
        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }
        public List<SaveCardCategoryType> Types { get; set; }
    }

    public class SaveCardCategoryType
    {
        public Guid Id { get; set; }
        public Guid CategoryTypeId { get; set; }
    }
}
