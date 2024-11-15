using System;
using System.Collections.Generic;
using System.Linq;

using OrdinaryDumpDeduplicator.Common;

namespace OrdinaryDumpDeduplicator
{
    public class OrdinaryDumpDeduplicator
    {
        private readonly DuplicatesProcessor _duplicatesProcessor;
        private readonly DataControllerSimple _dataController;
        private readonly FileSystemProvider _fileSystemProvider;

        private DataLocation _currentDataLocation;

        #region Constructor and initializer

        public OrdinaryDumpDeduplicator()
        {
            this._dataController = new DataControllerSimple();
            this._fileSystemProvider = new FileSystemProvider();
            this._duplicatesProcessor = new DuplicatesProcessor(_dataController, _fileSystemProvider);
            this._currentDataLocation = null;
        }

        public void Initialize()
        {
            _dataController.Initialize();
        }

        #endregion

        #region Public methods

        public IReadOnlyCollection<DataLocation> AddDataLocation(String directoryPath)
        {
            _fileSystemProvider.CheckPathValid(directoryPath);

            var directory = _fileSystemProvider.GetDirectoryInfo(directoryPath, parentDirectory: null);
            _dataController.AddDirectory(directory);

            DataLocation newDataLocation = GetDataLocation(directory);
            IReadOnlyCollection<DataLocation> dataLocations = _dataController.GetDataLocations();

            return dataLocations;
        }

        public DataLocation DoInspection(DataLocation dataLocation)
        {
            _currentDataLocation = null;
            var inspection = new Inspection(dataLocation, DateTime.Now);
            _dataController.AddInspection(inspection);

            FsUtils.IndexSubDirectoriesAndFiles(
               dataLocation.Path,
               out Directory rootDirectory,
               out IReadOnlyCollection<Directory> fsFolders,
               out IReadOnlyCollection<File> fsFiles);

            _dataController.AddDirectory(rootDirectory); // Тут просто сохраняем структуру папок-файлов в БД.

            // Собрать атрибуты всех файлов, включая размер. Сохранить FileState в статусе Unknown или Error (если не получилось доcтать атрибуты).
            IReadOnlyCollection<FileState> statesOfFiles = GetAttributesOfFiles(fsFiles, inspection);

            CountFilesAndSize(statesOfFiles, out Int32 filesCount, out Int64 dataSize);

            // Получить и сохранить хеши всех файлов. Обновить FileState до New.
            ComputeHashesOfFiles(statesOfFiles);

            inspection.FinishInspection(DateTime.Now);
            _dataController.UpdateInspection(inspection);

            _currentDataLocation = dataLocation;
            return _currentDataLocation;
        }

        public void ComputeHashesOfFiles(IReadOnlyCollection<FileState> statesOfFiles)
        {
            foreach (FileState fileState in statesOfFiles)
            {
                FileStatus fileStatus;
                BlobInfo blobInfo;

                try
                {
                    blobInfo = ComputeAndSaveBlobInfo(fileState.File.Path);
                    fileStatus = FileStatus.New;
                }
                catch (Exception ex)
                {
                    blobInfo = BlobInfo.BrokenBlobInfo;
                    fileStatus = FileStatus.Unreadable;
                }

                fileState.SetStatusAndBlobInfo(fileStatus, blobInfo);
                _dataController.UpdateFileState(fileState);
            }
        }

        public BlobInfo ComputeAndSaveBlobInfo(String filePath)
        {
            BlobInfo blobInfo;

            try
            {
                Byte[] sha1HashSum;
                Int64 fileLength;
                using (System.IO.FileStream fileStream = _fileSystemProvider.GetFileStream(filePath))
                {
                    sha1HashSum = FsUtils.ComputeSha1Hash(fileStream, out fileLength);
                }

                blobInfo = BlobInfo.Create(fileLength, sha1HashSum);
            }
            catch (Exception ex)
            {
                blobInfo = BlobInfo.BrokenBlobInfo;
            }

            _dataController.AddBlobInfo(blobInfo);
            return blobInfo;
        }

        public DuplicateReport GetDuplicatesFound(IReadOnlyCollection<DataLocation> dataLocations)
        {
            DuplicateReport duplicateReport = _duplicatesProcessor.GetDuplicatesFound(dataLocations);
            return duplicateReport;
        }

        public void MoveKnownDuplicatesToSpecialFolder(DuplicateReport duplicateReport, HierarchicalObject[] hierarchicalObjects)
        {
            _duplicatesProcessor.MoveKnownDuplicatesToSpecialFolder(duplicateReport, hierarchicalObjects);
        }

        public void DeleteDuplicate(DuplicateReport duplicateReport, HierarchicalObject[] hierarchicalObjects)
        {
            var fileObjectsToProcess = GetFiles(hierarchicalObjects);
            _duplicatesProcessor.DeleteDuplicate(duplicateReport, fileObjectsToProcess);
        }

        #endregion

        #region Private methods

        private IReadOnlyCollection<FileState> GetAttributesOfFiles(IReadOnlyCollection<File> files, Inspection inspection)
        {
            var result = new List<FileState>(files.Count);

            foreach (File file in files)
            {
                FileState fileState;

                try
                {
                    System.IO.FileInfo fileInfo = _fileSystemProvider.GetFileInfo(file);

                    var status = FileStatus.Unknown;
                    fileState = new FileState(file, inspection, previousState: null, fileInfo.Length, status, fileInfo.CreationTime, fileInfo.LastWriteTime);
                }
                catch (Exception ex)
                {
                    // TODO: log exception.

                    var blobInfo = BlobInfo.BrokenBlobInfo;
                    var status = FileStatus.Error;
                    var currentDateTime = DateTime.Now;
                    fileState = new FileState(file, inspection, previousState: null, size: -1, status, creationDate: currentDateTime, modificationDate: currentDateTime, blobInfo);
                }

                _dataController.AddFileState(fileState);
                result.Add(fileState);
            }

            return result;
        }

        private DataLocation GetDataLocation(Directory directory)
        {
            DataLocation dataLocation;
            IReadOnlyCollection<DataLocation> dataLocations = _dataController.GetDataLocations(new[] { directory });
            if (dataLocations.Count > 0)
            {
                dataLocation = dataLocations.First();
            }
            else
            {
                dataLocation = new DataLocation(directory);
                _dataController.AddDataLocation(dataLocation);
            }

            return dataLocation;
        }

        /// <remarks>Counts only available files.</remarks>
        private void CountFilesAndSize(IReadOnlyCollection<FileState> statesOfFiles, out Int32 filesCount, out Int64 dataSize)
        {
            filesCount = 0;
            dataSize = 0;
            foreach (FileState fileState in statesOfFiles)
            {
                if (fileState.Status != FileStatus.Error &&
                    fileState.Status != FileStatus.Unreadable &&
                    fileState.Size > 0)
                {
                    filesCount++;
                    dataSize += fileState.Size;
                }
            }
        }

        private static File[] GetFiles(IReadOnlyCollection<HierarchicalObject> hierarchicalObjects)
        {
            var files = new List<File>(hierarchicalObjects.Count);
            foreach (HierarchicalObject hierarchicalObject in hierarchicalObjects)
            {
                if (hierarchicalObject == null || hierarchicalObject.Object == null)
                {
                    throw new Exception("HierarchicalObject is empty."); // TODO
                }
                else if (hierarchicalObject.Type == typeof(File))
                {
                    var file = hierarchicalObject.Object as File;
                    files.Add(file);
                }
                else
                {
                    throw new ArgumentException($"Unknown type of wrapped object found '{hierarchicalObject}'");
                }
            }

            return files.ToArray();
        }

        #endregion
    }
}
