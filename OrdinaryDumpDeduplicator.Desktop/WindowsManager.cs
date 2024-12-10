using System;
using System.Windows.Forms;

namespace OrdinaryDumpDeduplicator.Desktop
{
    internal class WindowsManager
    {
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
                _duplicatesForm.Show(_mainForm);
            }
        }

        public void HideDuplicatesForm()
        {
            _duplicatesForm.Hide();
        }

        public void ShowAboutBox()
        {
            if (!_aboutBox.Visible)
            {
                _aboutBox.ShowDialog(_mainForm);
            }
        }

        public void HideAboutBox()
        {
            _aboutBox.Hide();
        }

        public void CloseAllAdditionalForms()
        {
            _duplicatesForm.Close();
            _aboutBox.Close();
        }

        #endregion
    }
}
