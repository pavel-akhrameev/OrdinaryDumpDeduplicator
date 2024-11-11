using System;
using System.Collections.Generic;

namespace OrdinaryDumpDeduplicator.Desktop
{
    internal interface IMainViewModel
    {
        event Action<String> AddDataLocationRequested;
        event Action<HierarchicalObject> RescanRequested;
        event Action<IReadOnlyCollection<HierarchicalObject>> FindDuplicatesRequested;

        event Action AboutFormRequested;
        event Func<Boolean> ApplicationCloseRequested;

        void SetListViewItems(IReadOnlyCollection<HierarchicalObject> hierarchicalObjects);

        void AddSessionMessage(String message);
    }
}
