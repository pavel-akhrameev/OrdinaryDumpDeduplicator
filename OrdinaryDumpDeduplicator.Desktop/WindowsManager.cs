using System;
using System.Windows.Forms;

namespace OrdinaryDumpDeduplicator.Desktop
{
    internal class WindowsManager
    {
        private System.Threading.SynchronizationContext _guiSynchronizationContext;

        private MainForm _mainForm;
        private DuplicateReportForm _duplicatesForm;

        private AboutBox _aboutBox;

        #region Constructor

        public WindowsManager()
        {
        }

        public void Initialize()
        {
            this._mainForm = new MainForm();
            this._duplicatesForm = new DuplicateReportForm();
            this._aboutBox = new AboutBox();

            this._guiSynchronizationContext = System.Threading.SynchronizationContext.Current;
        }

        #endregion

        #region Public properties

        public IMainViewModel MainViewModel => _mainForm;

        public IDuplicatesViewModel DuplicatesViewModel => _duplicatesForm;

        #endregion

        #region Public methods

        public Form GetStartForm()
        {
            if (_mainForm == null)
            {
                throw new InvalidOperationException("WindowsManager is not initialized.");
            }

            return _mainForm;
        }

        public void ShowDuplicatesForm()
        {
            if (!_duplicatesForm.Visible)
            {
                _guiSynchronizationContext.Post(new System.Threading.SendOrPostCallback((Object state) => _duplicatesForm.Show(_mainForm)), null);
            }
        }

        public void HideDuplicatesForm()
        {
            _guiSynchronizationContext.Post(new System.Threading.SendOrPostCallback((Object state) => _duplicatesForm.Hide()), null);
        }

        public void ShowAboutBox()
        {
            if (!_aboutBox.Visible)
            {
                _guiSynchronizationContext.Post(new System.Threading.SendOrPostCallback((Object state) => _aboutBox.ShowDialog(_mainForm)), null);
            }
        }

        public void HideAboutBox()
        {
            _guiSynchronizationContext.Post(new System.Threading.SendOrPostCallback((Object state) => _aboutBox.Hide()), null);
        }

        public void CloseAllAdditionalForms()
        {
            _duplicatesForm.Close();
            _aboutBox.Close();
        }

        #endregion
    }
}
