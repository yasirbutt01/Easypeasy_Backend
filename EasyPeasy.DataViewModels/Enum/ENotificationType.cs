using System;
using System.Collections.Generic;
using System.Text;

namespace EasyPeasy.DataViewModels.Enum
{
    public enum ENotificationType
    {
        SentYouCard,
        CardSent,
        ScheduledWillBeSent,
        ScheduledCardButInactivePackage,
        ScheduledCardSent,
        ScheduledCardSentFailed,
        PackageSubscribed,
        SentYouReminder,
        CalenderEventReminder
    }
}
