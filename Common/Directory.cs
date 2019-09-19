using System;
using System.Collections.Generic;

namespace OrdinaryDumpDeduplicator.Common
{
    public class Directory : FsEntity
    {
        #region Private fields

        private HashSet<Directory> _subFolders;
        private HashSet<File> _files;

        #endregion

        #region Constructors

        public Directory(String name, Directory parentDirectory, DateTime creationDate, DateTime modificationDate)
            : base(name, parentDirectory, creationDate, modificationDate)
        {
        }

        public Directory(
            String name,
            Directory parentDirectory,
            IEnumerable<Directory> subDirectories,
            IEnumerable<File> files,
            DateTime creationDate,
            DateTime modificationDate)
            : this(name, parentDirectory, creationDate, modificationDate)
        {
            this._subFolders = new HashSet<Directory>(subDirectories);
            this._files = new HashSet<File>(files);
        }

        #endregion
    }
}
