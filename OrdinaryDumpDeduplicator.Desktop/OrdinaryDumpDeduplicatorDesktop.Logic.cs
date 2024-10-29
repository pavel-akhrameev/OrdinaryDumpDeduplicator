using System;
using System.Collections.Generic;
using System.Linq;

using OrdinaryDumpDeduplicator.Common;

namespace OrdinaryDumpDeduplicator.Desktop
{
    partial class OrdinaryDumpDeduplicatorDesktop
    {
        #region Private fields

        private HashSet<DataLocation> _currentDataLocations;
        private DuplicateReport _currentDuplicateReport;

        #endregion

        #region Private methods

        private void SubscribeToEvents()
        {
            _windowsManager.MainViewModel.AddDataLocationRequested += AddDataLocationRequested;
            _windowsManager.MainViewModel.RescanRequested += RescanRequested;
            _windowsManager.MainViewModel.FindDuplicatesRequested += FindDuplicatesRequested;

            _windowsManager.MainViewModel.ViewGroupsByHashRequested += ViewDuplicatesByHashRequested;
            _windowsManager.MainViewModel.ViewGroupsByFoldersRequested += ViewDuplicatesByFoldersRequested;

            _windowsManager.MainViewModel.MoveToDuplicatesRequested += MoveToDuplicatesRequested;
            _windowsManager.MainViewModel.DeleteDuplicatesRequested += DeleteDuplicatesRequested;

            _windowsManager.MainViewModel.AboutFormRequested += AboutFormRequested;
            _windowsManager.MainViewModel.ApplicationCloseRequested += ApplicationCloseRequested;
        }

        private HierarchicalObject AddDataLocationRequested(String directoryPath) // TODO !!!!!
        {
            DataLocation dataLocation = _ordinaryDumpDeduplicator.AddDataLocation(directoryPath);

            var dataLocationObject = HierarchicalObject.Create(dataLocation, ObjectSort.None, childObjects: null, dataLocation.Path); // TODO: А каким типом будем передавать во фронт подобные объекты?
            return dataLocationObject;
        }

        private void RescanRequested(IReadOnlyCollection<HierarchicalObject> dataLocationObjects)
        {
            var dataLocationObject = dataLocationObjects.First(); // TODO
            if (dataLocationObject.Type != typeof(DataLocation))
            {
                throw new Exception(""); // TODO
            }

            DataLocation dataLocation = dataLocationObject.Object as DataLocation;

            IMainViewModel mainViewModel = _windowsManager.MainViewModel;
            mainViewModel.AddSessionMessage("Rescan started.");
            DateTime now = DateTime.Now;

            DataLocation currentDataLocation = _ordinaryDumpDeduplicator.DoInspection(dataLocation);
            AddCurrentDataLocation(currentDataLocation);

            TimeSpan timeSpent = DateTime.Now.Subtract(now);
            String timeSpentString = TimeSpanToString(timeSpent);
            mainViewModel.AddSessionMessage($"Rescan finished in {timeSpentString}.");

            ViewDuplicatesByHash(hideIsolatedDuplicates: true, doResetForm: true); // by default
        }

        private void FindDuplicatesRequested()
        {
            ViewDuplicatesByHash(hideIsolatedDuplicates: true, doResetForm: true); // by default
        }

        /// <remarks>Это событие вызывается с формы <c>DuplicateReportForm</c> значит, саму форму перезапускать не надо.</remarks>
        private void ViewDuplicatesByHashRequested(Boolean hideIsolatedDuplicates)
        {
            ViewDuplicatesByHash(hideIsolatedDuplicates, doResetForm: false);
        }

        private void ViewDuplicatesByFoldersRequested()
        {
            if (_currentDataLocations != null && _currentDataLocations.Count > 0)
            {
                _currentDuplicateReport = _ordinaryDumpDeduplicator.GetDuplicatesFound(_currentDataLocations.ToArray());

                HierarchicalObject[] duplicatesByDirectories = _currentDuplicateReport.GroupDuplicatesByDirectories();
                TreeViewItem[] itemsInReport = MakeTreeViewItems(duplicatesByDirectories, hideIsolatedDuplicates: false);

                _windowsManager.MainViewModel.SetTreeViewItems(itemsInReport);
            }
        }

        private void MoveToDuplicatesRequested(TreeViewItem[] treeViewItems)
        {
            HierarchicalObject[] hierarchicalObjects = GetHierarchicalObjects(treeViewItems);
            _ordinaryDumpDeduplicator.MoveKnownDuplicatesToSpecialFolder(_currentDuplicateReport, hierarchicalObjects);

            // TODO: обновить данные в БД.
            // TODO: обновить данные на форме.
        }

        private void DeleteDuplicatesRequested(TreeViewItem[] treeViewItems)
        {
            HierarchicalObject[] hierarchicalObjects = GetHierarchicalObjects(treeViewItems);
            if (hierarchicalObjects.Length > 1)
            {
                throw new Exception(""); // TODO
            }
            else if (hierarchicalObjects.Length == 1)
            {
                HierarchicalObject hierarchicalObject = hierarchicalObjects[0];
                _ordinaryDumpDeduplicator.DeleteDuplicate(_currentDuplicateReport, hierarchicalObject);
            }

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

        private void ViewDuplicatesByHash(Boolean hideIsolatedDuplicates, Boolean doResetForm)
        {
            if (_currentDataLocations != null && _currentDataLocations.Count > 0)
            {
                _currentDuplicateReport = _ordinaryDumpDeduplicator.GetDuplicatesFound(_currentDataLocations.ToArray());

                // TODO: Переделать на IEnumerable для оптимизации.

                HierarchicalObject[] uniqueIsolatedFiles = _currentDuplicateReport.GetUniqueIsolatedFiles();
                HierarchicalObject[] sortedUniqueIsolatedFiles = uniqueIsolatedFiles
                    .OrderByDescending(blobInfo => GetDuplicatesDataSize(blobInfo))
                    .ToArray();

                HierarchicalObject[] duplicatesByHashes = _currentDuplicateReport.GroupDuplicatesByHash();
                HierarchicalObject[] sortedDuplicatesByHashes = duplicatesByHashes
                    .OrderByDescending(blobInfo => GetDuplicatesDataSize(blobInfo))
                    .ToArray();

                Int32 uniqueIsolatedFilesCount = uniqueIsolatedFiles.Length;
                Int32 objectsCount = uniqueIsolatedFilesCount + sortedDuplicatesByHashes.Length;

                var objectsToView = new HierarchicalObject[objectsCount];
                Array.Copy(sortedUniqueIsolatedFiles, 0, objectsToView, 0, sortedUniqueIsolatedFiles.Length);
                Array.Copy(sortedDuplicatesByHashes, 0, objectsToView, uniqueIsolatedFilesCount, sortedDuplicatesByHashes.Length);

                TreeViewItem[] itemsInReport = MakeTreeViewItems(objectsToView, hideIsolatedDuplicates);

                _windowsManager.MainViewModel.SetTreeViewItems(itemsInReport);
            }
        }

        private void AddCurrentDataLocation(DataLocation dataLocation)
        {
            if (_currentDataLocations == null)
            {
                _currentDataLocations = new HashSet<DataLocation>();
            }

            _currentDataLocations.Add(dataLocation);
        }

        #endregion

        #region Private static methods

        private static TreeViewItem[] MakeTreeViewItems(HierarchicalObject[] hierarchicalObjects, Boolean hideIsolatedDuplicates)
        {
            var topLevelTreeViewItems = new List<TreeViewItem>(hierarchicalObjects.Length);
            foreach (HierarchicalObject objectInReport in hierarchicalObjects)
            {
                TreeViewItem treeViewItem = MakeTreeViewItem(objectInReport, hideIsolatedDuplicates);
                if (!treeViewItem.IsHidden)
                {
                    topLevelTreeViewItems.Add(treeViewItem);
                }
            }

            return topLevelTreeViewItems.ToArray();
        }

        /// <remarks>Логика преобразования <c>HierarchicalObject</c> в <c>TreeViewItem</c> с учетом признаков из .ObjectSort.</remarks>
        private static TreeViewItem MakeTreeViewItem(HierarchicalObject objectInReport, Boolean hideIsolatedDuplicates)
        {
            System.Drawing.Color itemColor;
            Boolean isObjectHiden;

            if (objectInReport.Sort.HasFlag(ObjectSort.Blob))
            {
                AnalyzeBlob(objectInReport, hideIsolatedDuplicates, out itemColor, out isObjectHiden);
            }
            else if (objectInReport.Sort.HasFlag(ObjectSort.FileSpecimen))
            {
                AnalyzeFile(objectInReport, hideIsolatedDuplicates, out itemColor, out isObjectHiden);
            }
            else
            {
                throw new Exception();
            }

            TreeViewItem[] childItems;
            HierarchicalObject[] childObjects = objectInReport.ChildObjects;
            if (!isObjectHiden & childObjects != null && childObjects.Length > 0)
            {
                //Boolean hideFilesFromIsolatedDuplicates = hideIsolatedDuplicates && objectInReport.Sort != ObjectSort.ContainsUniqueIsolatedFiles;
                Boolean hideFilesFromIsolatedDuplicates = hideIsolatedDuplicates;

                var childItemsList = new List<TreeViewItem>(childObjects.Length);
                foreach (var childObject in childObjects)
                {
                    TreeViewItem childItem = MakeTreeViewItem(childObject, hideFilesFromIsolatedDuplicates);
                    if (!childItem.IsHidden)
                    {
                        childItemsList.Add(childItem);
                    }
                }

                childItems = childItemsList.ToArray();
            }
            else
            {
                childItems = new TreeViewItem[] { };
            }

            var treeViewItem = new TreeViewItem(objectInReport, itemColor, childItems, isObjectHiden);
            return treeViewItem;
        }

        private static void AnalyzeBlob(HierarchicalObject objectInReport, Boolean hideIsolatedDuplicates, out System.Drawing.Color itemColor, out Boolean isObjectHiden)
        {
            if (objectInReport.Sort == ObjectSort.AllDuplicatesIsolated) // TODO: check
            {
                itemColor = System.Drawing.Color.DarkGreen;
                isObjectHiden = hideIsolatedDuplicates;
            }
            else if (objectInReport.Sort == ObjectSort.ContainsUniqueIsolatedFiles) // TODO: check
            {
                itemColor = System.Drawing.Color.Red;
                isObjectHiden = false;

                // TODO: Не скрывать чилдовые файлы совсем.
            }
            else
            {
                itemColor = System.Drawing.Color.Black;
                isObjectHiden = false;
            }
        }

        private static void AnalyzeFile(HierarchicalObject objectInReport, Boolean hideIsolatedDuplicates, out System.Drawing.Color itemColor, out Boolean isObjectHiden)
        {
            if (objectInReport.Sort.HasFlag(ObjectSort.IsUnique))
            {
                itemColor = System.Drawing.Color.Red;
                isObjectHiden = false;
            }
            else if (objectInReport.Sort.HasFlag(ObjectSort.IsolatedDuplicate))
            {
                itemColor = System.Drawing.Color.LightGreen;
                isObjectHiden = hideIsolatedDuplicates;
            }
            else // Обычный файл-дубликат.
            {
                itemColor = System.Drawing.Color.Black;
                isObjectHiden = false;
            }
        }

        private static HierarchicalObject[] GetHierarchicalObjects(TreeViewItem[] treeViewItems)
        {
            var hierarchicalObjects = new List<HierarchicalObject>(treeViewItems.Length);
            foreach (TreeViewItem treeViewItem in treeViewItems)
            {
                if (treeViewItem == null || treeViewItem.HierarchicalObject == null)
                {
                    throw new Exception("TreeViewItem is empty.");
                }

                hierarchicalObjects.Add(treeViewItem.HierarchicalObject);
            }

            return hierarchicalObjects.ToArray();
        }

        private static Int64 GetDuplicatesDataSize(HierarchicalObject objectInReport)
        {
            if (objectInReport.Type.Equals(typeof(BlobInfo)))
            {
                var blobInfo = objectInReport.Object as BlobInfo;
                Int32 blobCopiesCount = objectInReport.ChildObjects.Length;

                return blobInfo.Size * blobCopiesCount;
            }
            else
            {
                return -1;
            }
        }

        private static String TimeSpanToString(TimeSpan timeSpan)
        {
            /*
            String formatted = string.Format("{0}{1}{2}{3}",
                timeSpan.Duration().Days > 0 ? string.Format("{0:0} day{1}, ", timeSpan.Days, timeSpan.Days == 1 ? string.Empty : "s") : string.Empty,
                timeSpan.Duration().Hours > 0 ? string.Format("{0:0} hour{1}, ", timeSpan.Hours, timeSpan.Hours == 1 ? string.Empty : "s") : string.Empty,
                timeSpan.Duration().Minutes > 0 ? string.Format("{0:0} minute{1}, ", timeSpan.Minutes, timeSpan.Minutes == 1 ? string.Empty : "s") : string.Empty,
                timeSpan.Duration().Seconds > 0 ? string.Format("{0:0} second{1}", timeSpan.Seconds, timeSpan.Seconds == 1 ? string.Empty : "s") : string.Empty);

            if (formatted.EndsWith(", ")) formatted = formatted.Substring(0, formatted.Length - 2);

            if (string.IsNullOrEmpty(formatted)) formatted = "0 seconds";
            */

            const String d2 = "D2";

            Int32 hours = (Int32)Math.Floor(timeSpan.TotalHours);
            String millisecondsString = timeSpan.Milliseconds.ToString(d2).Substring(0, 2);
            String formattedString = $"{hours.ToString(d2)}:{timeSpan.Minutes.ToString(d2)}:{timeSpan.Seconds.ToString(d2)}.{millisecondsString}";

            return formattedString;
        }

        #endregion
    }
}