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

        private Directory _rootDirectory;

        public Directory RootDirectory { get { return _rootDirectory; } }

        public String Path { get { return _dataLocationPath; } }

        public DataLocation(Directory rootDirectory)
        {
            this._rootDirectory = rootDirectory;
            this._dataLocationPath = rootDirectory.Path;
        }

        public DataLocation(String dataLocationPath)
        {
            this._dataLocationPath = dataLocationPath;
        }

        [Obsolete]
        public void SetRootDirectory(Directory directory)
        {
            this._rootDirectory = directory;
        }
    }
}
