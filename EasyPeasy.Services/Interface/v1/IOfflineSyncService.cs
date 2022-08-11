using EasyPeasy.DataViewModels.Requests.v1;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EasyPeasy.Services.Interface.v1
{
    public interface IOfflineSyncService
    {
        Task<Tuple<string, SyncContactRequest>> SyncContacts(SyncContactRequest model, string deviceToken, Guid userId);
    }
}
