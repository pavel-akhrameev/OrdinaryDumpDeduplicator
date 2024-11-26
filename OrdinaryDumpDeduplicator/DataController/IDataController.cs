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

        void AddDataLocation(DataLocation dataLocation);

        IReadOnlyCollection<DataLocation> GetDataLocations(IEnumerable<Directory> directories);

        IReadOnlyCollection<DataLocation> GetDataLocations();

        void AddInspection(Inspection inspection);

        void UpdateInspection(Inspection inspection);

        HashSet<Inspection> GetLastInspections(IEnumerable<DataLocation> dataLocations);

        void AddFileState(FileState fileState);

        void UpdateFileState(FileState fileState);

        void AddBlobInfo(BlobInfo blobInfo);

        Dictionary<BlobInfo, File[]> GetDuplicatesByHash(IEnumerable<DataLocation> dataLocations);

        Dictionary<BlobInfo, File[]> GetDirectoryCurrentFiles(IReadOnlyCollection<Directory> directories, Boolean includeSubDirectories);

        FileState[] GetSimilarFileStates(FileState fileState);

        IReadOnlyCollection<Directory> GetSubDirectories(HashSet<Inspection> inspections, HashSet<Directory> directories, Boolean doRecursively);

        Boolean IsFileFromDirectory(Directory directory, File file);
    }
}
