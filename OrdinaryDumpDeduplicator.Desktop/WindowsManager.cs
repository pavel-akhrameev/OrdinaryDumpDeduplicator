using System;
using System.Windows.Forms;

namespace OrdinaryDumpDeduplicator.Desktop
{
    internal class WindowsManager
    {
        private MainForm _mainForm;
        private AboutBox _aboutBox;

        #region Constructor

        public WindowsManager()
        {
        }

        public void Initialize()
        {
            this._mainForm = new MainForm();
            this._aboutBox = new AboutBox();
        }

        #endregion

        #region Public properties

        public IMainViewModel MainViewModel => _mainForm;

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
            _aboutBox.Close();
        }

        #endregion
    }
}
