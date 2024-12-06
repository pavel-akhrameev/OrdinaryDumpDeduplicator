using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using OrdinaryDumpDeduplicator.Common;

namespace OrdinaryDumpDeduplicator
{
    public class DirectoryWithDuplicates
    {
        private readonly Directory _directory;

        private readonly HashSet<DirectoryWithDuplicates> _subDirectories;

        private readonly HashSet<FileInfo> _duplicatesFound;

        public DirectoryWithDuplicates(Directory directory, IEnumerable<DirectoryWithDuplicates> subDirectoriesWithDuplicates, /*[MaybeNull]*/ IEnumerable<FileInfo> duplicatesFound)
        {
            this._directory = directory;
            this._subDirectories = new HashSet<DirectoryWithDuplicates>(subDirectoriesWithDuplicates);

            if (duplicatesFound != null)
            {
                this._duplicatesFound = new HashSet<FileInfo>(duplicatesFound);
            }
            else
            {
                this._duplicatesFound = new HashSet<FileInfo>();
            }
        }

        public IReadOnlyCollection<DirectoryWithDuplicates> SubDirectories => _subDirectories;

        public IReadOnlyCollection<FileInfo> DuplicatesInTheDirectory => _duplicatesFound;

        public override string ToString()
        {
            return _directory.ToString();
        }
    }
}
