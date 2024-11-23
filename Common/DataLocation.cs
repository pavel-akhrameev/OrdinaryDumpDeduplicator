using System;

namespace OrdinaryDumpDeduplicator.Common
{
    /// <summary>
    /// Папка на файловой системе подлежащая сканированию или архивации.
    /// </summary>
    public class DataLocation
    {
        private readonly Guid _id;
        private readonly String _dataLocationPath;

        private Directory _directory;

        public Directory Directory { get { return _directory; } }

        public String Path { get { return _dataLocationPath; } }

        public DataLocation(Directory directory)
        {
            this._directory = directory;
            this._dataLocationPath = directory.Path;
        }

        public DataLocation(String dataLocationPath)
        {
            this._dataLocationPath = dataLocationPath;
        }
    }
}
