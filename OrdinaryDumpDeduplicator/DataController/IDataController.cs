using System;
using System.Collections.Generic;
using OrdinaryDumpDeduplicator.Common;

namespace OrdinaryDumpDeduplicator
{
    public interface IDataController
    {
        IReadOnlyCollection<File> GetFilesOfDirectory(Directory directory, Boolean includeSubDirectories);

        void AddFile(File file);

        void AddDirectory(Directory directory);

        IReadOnlyCollection<DataLocation> GetDataLocations(IEnumerable<Directory> directories);

        void AddDataLocation(DataLocation dataLocation);

        void AddInspection(Inspection inspection);

        void UpdateInspection(Inspection inspection);

        FileState[] GetSimilarFileStates(FileState fileState);

        void AddFileState(FileState fileState);

        void UpdateFileState(FileState fileState);

        Dictionary<BlobInfo, File[]> GetDuplicatesByHash(IEnumerable<DataLocation> dataLocations);

        Dictionary<BlobInfo, File[]> GetDirectoryCurrentFiles(IReadOnlyCollection<Directory> directories, Boolean includeSubDirectories);

        Boolean IsFileFromDirectory(Directory directory, File file);
    }
}
