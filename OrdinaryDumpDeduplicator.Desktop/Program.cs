using System;
using System.Windows.Forms;

namespace OrdinaryDumpDeduplicator.Desktop
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var ordinaryDumpDeduplicator = new OrdinaryDumpDeduplicator();
            var ordinaryDumpDeduplicatorDesktop = new OrdinaryDumpDeduplicatorDesktop(ordinaryDumpDeduplicator);

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            ordinaryDumpDeduplicatorDesktop.Start();
            var startForm = ordinaryDumpDeduplicatorDesktop.GetStartForm();
            Application.Run(startForm);
        }
    }
}
