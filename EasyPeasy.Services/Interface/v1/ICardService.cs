using EasyPeasy.DataViewModels.Enum;
using EasyPeasy.DataViewModels.Requests.v1;
using EasyPeasy.DataViewModels.Response.v1;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EasyPeasy.Data.DTOs;

namespace EasyPeasy.Services.Interface.v1
{
    public interface ICardService
    {
        Task<List<UserCardDetails>> Double1Fix();
        Task<bool> Send(CardSendRequest request, Guid userId);
        Task<List<CardArchivedCategoryCountResponse>> ArchivedCategoryCount(Guid userId, int skip, int take);
        Task<bool> CardAction(Guid greetingId, ECardAction action, Guid userId);
        Task<GetCardResponse> GetMyReceivedCards(Guid userId, int skip, int take, bool isArchive, Guid? categoryId, bool iseGiftAttached, bool isScheduled);
        Task<GetCardResponse> GetMySentCards(Guid userId, int skip, int take, bool isArchive, Guid? categoryId, bool iseGiftAttached, bool isScheduled);
        Task<Tuple<string, GetMyCardResponse>> CardDetailById(Guid cardId, Guid userId);
    }
}
