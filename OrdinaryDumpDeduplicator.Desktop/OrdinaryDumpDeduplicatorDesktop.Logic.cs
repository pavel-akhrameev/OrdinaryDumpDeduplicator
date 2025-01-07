using System;
using System.Collections.Generic;
using System.Linq;

using OrdinaryDumpDeduplicator.Common;

namespace OrdinaryDumpDeduplicator.Desktop
{
    partial class OrdinaryDumpDeduplicatorDesktop
    {
        #region Private fields

        private DuplicateReport _currentDuplicateReport; // We show only one DuplicateReportForm at a time.

        #endregion

        #region Private methods

        private void SubscribeToEvents()
        {
            _windowsManager.MainViewModel.AddDataLocationRequested += AddDataLocationRequested;
            _windowsManager.MainViewModel.RescanRequested += RescanRequested;
            _windowsManager.MainViewModel.FindDuplicatesRequested += FindDuplicatesRequested;

            _windowsManager.DuplicatesViewModel.ViewGroupsByHashRequested += ViewDuplicatesByHashRequested;
            _windowsManager.DuplicatesViewModel.ViewGroupsByFoldersRequested += ViewDuplicatesByFoldersRequested;

            _windowsManager.DuplicatesViewModel.MoveToDuplicatesRequested += MoveToDuplicatesRequested;
            _windowsManager.DuplicatesViewModel.DeleteDuplicatesRequested += DeleteDuplicatesRequested;

            _windowsManager.MainViewModel.AboutFormRequested += AboutFormRequested;
            _windowsManager.MainViewModel.ApplicationCloseRequested += ApplicationCloseRequested;
        }

        private void AddDataLocationRequested(String directoryPath)
        {
            IReadOnlyCollection<DataLocation> dataLocations = _ordinaryDumpDeduplicator.AddDataLocation(directoryPath);
            var listViewItems = MakeViewItems(dataLocations);

            IMainViewModel mainViewModel = _windowsManager.MainViewModel;
            mainViewModel.SetListViewItems(listViewItems);
        }

        private void RescanRequested(ItemToView dataLocationItem)
        {
            DataLocation dataLocation = dataLocationItem.WrappedObject as DataLocation;

            IMainViewModel mainViewModel = _windowsManager.MainViewModel;
            mainViewModel.AddSessionMessage("Rescan started.");
            DateTime now = DateTime.Now;

            System.Threading.Tasks.Task<DataLocation> doInspectionTask = _ordinaryDumpDeduplicator.DoInspection(dataLocation);
            doInspectionTask.ContinueWith(inspectionTask =>
            {
                DataLocation currentDataLocation = inspectionTask.Result;

                TimeSpan timeSpent = DateTime.Now.Subtract(now);
                String timeSpentString = TimeSpanToString(timeSpent);
                mainViewModel.AddSessionMessage($"Rescan finished in {timeSpentString}.");

                GetAndViewDuplicatesByHash(new[] { currentDataLocation }, hideIsolatedDuplicates: true, doResetForm: true); // by default
            });
        }

        private void FindDuplicatesRequested(IReadOnlyCollection<ItemToView> dataLocationItems)
        {
            DataLocation[] dataLocations = GetDataLocations(dataLocationItems);
            GetAndViewDuplicatesByHash(dataLocations, hideIsolatedDuplicates: true, doResetForm: true); // by default
        }

        /// <remarks>Это событие вызывается с формы <c>DuplicateReportForm</c> значит, саму форму перезапускать не надо.</remarks>
        private void ViewDuplicatesByHashRequested(Boolean hideIsolatedDuplicates)
        {
            GetAndViewDuplicatesByHash(_currentDuplicateReport.DataLocations, hideIsolatedDuplicates, doResetForm: false);
        }

        private void ViewDuplicatesByFoldersRequested(Boolean hideIsolatedDuplicates)
        {
            GetAndViewDuplicatesByFolders(_currentDuplicateReport.DataLocations, hideIsolatedDuplicates);
        }

        private void GetAndViewDuplicatesByHash(IReadOnlyCollection<DataLocation> dataLocations, Boolean hideIsolatedDuplicates, Boolean doResetForm)
        {
            if (dataLocations != null && dataLocations.Count > 0)
            {
                DateTime now = DateTime.Now;

                System.Threading.Tasks.Task<DuplicateReport> getDuplicatesTask = _ordinaryDumpDeduplicator.GetDuplicatesFound(dataLocations);
                getDuplicatesTask.ContinueWith(duplicateReportTask =>
                {
                    DuplicateReport duplicateReport = duplicateReportTask.Result;
                    _currentDuplicateReport = duplicateReport;

                    TimeSpan timeSpent = DateTime.Now.Subtract(now);
                    String timeSpentString = TimeSpanToString(timeSpent);
                    _windowsManager.MainViewModel.AddSessionMessage($"Duplicates search completed in {timeSpentString}.");

                    var duplicatesViewController = new DuplicatesViewController(duplicateReport, _windowsManager.DuplicatesViewModel);
                    duplicatesViewController.ViewDuplicatesByHash(hideIsolatedDuplicates, doResetForm);
                    _windowsManager.ShowDuplicatesForm();
                });
            }
        }

        private void GetAndViewDuplicatesByFolders(IReadOnlyCollection<DataLocation> dataLocations, Boolean hideIsolatedDuplicates)
        {
            if (dataLocations != null && dataLocations.Count > 0)
            {
                DateTime now = DateTime.Now;

                System.Threading.Tasks.Task<DuplicateReport> getDuplicatesTask = _ordinaryDumpDeduplicator.GetDuplicatesFound(dataLocations);
                getDuplicatesTask.ContinueWith(duplicateReportTask =>
                {
                    DuplicateReport duplicateReport = duplicateReportTask.Result;
                    _currentDuplicateReport = duplicateReport;

                    TimeSpan timeSpent = DateTime.Now.Subtract(now);
                    String timeSpentString = TimeSpanToString(timeSpent);
                    _windowsManager.MainViewModel.AddSessionMessage($"Duplicates search completed in {timeSpentString}.");

                    var duplicatesViewController = new DuplicatesViewController(duplicateReport, _windowsManager.DuplicatesViewModel);
                    duplicatesViewController.ViewDuplicatesByFolders(hideIsolatedDuplicates);
                    _windowsManager.ShowDuplicatesForm();
                });
            }
        }

        private void MoveToDuplicatesRequested(ItemToView[] treeViewItems)
        {
            IReadOnlyDictionary<FileInfo, ItemToView> duplicatesToMove = DuplicatesViewController.GetDuplicates(treeViewItems);
            _ordinaryDumpDeduplicator.MoveKnownDuplicatesToSpecialFolder(_currentDuplicateReport, duplicatesToMove.Keys.ToArray());

            // TODO: обновить данные в БД.
            // TODO: обновить данные на форме.
        }

        private void DeleteDuplicatesRequested(ItemToView[] treeViewItems)
        {
            IReadOnlyDictionary<FileInfo, ItemToView> duplicatesToDelete = DuplicatesViewController.GetDuplicates(treeViewItems);
            _ordinaryDumpDeduplicator.DeleteDuplicate(_currentDuplicateReport, duplicatesToDelete.Keys.ToArray());

            // TODO: обновить данные в БД.
            // TODO: обновить данные на форме.
        }

        private void AboutFormRequested()
        {
            _windowsManager.ShowAboutBox();
        }

        private Boolean ApplicationCloseRequested()
        {
            Boolean allowedToClose = true;
            _windowsManager.CloseAllAdditionalForms();

            return allowedToClose;
        }

        #endregion

        #region Private static methods

        private static ItemToView[] MakeViewItems(IReadOnlyCollection<DataLocation> dataLocations)
        {
            var itemsToView = new List<ItemToView>(dataLocations.Count);
            foreach (DataLocation dataLocation in dataLocations)
            {
                ItemToView item = MakeViewItem(dataLocation);
                itemsToView.Add(item);
            }

            return itemsToView.ToArray();
        }

        private static ItemToView MakeViewItem(DataLocation dataLocation)
        {
            String dataLocationString = dataLocation.Path;
            var itemToView = new ItemToView(dataLocation, typeof(DataLocation), childItems: new ItemToView[] { }, dataLocationString, System.Drawing.Color.Black, isMoveable: false, isDeletable: true, isHidden: false);
            return itemToView;
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

        private static DataLocation[] GetDataLocations(IReadOnlyCollection<ItemToView> itemsToView)
        {
            var dataLocations = new List<DataLocation>(itemsToView.Count);
            foreach (ItemToView itemToView in itemsToView)
            {
                if (itemToView == null || itemToView.WrappedObject == null || itemToView.Type != typeof(DataLocation))
                {
                    throw new Exception("ItemToView is not valid."); // TODO
                }

                dataLocations.Add(itemToView.WrappedObject as DataLocation);
            }

            return dataLocations.ToArray();
        }

        private static String TimeSpanToString(TimeSpan timeSpan)
        {
            const String d2 = "D2";

            Int32 hours = (Int32)Math.Floor(timeSpan.TotalHours);
            String millisecondsString = timeSpan.Milliseconds.ToString(d2).Substring(0, 2);
            String formattedString = $"{hours.ToString(d2)}:{timeSpan.Minutes.ToString(d2)}:{timeSpan.Seconds.ToString(d2)}.{millisecondsString}";

            return formattedString;
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
    }
}
