using System;
using System.Collections.Generic;

namespace OrdinaryDumpDeduplicator.Desktop
{
    internal interface IMainViewModel
    {
        event Action<String> AddDataLocationRequested;
        event Action<ItemToView> RescanRequested;
        event Action<IReadOnlyCollection<ItemToView>> FindDuplicatesRequested;

        event Action AboutFormRequested;
        event Func<Boolean> ApplicationCloseRequested;

        void SetListViewItems(IReadOnlyCollection<ItemToView> items);

        void AddSessionMessage(String message);
    }
}
