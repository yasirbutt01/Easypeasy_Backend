using EasyPeasy.DataViewModels.Response.admin;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasyPeasy.Services.Interface.admin
{
    public interface ICardReporting
    {
        GenralListResponse<SendingCard> GetSendingCards(GenralListResponse<SendingCard> page, DateTime? str, DateTime? endd);
    }
}
