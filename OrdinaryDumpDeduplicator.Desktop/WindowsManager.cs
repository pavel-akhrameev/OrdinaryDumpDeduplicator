using System;
using System.Windows.Forms;

namespace OrdinaryDumpDeduplicator.Desktop
{
    internal class WindowsManager
    {
        private MainForm _mainForm;

        #region Constructor

        public WindowsManager()
        {
        }

        public void Initialize()
        {
            this._mainForm = new MainForm();
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

        #endregion
    }
}
