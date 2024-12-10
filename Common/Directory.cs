using System;
using System.Collections.Generic;

namespace OrdinaryDumpDeduplicator.Common
{
    public sealed class Directory : FsEntity
    {
        #region Private fields

        private readonly Dictionary<String, Directory> _subDirectories;

        private readonly Dictionary<String, File> _files;

        #endregion

        #region Constructors

        public Directory(String name, Directory parentDirectory)
            : base(name, parentDirectory)
        {
            this._subDirectories = new Dictionary<String, Directory>();
            this._files = new Dictionary<String, File>();
        }

        public Directory(String name, Directory parentDirectory, String fullPath)
            : this(name, parentDirectory)
        {
            SetPath(fullPath);
        }

        #endregion

        #region Public properties

        public IReadOnlyCollection<Directory> SubDirectories => _subDirectories.Values;

        public IReadOnlyCollection<File> Files => _files.Values;

        #endregion

        #region Internal methods

        internal Directory AddSubDirectory(Directory directory) // Метод используется в FsUtils
        {
            var directoryName = directory.Name;

            if (_subDirectories.TryGetValue(directoryName, out Directory subDirectory))
            {
                throw new InvalidOperationException();
            }
            else
            {
                subDirectory = directory;
                _subDirectories.Add(directoryName, subDirectory);
            }

            return subDirectory;
        }

        internal File AddFile(File file) // Метод используется в FsUtils
        {
            var fileName = file.Name;

            if (_files.TryGetValue(fileName, out File subFile))
            {
                throw new InvalidOperationException();
            }
            else
            {
                subFile = file;
                _files.Add(fileName, subFile);
            }

            return subFile;
        }

        #endregion
    }
}
