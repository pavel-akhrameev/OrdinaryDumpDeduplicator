using System;

namespace OrdinaryDumpDeduplicator.Common
{
    /// <summary>
    /// Папка на файловой системе подлежащая сканированию или архивации.
    /// </summary>
    public class DataLocation
    {
        private readonly String _dataLocationPath;

        private readonly Directory _directory;

        public Directory Directory => _directory;

        public String Path => _dataLocationPath;

        public DataLocation(Directory directory)
        {
            this._directory = directory;
            this._dataLocationPath = directory.Path;
        }
    }
}
