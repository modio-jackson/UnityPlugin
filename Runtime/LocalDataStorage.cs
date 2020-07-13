using System;
using System.Text;
using System.Collections.Generic;

using UnityEngine;

using Newtonsoft.Json;

using ModIO.API;
using ModIO.PlatformIOCallbacks;

namespace ModIO
{
    /// <summary>An interface for storing/loading mod.io data on disk.</summary>
    public static class LocalDataStorage
    {
        // ---------[ Constants ]---------
        /// <summary>Defines the I/O functions to use for this platform.</summary>
        public static readonly IPlatformIO PLATFORM_IO;

        /// <summary>Defines the I/O functions to use for this platform.</summary>
        public static readonly IPlatformIO_Async TEMP_PLATFORM_IO_ASYNC;

        // ---------[ Initialization ]---------
        /// <summary>Loads the platform I/O behaviour.</summary>
        static LocalDataStorage()
        {
            // Selects the platform appropriate functions
            #if UNITY_EDITOR
                LocalDataStorage.PLATFORM_IO = new SystemIOWrapper_Editor();
                LocalDataStorage.TEMP_PLATFORM_IO_ASYNC = new SystemIOWrapper_Editor();
            #else
                LocalDataStorage.PLATFORM_IO = new SystemIOWrapper();
                LocalDataStorage.TEMP_PLATFORM_IO_ASYNC = new SystemIOWrapper();
            #endif

        }

        // ---------[ Data Management Interface ]---------
        // ------ File I/O ------
        /// <summary>Reads a file.</summary>
        public static void ReadFile(string path, ReadFileCallback onComplete)
        {
            LocalDataStorage.TEMP_PLATFORM_IO_ASYNC.ReadFile(path, onComplete);
        }

        /// <summary>Reads a file and parses the data as a JSON object instance.</summary>
        public static void ReadJSONFile<T>(string path, ReadJSONFileCallback<T> onComplete)
        {
            Debug.Assert(onComplete != null);

            LocalDataStorage.TEMP_PLATFORM_IO_ASYNC.ReadFile(path, (p, success, data) =>
            {
                T jsonObject;

                if(success)
                {
                    success = IOUtilities.TryParseUTF8JSONData<T>(data, out jsonObject);

                    if(!success)
                    {
                        Debug.LogWarning("[mod.io] Failed parse file content as JSON Object."
                                         + "\nFile: " + path + "\n\n");
                    }
                }
                else
                {
                    jsonObject = default(T);
                }

                if(onComplete != null)
                {
                    onComplete.Invoke(path, success, jsonObject);
                }
            });
        }

        /// <summary>Writes a file.</summary>
        public static void WriteFile(string path, byte[] data, WriteFileCallback onComplete)
        {
            #if DEBUG
            if(data.Length == 0)
            {
                Debug.Log("[mod.io] Writing 0-byte file to: " + path);
            }
            #endif // DEBUG

            LocalDataStorage.TEMP_PLATFORM_IO_ASYNC.WriteFile(path, data, onComplete);
        }

        /// <summary>Writes a JSON file.</summary>
        public static void WriteJSONFile<T>(string path, T jsonObject, WriteFileCallback onComplete)
        {
            byte[] data = IOUtilities.GenerateUTF8JSONData<T>(jsonObject);

            if(data != null && data.Length > 0)
            {
                LocalDataStorage.TEMP_PLATFORM_IO_ASYNC.WriteFile(path, data, onComplete);
            }
            else
            {
                Debug.LogWarning("[mod.io] Failed create JSON representation of object before writing file."
                                 + "\nFile: " + path + "\n\n");

                if(onComplete != null)
                {
                    onComplete.Invoke(path, false);
                }
            }
        }

        // ------ File Management ------
        /// <summary>Deletes a file.</summary>
        public static void DeleteFile(string path, DeleteFileCallback onComplete)
        {
            LocalDataStorage.TEMP_PLATFORM_IO_ASYNC.DeleteFile(path, onComplete);
        }

        /// <summary>Moves a file.</summary>
        public static void MoveFile(string source, string destination, MoveFileCallback onComplete)
        {
            LocalDataStorage.TEMP_PLATFORM_IO_ASYNC.MoveFile(source, destination, onComplete);
        }

        /// <summary>Checks for the existence of a file.</summary>
        public static void GetFileExists(string path, GetFileExistsCallback onComplete)
        {
            LocalDataStorage.TEMP_PLATFORM_IO_ASYNC.GetFileExists(path, onComplete);
        }

        /// <summary>Gets the size and md5 hash of a file.</summary>
        public static void GetFileSizeAndHash(string path, GetFileSizeAndHashCallback onComplete)
        {
            LocalDataStorage.TEMP_PLATFORM_IO_ASYNC.GetFileSizeAndHash(path, onComplete);
        }

        /// <summary>Gets the files at a location.</summary>
        public static void GetFiles(string path, string nameFilter, bool recurseSubdirectories,
                                    GetFilesCallback onComplete)
        {
            LocalDataStorage.TEMP_PLATFORM_IO_ASYNC.GetFiles(path, nameFilter, recurseSubdirectories, onComplete);
        }

        // ------ Directory Management ------
        /// <summary>Creates a directory.</summary>
        public static void CreateDirectory(string path, CreateDirectoryCallback onComplete)
        {
            LocalDataStorage.TEMP_PLATFORM_IO_ASYNC.CreateDirectory(path, onComplete);
        }

        /// <summary>Deletes a directory.</summary>
        public static void DeleteDirectory(string path, DeleteDirectoryCallback onComplete)
        {
            LocalDataStorage.TEMP_PLATFORM_IO_ASYNC.DeleteDirectory(path, onComplete);
        }

        /// <summary>Moves a directory.</summary>
        public static void MoveDirectory(string source, string destination, MoveDirectoryCallback onComplete)
        {
            LocalDataStorage.TEMP_PLATFORM_IO_ASYNC.MoveDirectory(source, destination, onComplete);
        }

        /// <summary>Checks for the existence of a directory.</summary>
        public static void GetDirectoryExists(string path, GetDirectoryExistsCallback onComplete)
        {
            LocalDataStorage.TEMP_PLATFORM_IO_ASYNC.GetDirectoryExists(path, onComplete);
        }

        /// <summary>Gets a list of directories found at the given location.</summary>
        public static void GetDirectories(string path, GetDirectoriesCallback onComplete)
        {
            LocalDataStorage.TEMP_PLATFORM_IO_ASYNC.GetDirectories(path, onComplete);
        }
    }
}
