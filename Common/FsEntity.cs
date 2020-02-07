using System;

namespace OrdinaryDumpDeduplicator.Common
{
    public abstract class FsEntity
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
                        ? System.IO.Path.Combine(_parentDirectory.Path, _name.ToString())
                        : _name;
                }

                return _path;
            }
        }

        #endregion

        #region Overrides of object

        public override bool Equals(object obj)
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

            var isEqual = this._name.Equals(other._name) &&
                ((this._parentDirectory == null && other._parentDirectory == null) ||
                  this._parentDirectory.Equals(other._parentDirectory));
            return isEqual;
        }

        public override int GetHashCode()
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
