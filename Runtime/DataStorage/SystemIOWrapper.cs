using System;
using System.Collections.Generic;
using System.IO;

using Debug = UnityEngine.Debug;

namespace ModIO
{
    /// <summary>Wraps the System.IO functionality in an IPlatformIO class.</summary>
    public class SystemIOWrapper : IPlatformIO, IUserDataIO
    {
        // ---------[ IPlatformIO Interface ]---------
        // --- File I/O ---
        /// <summary>Reads a file.</summary>
        bool IPlatformIO.ReadFile(string path, out byte[] data)
        {
            return this.ReadFile(path, out data);
        }

        /// <summary>Writes a file.</summary>
        bool IPlatformIO.WriteFile(string path, byte[] data)
        {
            return this.WriteFile(path, data);
        }

        // --- File Management ---
        /// <summary>Deletes a file.</summary>
        bool IPlatformIO.DeleteFile(string path)
        {
            return this.DeleteFile(path);
        }

        /// <summary>Moves a file.</summary>
        bool IPlatformIO.MoveFile(string source, string destination)
        {
            return this.MoveFile(source, destination);
        }

        /// <summary>Gets the size of a file.</summary>
        bool IPlatformIO.GetFileExists(string path)
        {
            return this.GetFileExists(path);
        }

        /// <summary>Gets the size of a file.</summary>
        Int64 IPlatformIO.GetFileSize(string path)
        {
            return this.GetFileSize(path);
        }

        /// <summary>Gets the size and md5 hash of a file.</summary>
        bool IPlatformIO.GetFileSizeAndHash(string path, out Int64 byteCount, out string md5Hash)
        {
            return this.GetFileSizeAndHash(path, out byteCount, out md5Hash);
        }

        /// <summary>Gets the files at a location.</summary>
        IList<string> IPlatformIO.GetFiles(string path, string nameFilter, bool recurseSubdirectories)
        {
            return this.GetFiles(path, nameFilter, recurseSubdirectories);
        }

        // --- Directory Management ---
        /// <summary>Creates a directory.</summary>
        bool IPlatformIO.CreateDirectory(string path)
        {
            return this.CreateDirectory(path);
        }

        /// <summary>Deletes a directory.</summary>
        bool IPlatformIO.DeleteDirectory(string path)
        {
            return this.DeleteDirectory(path);
        }

        /// <summary>Moves a directory.</summary>
        bool IPlatformIO.MoveDirectory(string source, string destination)
        {
            return this.MoveDirectory(source, destination);
        }

        /// <summary>Checks for the existence of a directory.</summary>
        bool IPlatformIO.GetDirectoryExists(string path)
        {
            return this.GetDirectoryExists(path);
        }

        /// <summary>Gets the sub-directories at a location.</summary>
        IList<string> IPlatformIO.GetDirectories(string path)
        {
            return this.GetDirectories(path);
        }

        // ---------[ IUserDataIO Interface ]---------
        /// <summary>The directory for the active user's data.</summary>
        public string userDir = PluginSettings.USER_DIRECTORY;

        // --- Initialization ---
        /// <summary>Initializes the storage system for the given user.</summary>
        public virtual void SetActiveUser(string platformUserId, UserDataIOCallbacks.SetActiveUserCallback<string> callback)
        {
            this.userDir = this.GenerateActiveUserDirectory(platformUserId);

            bool success = this.CreateDirectory(this.userDir);
            if(callback != null)
            {
                callback.Invoke(platformUserId, success);
            }
        }

        /// <summary>Initializes the storage system for the given user.</summary>
        public virtual void SetActiveUser(int platformUserId, UserDataIOCallbacks.SetActiveUserCallback<int> callback)
        {
            this.userDir = this.GenerateActiveUserDirectory(platformUserId.ToString("x8"));

            bool success = this.CreateDirectory(this.userDir);
            if(callback != null)
            {
                callback.Invoke(platformUserId, success);
            }
        }

        /// <summary>Determines the user directory for a given user id..</summary>
        protected virtual string GenerateActiveUserDirectory(string platformUserId)
        {
            string userDir = PluginSettings.USER_DIRECTORY;

            if(!string.IsNullOrEmpty(platformUserId))
            {
                string folderName = IOUtilities.MakeValidFileName(platformUserId);
                userDir = IOUtilities.CombinePath(PluginSettings.USER_DIRECTORY, folderName);
            }

            return userDir;
        }

        /// <summary>Deletes all of the active user's data.</summary>
        void IUserDataIO.ClearActiveUserData(UserDataIOCallbacks.ClearActiveUserDataCallback callback)
        {
            bool success = this.DeleteDirectory(this.userDir);

            if(callback != null)
            {
                callback.Invoke(success);
            }
        }

        // --- File I/O ---
        /// <summary>Reads a file.</summary>
        void IUserDataIO.ReadFile(string relativePath, UserDataIOCallbacks.ReadFileCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(relativePath));
            Debug.Assert(callback != null);

            string path = IOUtilities.CombinePath(this.userDir, relativePath);
            byte[] data;
            bool success = this.ReadFile(path, out data);

            callback.Invoke(relativePath, success, data);
        }

        /// <summary>Writes a file.</summary>
        void IUserDataIO.WriteFile(string relativePath, byte[] data, UserDataIOCallbacks.WriteFileCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(relativePath));
            Debug.Assert(data != null);

            string path = IOUtilities.CombinePath(this.userDir, relativePath);
            bool success = this.WriteFile(path, data);

            if(callback != null) { callback.Invoke(relativePath, success); }
        }

        // --- File Management ---
        /// <summary>Deletes a file.</summary>
        void IUserDataIO.DeleteFile(string relativePath, UserDataIOCallbacks.DeleteFileCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(relativePath));

            string path = IOUtilities.CombinePath(this.userDir, relativePath);
            bool success = this.DeleteFile(path);

            if(callback != null) { callback.Invoke(relativePath, success); }
        }

        /// <summary>Checks for the existence of a file.</summary>
        void IUserDataIO.GetFileExists(string relativePath, UserDataIOCallbacks.GetFileExistsCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(relativePath));
            Debug.Assert(callback != null);

            string path = IOUtilities.CombinePath(this.userDir, relativePath);
            bool doesExist = this.GetFileExists(path);

            callback.Invoke(relativePath, doesExist);
        }

        /// <summary>Gets the size of a file.</summary>
        void IUserDataIO.GetFileSize(string relativePath, UserDataIOCallbacks.GetFileSizeCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(relativePath));
            Debug.Assert(callback != null);

            string path = IOUtilities.CombinePath(this.userDir, relativePath);
            Int64 byteCount = this.GetFileSize(path);

            callback.Invoke(relativePath, byteCount);
        }

        /// <summary>Gets the size and md5 hash of a file.</summary>
        void IUserDataIO.GetFileSizeAndHash(string relativePath, UserDataIOCallbacks.GetFileSizeAndHashCallback callback)
        {
            Debug.Assert(!string.IsNullOrEmpty(relativePath));
            Debug.Assert(callback != null);

            string path = IOUtilities.CombinePath(this.userDir, relativePath);
            Int64 byteCount;
            string md5Hash;
            bool success = this.GetFileSizeAndHash(path, out byteCount, out md5Hash);

            callback.Invoke(relativePath, success, byteCount, md5Hash);
        }

        // ---------[ File I/O Functionality ]---------
        /// <summary>Reads a file.</summary>
        public virtual bool ReadFile(string path, out byte[] data)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            if(!File.Exists(path))
            {
                data = null;
                return false;
            }

            try
            {
                data = File.ReadAllBytes(path);
                return true;
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to read file.\nFile: " + path + "\n\n");
                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));

                data = null;
                return false;
            }
        }

        /// <summary>Writes a file.</summary>
        public virtual bool WriteFile(string path, byte[] data)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));
            Debug.Assert(data != null);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, data);

                return true;
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to write file.\nFile: " + path + "\n\n");
                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));

                return false;
            }
        }

        // --- File Management ---
        /// <summary>Deletes a file.</summary>
        public virtual bool DeleteFile(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            try
            {
                if(File.Exists(path))
                {
                    File.Delete(path);
                }

                return true;
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to delete file.\nFile: " + path + "\n\n");
                Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));

                return false;
            }
        }

        /// <summary>Moves a file.</summary>
        public virtual bool MoveFile(string source, string destination)
        {
            Debug.Assert(!string.IsNullOrEmpty(source));
            Debug.Assert(!string.IsNullOrEmpty(destination));

            try
            {
                File.Move(source, destination);

                return true;
            }
            catch(Exception e)
            {
                string warningInfo = ("Failed to move file."
                                      + "\nSource File: " + source
                                      + "\nDestination: " + destination
                                      + "\n\n");
                Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));

                return false;
            }
        }

        /// <summary>Gets the size of a file.</summary>
        public virtual bool GetFileExists(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            return File.Exists(path);
        }

        /// <summary>Gets the size of a file.</summary>
        public virtual Int64 GetFileSize(string path)
        {
            Debug.Assert(!String.IsNullOrEmpty(path));

            if(!File.Exists(path)) { return -1; }

            try
            {
                var fileInfo = new FileInfo(path);

                return fileInfo.Length;
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to get file size.\nFile: " + path + "\n\n");
                Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));

                return -1;
            }
        }

        /// <summary>Gets the size and md5 hash of a file.</summary>
        public virtual bool GetFileSizeAndHash(string path, out Int64 byteCount, out string md5Hash)
        {
            Debug.Assert(!String.IsNullOrEmpty(path));

            byteCount = -1;
            md5Hash = null;

            if(!File.Exists(path)) { return false; }

            // get byteCount
            try
            {
                byteCount = (new FileInfo(path)).Length;
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to get file size.\nFile: " + path + "\n\n");
                Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));

                byteCount = -1;
                return false;
            }

            // get hash
            try
            {
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    using (var stream = File.OpenRead(path))
                    {
                        var hash = md5.ComputeHash(stream);
                        md5Hash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    }
                }
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to calculate file hash.\nFile: " + path + "\n\n");
                Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));

                md5Hash = null;
                return false;
            }

            // success!
            return true;
        }

        /// <summary>Gets the files at a location.</summary>
        public virtual IList<string> GetFiles(string path, string nameFilter, bool recurseSubdirectories)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            if(!Directory.Exists(path)) { return null; }

            var searchOption = (recurseSubdirectories
                                ? SearchOption.AllDirectories
                                : SearchOption.TopDirectoryOnly);

            if(nameFilter == null)
            {
                nameFilter = "*";
            }

            return Directory.GetFiles(path, nameFilter, searchOption);
        }

        // --- Directory Management ---
        /// <summary>Creates a directory.</summary>
        public virtual bool CreateDirectory(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            try
            {
                Directory.CreateDirectory(path);

                return true;
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to create directory.\nDirectory: " + path + "\n\n");
                Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));

                return true;
            }
        }

        /// <summary>Deletes a directory.</summary>
        public virtual bool DeleteDirectory(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            try
            {
                if(Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }

                return true;
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to delete directory.\nDirectory: " + path + "\n\n");
                Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));

                return false;
            }
        }

        /// <summary>Moves a directory.</summary>
        public virtual bool MoveDirectory(string source, string destination)
        {
            Debug.Assert(!string.IsNullOrEmpty(source));
            Debug.Assert(!string.IsNullOrEmpty(destination));

            try
            {
                Directory.Move(source, destination);

                return true;
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to move directory."
                                      + "\nSource Directory: " + source
                                      + "\nDestination: " + destination
                                      + "\n\n" + Utility.GenerateExceptionDebugString(e));
                Debug.LogWarning(warningInfo + Utility.GenerateExceptionDebugString(e));

                return false;
            }
        }

        /// <summary>Checks for the existence of a directory.</summary>
        public virtual bool GetDirectoryExists(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            return Directory.Exists(path);
        }

        /// <summary>Gets the sub-directories at a location.</summary>
        public virtual IList<string> GetDirectories(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            if(!Directory.Exists(path)) { return null; }

            try
            {
                return Directory.GetDirectories(path);
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to get directories.\nDirectory: " + path + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));

                return null;
            }
        }
    }
}
