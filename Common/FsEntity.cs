using System;
using System.Collections.Generic;
using System.Text;

namespace OrdinaryDumpDeduplicator.Common
{
    public abstract class FsEntity
    {
        #region Private fields

        private readonly String _name;
        private readonly Directory _parentDirectory;

        private DateTime _creationDate;
        private DateTime _modificationDate;

        #endregion

        #region Constructor

        protected FsEntity(String name, Directory parentDirectory, DateTime creationDate, DateTime modificationDate)
        {
            this._name = name;
            this._parentDirectory = parentDirectory;

            this._creationDate = creationDate;
            this._modificationDate = modificationDate;
        }

        #endregion

        #region Public properties

        public String Name
        {
            get
            {
                return _name;
            }
        }

        public Directory ParentDirectory
        {
            get
            {
                return _parentDirectory;
            }
        }

        #endregion
    }
}
