using System;
using System.Collections.Generic;

using OrdinaryDumpDeduplicator.Common;

namespace OrdinaryDumpDeduplicator
{
    internal static class DataStructureHelper
    {
        /// <summary>
        /// Получить директории 'duplicates found' для каждой из <paramref name="dataLocations"/>.
        /// </summary>
        /// <remarks></remarks>
        public static HashSet<Directory> GetDirectoriesForIsolatedDuplicates(IReadOnlyCollection<DataLocation> dataLocations)
        {
            var directoriesForIsolatedDuplicates = new HashSet<Directory>();
            foreach (DataLocation dataLocation in dataLocations)
            {
                Boolean containsDirectoryForDuplicates = DataStructureHelper.TryFindDirectoryForDuplicates(dataLocation, out Directory directoryForDuplicates);
                if (containsDirectoryForDuplicates)
                {
                    // А есть ли смысл тут проверять её наличие на файловой системе
                    // (!) Файловую систему мы трогаем только во время инспекции.
                    // Вот тут получить её из БД.
                    directoriesForIsolatedDuplicates.Add(directoryForDuplicates);
                }
            }

            return directoriesForIsolatedDuplicates;
        }

        public static Boolean TryFindDirectoryForDuplicates(DataLocation dataLocation, out Directory directoryForDuplicates)
        {
            String duplicatesFolderPath = System.IO.Path.Combine(dataLocation.Path, DuplicatesProcessor.FOLDER_NAME_FOR_DUPLICATES);

            Boolean duplicatesFolderExists = System.IO.Directory.Exists(duplicatesFolderPath);
            if (duplicatesFolderExists)
            {
                var directoryInfo = new System.IO.DirectoryInfo(duplicatesFolderPath);

                directoryForDuplicates = new Directory(directoryInfo.Name, dataLocation.Directory); // TODO: check
            }
            else
            {
                directoryForDuplicates = null;
            }

            return duplicatesFolderExists;
        }
    }
}
