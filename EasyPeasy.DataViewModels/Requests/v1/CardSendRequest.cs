using System;
using System.Collections.Generic;
using System.Text;

namespace EasyPeasy.DataViewModels.Requests.v1
{
    public class CardSendRequest
    {
        public Guid CardId { get; set; }
        public Guid CategoryId { get; set; }
        public string Description { get; set; }
        public int FontId { get; set; }
        public Guid? CategoryMusicFileId { get; set; }
        public bool IsSentInGroup { get; set; }
        public Guid? UserGroupId { get; set; }
        public bool IsScheduled { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public string VideoUrl { get; set; }
        public string VideoThumbUrl { get; set; }
        public bool IsReminder { get; set; }

        public bool IseGiftAttached { get; set; }

        public CardSendeGiftRequest EGiftDetail { get; set; }


        public List<CardSendDetailRequest> details { get; set; }

        public List<CardSendImageRequest> Images { get; set; }

        public CardSendRequest()
        {
            details = new List<CardSendDetailRequest>();
            Images = new List<CardSendImageRequest>();
        }
    }

    public class CardSendeGiftRequest
    {
        public Guid? PaymentMethodId { get; set; }
        public string ProductId { get; set; }
        public double CardValue { get; set; }
        public double FeeAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string DenominationType { get; set; }
        public string BrandName { get; set; }
        public string PrimaryColor { get; set; }
        public string SecondaryColor { get; set; }
        public string CardImageUrl { get; set; }

    }

    public class CardSendDetailRequest
    {
        public Guid? SentToId { get; set; }
        public string SentToName { get; set; }
        public string SentToPhoneNumber { get; set; }
    }

    public class CardSendImageRequest
    {
        public string ImageFileUrl { get; set; }
        public string ImageFileThumbnailUrl { get; set; }
    }
}
