using System;
using System.Collections.Generic;
using System.Linq;

using OrdinaryDumpDeduplicator.Common;

namespace OrdinaryDumpDeduplicator.Desktop
{
    internal class DuplicatesViewController
    {
        private readonly OrdinaryDumpDeduplicator _ordinaryDumpDeduplicator;
        private readonly DuplicateReport _duplicateReport;
        private readonly IDuplicatesViewModel _duplicatesViewModel;

        private ItemToView[] _itemsInReport;
        private IReadOnlyCollection<DirectoryWithDuplicates> _duplicatesByDirectories;
        private Boolean _hideIsolatedDuplicates;

        public DuplicatesViewController(OrdinaryDumpDeduplicator deduplicator, DuplicateReport duplicateReport, IDuplicatesViewModel duplicatesViewModel)
        {
            this._ordinaryDumpDeduplicator = deduplicator;
            this._duplicateReport = duplicateReport;
            this._duplicatesViewModel = duplicatesViewModel;
        }

        public void ViewDuplicatesByHash(Boolean hideIsolatedDuplicates, Boolean doResetForm)
        {
            _hideIsolatedDuplicates = hideIsolatedDuplicates;
            // TODO: Переделать на IEnumerable для оптимизации.

            IReadOnlyCollection<SameContentFilesInfo> uniqueIsolatedFiles = _duplicateReport.IsolatedFilesOnly;
            SameContentFilesInfo[] sortedUniqueIsolatedFiles = uniqueIsolatedFiles
                .OrderByDescending(blobInfo => blobInfo.AllDataSize)
                .ToArray();

            SameContentFilesInfo[] sortedDuplicatesByHash;

            if (hideIsolatedDuplicates)
            {
                IReadOnlyCollection<SameContentFilesInfo> duplicatesByHashes = _duplicateReport.UnprocessedDuplicatesFound;
                sortedDuplicatesByHash = duplicatesByHashes
                    .OrderByDescending(blobInfo => blobInfo.DuplicatesDataSize)
                    .ToArray();
            }
            else
            {
                IReadOnlyCollection<SameContentFilesInfo> duplicatesByHashes = _duplicateReport.UnprocessedDuplicatesFound;
                IReadOnlyCollection<SameContentFilesInfo> allDuplicatesIsolated = _duplicateReport.AllDuplicatesIsolated;
                sortedDuplicatesByHash = duplicatesByHashes
                    .Concat(allDuplicatesIsolated)
                    .OrderByDescending(blobInfo => blobInfo.AllDuplicatesDataSize)
                    .ToArray();
            }

            Int32 objectsCount = sortedUniqueIsolatedFiles.Length + sortedDuplicatesByHash.Length;

            var objectsToView = new SameContentFilesInfo[objectsCount];
            Array.Copy(sortedUniqueIsolatedFiles, 0, objectsToView, 0, sortedUniqueIsolatedFiles.Length);
            Array.Copy(sortedDuplicatesByHash, 0, objectsToView, sortedUniqueIsolatedFiles.Length, sortedDuplicatesByHash.Length);

            _duplicatesByDirectories = null;
            _itemsInReport = MakeViewItems(objectsToView, hideIsolatedDuplicates);
            _duplicatesViewModel.SetTreeViewItems(_itemsInReport, doResetForm);
        }

        public void ViewDuplicatesByFolders(Boolean hideIsolatedDuplicates)
        {
            _hideIsolatedDuplicates = hideIsolatedDuplicates;

            _duplicatesByDirectories = _duplicateReport.GetDuplicatesFoundByDirectories(!hideIsolatedDuplicates);
            _itemsInReport = MakeViewItems(_duplicatesByDirectories, hideIsolatedDuplicates);
            _duplicatesViewModel.SetTreeViewItems(_itemsInReport, resetForm: false);
        }

        public void MoveToDuplicates(ItemToView[] treeViewItems)
        {
            IReadOnlyDictionary<FileInfo, ItemToView> itemsAndFilesToMove = GetDuplicates(treeViewItems);
            var duplicatesToMove = new List<FileInfo>(itemsAndFilesToMove.Keys);
            IDictionary<FileInfo, FileInfo> movedFilesInfo = _ordinaryDumpDeduplicator.MoveDuplicatesToSpecialFolder(_duplicateReport, duplicatesToMove);

            foreach (KeyValuePair<FileInfo, ItemToView> movedItem in itemsAndFilesToMove)
            {
                if (movedFilesInfo.TryGetValue(movedItem.Key, out FileInfo movedFileInfo))
                {
                    // File moved.
                    _duplicatesViewModel.DeleteTreeViewItem(movedItem.Value);

                    ItemToView fileViewItem = MakeViewItem(movedFileInfo, _hideIsolatedDuplicates);
                    if (!fileViewItem.IsHidden)
                    {
                        ItemToView parentItemToView = GetParentItemToView(movedItem.Key);
                        _duplicatesViewModel.AddTreeViewItem(fileViewItem, parentItemToView);
                    }

                    // TODO: Обновить ItemToView от родительского SameContentFilesInfo или от DirectoryWithDuplicates.
                    // TODO: (?) Как понять, нужно ли сейчас обновить родительский item?
                }
            }
        }

        public void DeleteDuplicates(ItemToView[] treeViewItems)
        {
            IReadOnlyDictionary<FileInfo, ItemToView> itemsAndFilesToDelete = GetDuplicates(treeViewItems);
            var duplicatesToDelete = new List<FileInfo>(itemsAndFilesToDelete.Keys);
            IReadOnlyCollection<FileInfo> removedDuplicatesInfo = _ordinaryDumpDeduplicator.DeleteDuplicates(_duplicateReport, duplicatesToDelete);
            HashSet<FileInfo> removedDuplicatesSet = new HashSet<FileInfo>(removedDuplicatesInfo);

            foreach (KeyValuePair<FileInfo, ItemToView> deletedItem in itemsAndFilesToDelete)
            {
                if (removedDuplicatesSet.Contains(deletedItem.Key))
                {
                    // File removed.
                    _duplicatesViewModel.DeleteTreeViewItem(deletedItem.Value);

                    // TODO: Обновить ItemToView от родительского SameContentFilesInfo или от DirectoryWithDuplicates.
                    // TODO: (?) Как понять, нужно ли сейчас обновить родительский item?
                }
            }
        }

        #region Private methods

        private ItemToView GetParentItemToView(FileInfo fileInfo)
        {
            ItemToView parentItemToView;

            if (_duplicatesByDirectories != null)
            {
                DirectoryWithDuplicates parentDirectory = _duplicatesByDirectories
                    .SelectMany(directory => directory.SubDirectories)
                    .Concat(_duplicatesByDirectories)
                    .Single(directory => directory.DuplicatesInTheDirectory.Contains(fileInfo));

                parentItemToView = _itemsInReport.SingleOrDefault(itemToView => parentDirectory.Equals(itemToView.WrappedObject));

            }
            else
            {
                SameContentFilesInfo sameContentFilesInfo = fileInfo.SameContentFilesInfo;
                parentItemToView = _itemsInReport.SingleOrDefault(itemToView => sameContentFilesInfo.Equals(itemToView.WrappedObject));
            }

            return parentItemToView;
        }

        #region Private static methods

        private static IReadOnlyDictionary<FileInfo, ItemToView> GetDuplicates(ItemToView[] treeViewItems)
        {
            var fileObjects = new Dictionary<FileInfo, ItemToView>(treeViewItems.Length);
            foreach (ItemToView treeViewItem in treeViewItems)
            {
                if (treeViewItem == null || treeViewItem.WrappedObject == null)
                {
                    throw new Exception("TreeViewItem is empty.");
                }

                if (treeViewItem.Type == typeof(FileInfo))
                {
                    var duplicateInfo = (FileInfo)treeViewItem.WrappedObject;
                    fileObjects.Add(duplicateInfo, treeViewItem);
                }
            }

            return fileObjects;
        }

        private static ItemToView[] MakeViewItems(IReadOnlyCollection<SameContentFilesInfo> duplicatesToView, Boolean hideIsolatedDuplicates)
        {
            var topLevelTreeViewItems = new List<ItemToView>(duplicatesToView.Count);
            foreach (SameContentFilesInfo groupInReport in duplicatesToView)
            {
                ItemToView treeViewItem = MakeViewItem(groupInReport, hideIsolatedDuplicates);
                if (!treeViewItem.IsHidden)
                {
                    topLevelTreeViewItems.Add(treeViewItem);
                }
            }

            return topLevelTreeViewItems.ToArray();
        }

        private static ItemToView[] MakeViewItems(IReadOnlyCollection<DirectoryWithDuplicates> directories, Boolean hideIsolatedDuplicates)
        {
            var topLevelTreeViewItems = new List<ItemToView>(directories.Count);
            foreach (DirectoryWithDuplicates directoryWithDuplicates in directories)
            {
                ItemToView treeViewItem = MakeViewItem(directoryWithDuplicates, hideIsolatedDuplicates);
                if (!treeViewItem.IsHidden)
                {
                    topLevelTreeViewItems.Add(treeViewItem);
                }
            }

            return topLevelTreeViewItems.ToArray();
        }

        private static ItemToView MakeViewItem(SameContentFilesInfo duplicatesInfo, Boolean hideIsolatedDuplicates)
        {
            var childItemsList = new List<ItemToView>(duplicatesInfo.Duplicates.Count);
            foreach (FileInfo duplicate in duplicatesInfo.Duplicates)
            {
                ItemToView childItem = MakeViewItem(duplicate, hideIsolatedDuplicates);
                if (!childItem.IsHidden)
                {
                    childItemsList.Add(childItem);
                }
            }

            ItemToView[] childItems = childItemsList.ToArray();
            ItemToView treeViewItem = AnalyzeSameContentFiles(duplicatesInfo, childItems, hideIsolatedDuplicates);

            return treeViewItem;
        }

        private static ItemToView MakeViewItem(DirectoryWithDuplicates directory, Boolean hideIsolatedDuplicates)
        {
            var subDirectoriesToView = new List<ItemToView>();
            foreach (DirectoryWithDuplicates subDirectory in directory.SubDirectories)
            {
                ItemToView subDirectoryItem = MakeViewItem(subDirectory, hideIsolatedDuplicates);
                if (!subDirectoryItem.IsHidden)
                {
                    subDirectoriesToView.Add(subDirectoryItem);
                }
            }

            var duplicatesToView = new List<ItemToView>();
            foreach (FileInfo duplicateInfo in directory.DuplicatesInTheDirectory)
            {
                ItemToView duplicateItem = MakeViewItem(duplicateInfo, hideIsolatedDuplicates);
                if (!duplicateItem.IsHidden)
                {
                    duplicatesToView.Add(duplicateItem);
                }
            }

            var childItems = new List<ItemToView>();
            childItems.AddRange(subDirectoriesToView);
            childItems.AddRange(duplicatesToView);

            var directoryToView = AnalyzeDirectoryWithDuplicates(directory, childItems.ToArray());
            return directoryToView;
        }

        private static ItemToView MakeViewItem(FileInfo fileInfo, Boolean hideIsolatedDuplicates)
        {
            ItemToView treeViewItem = AnalyzeFileInfo(fileInfo, hideIsolatedDuplicates);
            return treeViewItem;
        }

        private static ItemToView AnalyzeSameContentFiles(SameContentFilesInfo sameContentFilesInfo, ItemToView[] childItems, Boolean hideIsolatedDuplicates)
        {
            System.Drawing.Color itemColor;
            const Boolean isObjectMoveable = false;
            const Boolean isObjectDeletable = false;
            Boolean isObjectHidden;

            if (sameContentFilesInfo.AllDuplicatesIsolated)
            {
                itemColor = System.Drawing.Color.DarkGreen;
                isObjectHidden = hideIsolatedDuplicates;
            }
            else if (sameContentFilesInfo.ContainsIsolatedFilesOnly)
            {
                itemColor = System.Drawing.Color.Red;
                isObjectHidden = false;
            }
            else
            {
                itemColor = System.Drawing.Color.Black;
                isObjectHidden = false;
            }

            String duplicatesInfoString = MakeSameContentFilesInfoString(sameContentFilesInfo);
            var treeViewItem = new ItemToView(sameContentFilesInfo, typeof(SameContentFilesInfo), childItems, duplicatesInfoString, itemColor, isObjectMoveable, isObjectDeletable, isObjectHidden);
            return treeViewItem;
        }

        private static ItemToView AnalyzeDirectoryWithDuplicates(DirectoryWithDuplicates directory, ItemToView[] childItems)
        {
            System.Drawing.Color itemColor;
            const Boolean isObjectMoveable = false;
            const Boolean isObjectDeletable = false;
            Boolean isObjectHidden;

            if (childItems.Length > 0)
            {
                Boolean someSubItemsRed = childItems.Any(item => item.Color == System.Drawing.Color.Red);
                if (someSubItemsRed)
                {
                    itemColor = System.Drawing.Color.Red;
                    isObjectHidden = false;
                }
                else
                {
                    Boolean allSubItemsGreen = childItems.All(item => item.Color == System.Drawing.Color.Green || item.Color == System.Drawing.Color.DarkGreen);
                    itemColor = allSubItemsGreen
                        ? System.Drawing.Color.DarkGreen
                        : System.Drawing.Color.Black;
                    isObjectHidden = false;
                }
            }
            else
            {
                itemColor = System.Drawing.Color.Black;
                isObjectHidden = true;
            }

            String directoryRepresentationString = directory.ToString();
            var directoryToView = new ItemToView(directory, directory.GetType(), childItems.ToArray(), directoryRepresentationString, itemColor, isObjectMoveable, isObjectDeletable, isObjectHidden);
            return directoryToView;
        }

        private static ItemToView AnalyzeFileInfo(FileInfo fileInfo, Boolean hideIsolatedDuplicates)
        {
            System.Drawing.Color itemColor;
            Boolean isObjectMoveable;
            Boolean isObjectDeletable;
            Boolean isObjectHidden;

            if (fileInfo.Sort.HasFlag(DuplicateSort.IsolatedDuplicate))
            {
                if (fileInfo.IsBlobTotallyIsolated)
                {
                    itemColor = System.Drawing.Color.Red;
                    isObjectMoveable = false;
                    isObjectDeletable = false;
                    isObjectHidden = false;
                }
                else
                {
                    itemColor = System.Drawing.Color.Green;
                    isObjectMoveable = false;
                    isObjectDeletable = true;
                    isObjectHidden = hideIsolatedDuplicates;
                }
            }
            else // Обычный файл-дубликат.
            {
                itemColor = System.Drawing.Color.Black;
                isObjectMoveable = true;
                isObjectDeletable = false;
                isObjectHidden = false;
            }

            String fileRepresentation = $"{fileInfo.File.Name} | {fileInfo.File.ParentDirectory.Path}";
            var itemToView = new ItemToView(fileInfo, typeof(FileInfo), childItems: new ItemToView[] { }, fileRepresentation, itemColor, isObjectMoveable, isObjectDeletable, isObjectHidden);
            return itemToView;
        }

        private static String MakeSameContentFilesInfoString(SameContentFilesInfo sameContentFilesInfo)
        {
            //String dataSizeString = Helper.GetDataSizeString(sameContentFilesInfo.BlobInfo.Size).PadLeft(totalWidth: 10);
            String dataSizeString = Helper.GetDataSizeString(sameContentFilesInfo.BlobInfo.Size);
            //String duplicatesCountString = sameContentFilesInfo.Duplicates.Count.ToString().PadRight(totalWidth: 2);
            String duplicatesCountString = sameContentFilesInfo.Duplicates.Count.ToString();

            return $"{dataSizeString} x {duplicatesCountString} | {sameContentFilesInfo.BlobInfo.HexString}";
        }

        #endregion

        #endregion
    }
}
