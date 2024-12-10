using System;

namespace OrdinaryDumpDeduplicator.Common
{
    public abstract class FsEntity: IEquatable<FsEntity>
    {
        #region Private fields

        private readonly String _name;

        private readonly Directory _parentDirectory;

        private String _path;

        #endregion

        #region Constructor

        internal FsEntity(String name, Directory parentDirectory)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("name");
            }

            this._name = name;
            this._parentDirectory = parentDirectory;
        }

        #endregion

        #region Public properties

        public String Name => _name;

        public Directory ParentDirectory => _parentDirectory;

        public String Path
        {
            get
            {
                if (_path == null)
                {
                    _path = (_parentDirectory != null)
                        ? System.IO.Path.Combine(_parentDirectory.Path, _name)
                        : _name;
                }

                return _path;
            }
        }

        #endregion

        #region Overrides of object

        public override Boolean Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var other = obj as FsEntity;
            if (other == null)
            {
                return false;
            }

            return Equals(other);
        }

        public Boolean Equals(FsEntity other)
        {
            var isEqual = other != null && this._name.Equals(other._name) &&
                ((this._parentDirectory != null && this._parentDirectory.Equals(other._parentDirectory)) ||
                    this._parentDirectory == null && other._parentDirectory == null);

            return isEqual;
        }

        public override Int32 GetHashCode()
        {
            var hashCode = _parentDirectory != null
                ? _name.GetHashCode() ^ _parentDirectory.GetHashCode()
                : _name.GetHashCode();

            return hashCode;
        }

        public override String ToString()
        {
            return this._name;
        }

        #endregion

        #region Private methods

        protected void SetPath(String fullPath)
        {
            this._path = fullPath;
        }

        #endregion
    }
}
