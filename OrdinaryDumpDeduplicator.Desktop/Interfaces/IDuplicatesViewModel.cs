using System;

namespace OrdinaryDumpDeduplicator.Desktop
{
    internal interface IDuplicatesViewModel
    {
        event Action<Boolean> ViewGroupsByHashRequested;
        event Action ViewGroupsByFoldersRequested;

        event Action<TreeViewItem[]> MoveToDuplicatesRequested;
        event Action<TreeViewItem[]> DeleteDuplicatesRequested;

        void SetTreeViewItems(TreeViewItem[] treeViewItems, Boolean resetForm);

        void AddSessionMessage(String message);
    }
}
