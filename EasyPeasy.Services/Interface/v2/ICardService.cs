using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EasyPeasy.DataViewModels.Enum;
using EasyPeasy.DataViewModels.Response.v2;

namespace EasyPeasy.Services.Interface.v2
{
    public interface ICardService
    {
        Task<Tuple<string, GetMyCardResponse>> CardDetailById(Guid cardId, Guid userId);
        Task<bool> CardAction(List<Guid> greetingIds, ECardAction action, Guid userId);
        Task<List<CardRecipientListResponse>> GetCardRecipients(Guid greetingMasterId, Guid userId);
        Task<GetCardResponse> GetMySentCards(Guid userId, int skip, int take, bool isArchive, Guid? categoryId,
            bool iseGiftAttached, bool isScheduled);
    }
}
