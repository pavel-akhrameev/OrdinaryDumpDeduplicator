using System;
using System.Collections.Generic;

namespace OrdinaryDumpDeduplicator.Common
{
    public sealed class Directory : FsEntity
    {
        #region Private fields

        private readonly Int32? _id;

        private readonly Dictionary<string, Directory> _subDirectories;

        private readonly Dictionary<string, File> _files;

        #endregion

        #region Constructors

        public Directory(String name, Directory parentDirectory, Int32? id = null)
            : base(name, parentDirectory)
        {
            this._subDirectories = new Dictionary<string, Directory>();
            this._files = new Dictionary<string, File>();
            this._id = id;
        }

        public Directory(String name, Directory parentDirectory, String fullPath, Int32? id = null)
            : this(name, parentDirectory, id)
        {
            SetPath(fullPath);
        }

        #endregion

        #region Public properties

        public Int32? Id => _id;

        public IReadOnlyCollection<Directory> SubDirectories => _subDirectories.Values;

        public IReadOnlyCollection<File> Files => _files.Values;

        #endregion

        #region Public methods

        public bool TryGetDirectory(String directoryName, out Directory directory)
        {
            var result = _subDirectories.TryGetValue(directoryName, out directory);
            return result;
        }

        public bool TryGetFile(String fileName, out File file)
        {
            var result = _files.TryGetValue(fileName, out file);
            return result;
        }

        internal Directory AddSubDirectory(Directory directory) // Метод используется в FsUtils
        {
            var directoryName = directory.Name;

            Directory subDirectory;
            if (_subDirectories.TryGetValue(directoryName, out subDirectory))
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

            File subFile;
            if (_files.TryGetValue(fileName, out subFile))
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
