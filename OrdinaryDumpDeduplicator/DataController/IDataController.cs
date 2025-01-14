using System;
using System.Collections.Generic;

using OrdinaryDumpDeduplicator.Common;

namespace OrdinaryDumpDeduplicator
{
    public interface IDataController
    {
        void AddFile(File file);

        void RemoveFile(File file);

        void AddDirectory(Directory directory);

        Directory FindDirectory(Directory parentDirectory, String name);

        Directory FindDirectory(String directoryPath);

        void AddDataLocation(DataLocation dataLocation);

        IReadOnlyCollection<DataLocation> GetDataLocations(IEnumerable<Directory> directories);

        IReadOnlyCollection<DataLocation> GetDataLocations();

        void AddInspection(Inspection inspection);

        void UpdateInspection(Inspection inspection);

        HashSet<Inspection> GetLastInspections(IEnumerable<DataLocation> dataLocations);

        void AddFileState(FileState fileState);

        void UpdateFileState(FileState fileState);

        FileState GetFileState(File file, Inspection inspection);

        FileState GetLastFileState(File file);

        void AddBlobInfo(BlobInfo blobInfo);

        IReadOnlyCollection<FileInfo> GetDuplicates(IEnumerable<DataLocation> dataLocations);

        IReadOnlyCollection<FileInfo> GetDirectoryCurrentFiles(IReadOnlyCollection<Directory> directories, Boolean includeSubDirectories);

        IReadOnlyCollection<Directory> GetSubDirectories(HashSet<Inspection> inspections, HashSet<Directory> directories, Boolean doRecursively);

        Boolean IsFileFromDirectory(Directory directory, File file);
    }
}
