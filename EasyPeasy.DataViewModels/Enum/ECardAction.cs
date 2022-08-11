using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;

namespace EasyPeasy.DataViewModels.Enum
{
    public enum ECardAction
    {
        Archive = 1,
        UnArchive = 2,
        Delete = 3,
        SendReminder = 4,
        CardOpened = 5
    }
}
