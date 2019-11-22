// #define ENABLE_STEAMWORKS_FACEPUNCH
// #define ENABLE_STEAMWORKS_NET
// #define ENABLE_STEAM_OTHER
// #define ENABLE_GOG_AUTHENTICATION

/*** NOTE:
 * If building to a platform other than Mac, Windows (exe), or Linux,
 * the Unity #define directives as specified at [https://docs.unity3d.com/Manual/PlatformDependentCompilation.html]
 * act to enable those specific authentication methods and thus do not require manual activation.
 ***/

using System;
using System.Linq;
using System.Collections.Generic;

using Debug = UnityEngine.Debug;

namespace ModIO
{
    /// <summary>A collection of user management functions provided for convenience.</summary>
    public static class UserAccountManagement
    {
        // ---------[ CONSTANTS ]---------
        #if ENABLE_STEAMWORKS_FACEPUNCH || ENABLE_STEAMWORKS_NET || ENABLE_STEAM_OTHER
            public static readonly string PROFILE_URL_POSTFIX = "?ref=steam";
        #elif ENABLE_GOG_AUTHENTICATION
            public static readonly string PROFILE_URL_POSTFIX = "?ref=gog";
        #else
            public static readonly string PROFILE_URL_POSTFIX = string.Empty;
        #endif

        /// <summary>Load user data file.</summary>
        private static byte[] LoadUserDataFile()
        {
            byte[] data;

            #if UNITY_EDITOR
                // NOTE(@jackson): ".modio" is banned on Mac
                string filePath = IOUtilities.CombinePath(UnityEngine.Application.dataPath,
                                                          "modio~",
                                                          "user.data");

                data = IOUtilities.LoadBinaryFile(filePath);

            #elif ENABLE_STEAMWORKS_FACEPUNCH
                string filePath = "modio_user.data";

                if(Steamworks.SteamRemoteStorage.FileExists(filePath))
                {
                    data = Steamworks.SteamRemoteStorage.FileRead(filePath);
                }

            #elif ENABLE_STEAMWORKS_NET
                string filePath = "modio_user.data";

                if(Steamworks.SteamRemoteStorage.FileExists(filePath))
                {
                    int fileSize = Steamworks.SteamRemoteStorage.GetFileSize(filePath);

                    if(fileSize > 0)
                    {
                        if(fileSize > 1024) { fileSize = 1024; }

                        data = new byte[fileSize];
                        Steamworks.SteamRemoteStorage.FileRead(filePath, data, fileSize);
                    }
                }

            #elif UNITY_STANDALONE_WIN
                string filePath = IOUtilities.Combine("%APPDATA%",
                                                      "modio",
                                                      "game-" + PluginSettings.data.gameId,
                                                      "user.data");

                data = IOUtilities.LoadBinaryFile(filePath);

            #elif UNITY_STANDALONE_OSX
                string filePath = ("~/Library/Application Support/mod.io/"
                                   + "game-" + PluginSettings.data.gameId
                                   + "/user.data");

                data = IOUtilities.LoadBinaryFile(filePath);

            #elif UNITY_STANDALONE_LINUX
                string filePath = ("~/.config/mod.io/"
                                   + "game-" + PluginSettings.data.gameId
                                   + "/user.data");

                data = IOUtilities.LoadBinaryFile(filePath);

            #else
                throw new System.NotImplementedException("[mod.io] The loading of user data for this"
                                                         + " build definition has not been implemented.");

            #endif

            return data;
        }

        // ---------[ DATA ]---------
        /// <summary>User data for the currently active user.</summary>
        private static LocalUserData m_activeUserData = new LocalUserData()
        {
            modioUserId = ModProfile.NULL_ID,
            localUserId = null,
            enabledModIds = new int[0],
        };

        // ---------[ UTILITY ]---------
        /// <summary>A wrapper function for setting the UserAuthenticationData.wasTokenRejected to false.</summary>
        public static void MarkAuthTokenRejected()
        {
            UserAuthenticationData data = UserAuthenticationData.instance;
            data.wasTokenRejected = true;
            UserAuthenticationData.instance = data;
        }

        /// <summary>Returns the enabled mods for the active user.</summary>
        public static List<int> GetEnabledModIds()
        {
            return new List<int>(UserAccountManagement.m_activeUserData.enabledModIds);
        }

        /// <summary>Sets the enabled mods for the active user.</summary>
        public static void SetEnabledModIds(IEnumerable<int> modIds)
        {
            int[] modIdArray;

            if(modIds == null)
            {
                modIdArray = new int[0];
            }
            else
            {
                modIdArray = modIds.ToArray();
            }

            UserAccountManagement.m_activeUserData.enabledModIds = modIdArray;
        }

        // ---------[ AUTHENTICATION ]---------
        /// <summary>Requests a login code be sent to an email address.</summary>
        public static void RequestSecurityCode(string emailAddress,
                                               Action onSuccess,
                                               Action<WebRequestError> onError)
        {
            if(onSuccess == null)
            {
                APIClient.SendSecurityCode(emailAddress, null, onError);
            }
            else
            {
                APIClient.SendSecurityCode(emailAddress, (m) => onSuccess(), onError);
            }
        }

        /// <summary>Attempts to authenticate a user using an emailed security code.</summary>
        public static void AuthenticateWithSecurityCode(string securityCode,
                                                        Action<UserProfile> onSuccess,
                                                        Action<WebRequestError> onError)
        {
            APIClient.GetOAuthToken(securityCode, (t) =>
            {
                UserAuthenticationData authData = new UserAuthenticationData()
                {
                    token = t,
                    wasTokenRejected = false,
                    externalAuthToken = null,
                    externalAuthId = null,
                };

                UserAuthenticationData.instance = authData;
                UserAccountManagement.FetchUserProfile(onSuccess, onError);
            },
            onError);
        }

        /// <summary>Attempts to reauthenticate using the enabled service.</summary>
        public static void ReauthenticateWithExternalAuthToken(Action<UserProfile> onSuccess,
                                                               Action<WebRequestError> onError)
        {
            Debug.Assert(!string.IsNullOrEmpty(UserAuthenticationData.instance.externalAuthToken));

            Action<string, Action<string>, Action<WebRequestError>> authAction = null;

            #if ENABLE_STEAMWORKS_FACEPUNCH || ENABLE_STEAMWORKS_NET || ENABLE_STEAM_OTHER
                authAction = APIClient.RequestSteamAuthentication;
            #elif ENABLE_GOG_AUTHENTICATION
                authAction = APIClient.RequestGOGAuthentication;
            #endif

            #if DEBUG
                if(authAction == null)
                {
                    Debug.LogError("[mod.io] Cannot reauthenticate without enabling an external"
                                   + " authentication service. Please refer to this file"
                                   + " (UserAccountManagement.cs) and uncomment the #define for the"
                                   + " desired service at the beginning of the file.");
                }
            #endif

            authAction.Invoke(UserAuthenticationData.instance.externalAuthToken, (t) =>
            {
                UserAuthenticationData authData = new UserAuthenticationData()
                {
                    token = t,
                    wasTokenRejected = false,
                };

                UserAuthenticationData.instance = authData;
                UserAccountManagement.FetchUserProfile(onSuccess, onError);
            },
            onError);
        }

        /// <summary>Stores the oAuthToken and steamTicket and fetches the UserProfile.</summary>
        private static void FetchUserProfile(Action<UserProfile> onSuccess,
                                             Action<WebRequestError> onError)
        {
            APIClient.GetAuthenticatedUser((p) =>
            {
                UserAuthenticationData data = UserAuthenticationData.instance;
                data.userId = p.id;
                UserAuthenticationData.instance = data;

                if(onSuccess != null)
                {
                    onSuccess(p);
                }
            },
            onError);
        }

        // ---------[ STEAM AUTHENTICATION ]---------
        #if ENABLE_STEAMWORKS_FACEPUNCH || ENABLE_STEAMWORKS_NET || ENABLE_STEAM_OTHER
        /// <summary>Attempts to authenticate a user using a Steam Encrypted App Ticket.</summary>
        /// <remarks>This version is designed to match the Steamworks.NET implementation by
        /// @rlabrecque at https://github.com/rlabrecque/Steamworks.NET</remarks>
        public static void AuthenticateWithSteamEncryptedAppTicket(byte[] pTicket, uint pcbTicket,
                                                                   Action<UserProfile> onSuccess,
                                                                   Action<WebRequestError> onError)
        {
            string encodedTicket = Utility.EncodeEncryptedAppTicket(pTicket, pcbTicket);
            UserAccountManagement.AuthenticateWithSteamEncryptedAppTicket(encodedTicket,
                                                                          onSuccess, onError);
        }

        /// <summary>Attempts to authenticate a user using a Steam Encrypted App Ticket.</summary>
        /// <remarks>This version is designed to match the FacePunch.SteamWorks implementation by
        /// @garrynewman at https://github.com/Facepunch/Facepunch.Steamworks</remarks>
        public static void AuthenticateWithSteamEncryptedAppTicket(byte[] authTicketData,
                                                                   Action<UserProfile> onSuccess,
                                                                   Action<WebRequestError> onError)
        {
            string encodedTicket = Utility.EncodeEncryptedAppTicket(authTicketData, (uint)authTicketData.Length);
            UserAccountManagement.AuthenticateWithSteamEncryptedAppTicket(encodedTicket,
                                                                          onSuccess, onError);
        }


        /// <summary>Attempts to authenticate a user using a Steam Encrypted App Ticket.</summary>
        public static void AuthenticateWithSteamEncryptedAppTicket(string encodedTicket,
                                                                   Action<UserProfile> onSuccess,
                                                                   Action<WebRequestError> onError)
        {
            APIClient.RequestSteamAuthentication(encodedTicket, (t) =>
            {
                UserAuthenticationData authData = new UserAuthenticationData()
                {
                    token = t,
                    wasTokenRejected = false,
                    externalAuthToken = encodedTicket,
                    externalAuthId = null,
                };

                UserAuthenticationData.instance = authData;
                UserAccountManagement.FetchUserProfile(onSuccess, onError);
            },
            onError);
        }
        #endif

        // ---------[ GOG AUTHENTICATION ]---------
        #if ENABLE_GOG_AUTHENTICATION
        /// <summary>Attempts to authenticate a user using a GOG Encrypted App Ticket.</summary>
        public static void AuthenticateWithGOGEncryptedAppTicket(byte[] data, uint dataSize,
                                                                 Action<UserProfile> onSuccess,
                                                                 Action<WebRequestError> onError)
        {
            string encodedTicket = Utility.EncodeEncryptedAppTicket(data, dataSize);
            UserAccountManagement.AuthenticateWithGOGEncryptedAppTicket(encodedTicket,
                                                                        onSuccess, onError);
        }

        /// <summary>Attempts to authenticate a user using a GOG Encrypted App Ticket.</summary>
        public static void AuthenticateWithGOGEncryptedAppTicket(string encodedTicket,
                                                                 Action<UserProfile> onSuccess,
                                                                 Action<WebRequestError> onError)
        {
            APIClient.RequestGOGAuthentication(encodedTicket, (t) =>
            {
                UserAuthenticationData authData = new UserAuthenticationData()
                {
                    token = t,
                    wasTokenRejected = false,
                    externalAuthToken = encodedTicket,
                    externalAuthId = null,
                };

                UserAuthenticationData.instance = authData;
                UserAccountManagement.FetchUserProfile(onSuccess, onError);
            },
            onError);
        }
        #endif
    }
}
