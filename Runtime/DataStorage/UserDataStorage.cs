using System;
using System.Text;

using Newtonsoft.Json;

using Debug = UnityEngine.Debug;

using ModIO.UserDataIOCallbacks;

namespace ModIO
{
    /// <summary>Functions for user-specific data I/O.</summary>
    public static class UserDataStorage
    {
        // ---------[ Constants ]---------
        /// <summary>Defines the i/o functions to use for this platform.</summary>
        public static readonly IUserDataIO PLATFORM_IO;

        // ---------[ Accessors ]---------
        /// <summary>Active User Data directory.</summary>
        public static string ActiveUserDirectory
        {
            get { return PLATFORM_IO.ActiveUserDirectory; }
        }

        // ---------[ Initialization ]---------
        /// <summary>Loads the platform I/O behaviour.</summary>
        static UserDataStorage()
        {
            // Select the platform appropriate functions
            #if UNITY_EDITOR
                UserDataStorage.PLATFORM_IO = new SystemIOWrapper_Editor();
            #else
                var udModuleType = System.Type.GetType(PluginSettings.data.UserDataModuleClassName);
                IUserDataIO udModuleInstance = (IUserDataIO)System.Activator.CreateInstance(udModuleType);
                UserDataStorage.PLATFORM_IO = udModuleInstance;
            #endif
        }

        /// <summary>Initializes the data storage functionality for a given user.</summary>
        public static void SetActiveUser(string platformUserId, SetActiveUserCallback<string> callback)
        {
            UserDataStorage.PLATFORM_IO.SetActiveUser(platformUserId, callback);
        }

        /// <summary>Initializes the data storage functionality for a given user.</summary>
        public static void SetActiveUser(int platformUserId, SetActiveUserCallback<int> callback)
        {
            UserDataStorage.PLATFORM_IO.SetActiveUser(platformUserId, callback);
        }

        // ---------[ I/O Interface ]---------
        /// <summary>Function for reading a user-specific file.</summary>
        public static void ReadFile(string relativePath, ReadFileCallback callback)
        {
            UserDataStorage.PLATFORM_IO.ReadFile(relativePath, callback);
        }

        /// <summary>Function used to read a user data file.</summary>
        public static void ReadJSONFile<T>(string relativePath, ReadJSONFileCallback<T> callback)
        {
            UserDataStorage.ReadFile(relativePath, (p, success, fileData) =>
            {
                T jsonObject;

                if(success)
                {
                    success = IOUtilities.TryParseUTF8JSONData(fileData, out jsonObject);
                }
                else
                {
                    jsonObject = default(T);
                }

                callback.Invoke(relativePath, success, jsonObject);
            });
        }

        /// <summary>Function for writing a user-specific file.</summary>
        public static void WriteFile(string relativePath, byte[] data, WriteFileCallback callback)
        {
            Debug.Assert(data != null);

            #if DEBUG
            if(data.Length == 0)
            {
                Debug.LogWarning("[mod.io] Writing 0-byte user file to: " + relativePath);
            }
            #endif // DEBUG

            UserDataStorage.PLATFORM_IO.WriteFile(relativePath, data, callback);
        }

        /// <summary>Function used to read a user data file.</summary>
        public static void WriteJSONFile<T>(string relativePath, T jsonObject, WriteFileCallback callback)
        {
            byte[] data = IOUtilities.GenerateUTF8JSONData<T>(jsonObject);

            if(data != null)
            {
                UserDataStorage.WriteFile(relativePath, data, callback);
            }
            else
            {
                Debug.LogWarning("[mod.io] Failed create JSON representation of object before writing file."
                                 + "\nFile: " + relativePath + "\n\n");

                if(callback != null)
                {
                    callback.Invoke(relativePath, false);
                }
            }
        }

        /// <summary>Function for deleting a user-specific file.</summary>
        public static void DeleteFile(string relativePath, DeleteFileCallback callback)
        {
            UserDataStorage.PLATFORM_IO.DeleteFile(relativePath, callback);
        }

        /// <summary>Function for clearing of the active user's data.</summary>
        public static void ClearActiveUserData(ClearActiveUserDataCallback callback)
        {
            UserDataStorage.PLATFORM_IO.ClearActiveUserData(callback);
        }
    }
}
