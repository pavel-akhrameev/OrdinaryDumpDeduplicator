using System;
using System.Windows.Forms;

namespace OrdinaryDumpDeduplicator.Desktop
{
    internal partial class OrdinaryDumpDeduplicatorDesktop
    {
        private readonly OrdinaryDumpDeduplicator _ordinaryDumpDeduplicator;
        private readonly WindowsManager _windowsManager;

        public OrdinaryDumpDeduplicatorDesktop(OrdinaryDumpDeduplicator ordinaryDumpDeduplicator)
        {
            this._ordinaryDumpDeduplicator = ordinaryDumpDeduplicator;
            this._windowsManager = new WindowsManager();
        }

        public void Start()
        {
            _ordinaryDumpDeduplicator.Initialize();
            _windowsManager.Initialize();

            SubscribeToEvents();
        }

        public Form GetStartForm()
        {
            var mainForm = _windowsManager.GetStartForm();
            return mainForm;
        }
    }
}
