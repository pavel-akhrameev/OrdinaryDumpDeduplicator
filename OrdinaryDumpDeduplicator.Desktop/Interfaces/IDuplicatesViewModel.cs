using System;

namespace OrdinaryDumpDeduplicator.Desktop
{
    internal interface IDuplicatesViewModel
    {
        event Action<Boolean> ViewGroupsByHashRequested;
        event Action<Boolean> ViewGroupsByFoldersRequested;

        event Action<ItemToView[]> MoveToDuplicatesRequested;
        event Action<ItemToView[]> DeleteDuplicatesRequested;

        void SetTreeViewItems(ItemToView[] treeViewItems, Boolean resetForm);

        void AddTreeViewItem(ItemToView treeViewItem, ItemToView parentItemToView);

        void DeleteTreeViewItem(ItemToView treeViewItem);

        void AddSessionMessage(String message);
    }
}
