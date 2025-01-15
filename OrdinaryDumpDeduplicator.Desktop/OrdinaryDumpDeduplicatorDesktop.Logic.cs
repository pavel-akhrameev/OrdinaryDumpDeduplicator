using System;
using System.Collections.Generic;

using OrdinaryDumpDeduplicator.Common;

namespace OrdinaryDumpDeduplicator.Desktop
{
    partial class OrdinaryDumpDeduplicatorDesktop
    {
        #region Private fields

        private DuplicateReport _currentDuplicateReport; // We show only one DuplicateReportForm at a time.
        private DuplicatesViewController _currentDuplicatesViewController;

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
            mainViewModel.AddSessionMessage("Scan started.");
            DateTime now = DateTime.Now;

            System.Threading.Tasks.Task<DataLocation> doInspectionTask = _ordinaryDumpDeduplicator.DoInspection(dataLocation);
            doInspectionTask.ContinueWith(inspectionTask =>
            {
                DataLocation currentDataLocation = inspectionTask.Result;

                TimeSpan timeSpent = DateTime.Now.Subtract(now);
                String timeSpentString = TimeSpanToString(timeSpent);
                mainViewModel.AddSessionMessage($"Scan finished in {timeSpentString}.");

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
                    //_windowsManager.MainViewModel.AddSessionMessage($"Duplicates search completed in {timeSpentString}.");

                    _currentDuplicatesViewController = new DuplicatesViewController(_ordinaryDumpDeduplicator, duplicateReport, _windowsManager.DuplicatesViewModel);
                    _currentDuplicatesViewController.ViewDuplicatesByHash(hideIsolatedDuplicates, doResetForm);
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

                    _currentDuplicatesViewController = new DuplicatesViewController(_ordinaryDumpDeduplicator, duplicateReport, _windowsManager.DuplicatesViewModel);
                    _currentDuplicatesViewController.ViewDuplicatesByFolders(hideIsolatedDuplicates);
                    _windowsManager.ShowDuplicatesForm();
                });
            }
        }

        private void MoveToDuplicatesRequested(ItemToView[] treeViewItems)
        {
            _currentDuplicatesViewController.MoveToDuplicates(treeViewItems);
        }

        private void DeleteDuplicatesRequested(ItemToView[] treeViewItems)
        {
            _currentDuplicatesViewController.DeleteDuplicates(treeViewItems);
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

        private static DataLocation[] GetDataLocations(IReadOnlyCollection<ItemToView> itemsToView) // TODO: optimize
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

        private static String TimeSpanToString(TimeSpan timeSpan)
        {
            const String d2 = "D2";

            Int32 hours = (Int32)Math.Floor(timeSpan.TotalHours);
            String millisecondsString = timeSpan.Milliseconds.ToString(d2).Substring(0, 2);
            String formattedString = $"{hours.ToString(d2)}:{timeSpan.Minutes.ToString(d2)}:{timeSpan.Seconds.ToString(d2)}.{millisecondsString}";

            return formattedString;
        }

        #endregion
    }
}
