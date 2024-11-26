using System;

namespace OrdinaryDumpDeduplicator.Common
{
    /// <summary>
    /// Папка на файловой системе подлежащая сканированию или архивации.
    /// </summary>
    public class DataLocation : IEquatable<DataLocation>
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

        #region Overrides of object

        public override Boolean Equals(Object obj)
        {
            if (obj == null)
            {
                return false;
            }

            DataLocation other = obj as DataLocation;
            return Equals(other);
        }

        public Boolean Equals(DataLocation other)
        {
            if (other == null)
            {
                return false;
            }

            Boolean isEqual = this._directory.Equals(other._directory);
            return isEqual;
        }

        public override int GetHashCode()
        {
            return this._directory.GetHashCode();
        }

        public override String ToString()
        {
            return this._directory.ToString();
        }

        #endregion
    }
}
