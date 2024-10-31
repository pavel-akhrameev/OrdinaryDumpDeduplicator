using System;
using System.Collections.Generic;

namespace OrdinaryDumpDeduplicator.Desktop
{
    internal interface IMainViewModel
    {
        event Action<String> AddDataLocationRequested;
        event Action<IReadOnlyCollection<HierarchicalObject>> RescanRequested;
        event Action FindDuplicatesRequested; // Переделать на передачу DataLocations

        event Action AboutFormRequested;
        event Func<Boolean> ApplicationCloseRequested;

        void SetListViewItems(IReadOnlyCollection<HierarchicalObject> hierarchicalObjects);

        void AddSessionMessage(String message);
    }
}
