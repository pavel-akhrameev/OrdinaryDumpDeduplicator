using System;
using System.Collections.Generic;

namespace OrdinaryDumpDeduplicator.Desktop
{
    internal interface IMainViewModel
    {
        event Func<String, HierarchicalObject> AddDataLocationRequested;
        event Action<IReadOnlyCollection<HierarchicalObject>> RescanRequested;
        event Action FindDuplicatesRequested; // Переделать на передачу DataLocations

        event Action<Boolean> ViewGroupsByHashRequested;
        event Action ViewGroupsByFoldersRequested;

        event Action<TreeViewItem[]> MoveToDuplicatesRequested;
        event Action<TreeViewItem[]> DeleteDuplicatesRequested;

        event Action AboutFormRequested;
        event Func<Boolean> ApplicationCloseRequested;

        void SetTreeViewItems(TreeViewItem[] treeViewItems);

        void AddSessionMessage(String message);
    }
}
