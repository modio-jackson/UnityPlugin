using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Manages requests made for ModProfiles.</summary>
    public class ModProfileRequestManager : MonoBehaviour, IModSubscriptionsUpdateReceiver
    {
        // ---------[ SINGLETON ]---------
        /// <summary>Singleton instance.</summary>
        private static ModProfileRequestManager _instance = null;
        /// <summary>Singleton instance.</summary>
        public static ModProfileRequestManager instance
        {
            get
            {
                if(ModProfileRequestManager._instance == null)
                {
                    ModProfileRequestManager._instance = UIUtilities.FindComponentInScene<ModProfileRequestManager>(true);

                    if(ModProfileRequestManager._instance == null)
                    {
                        GameObject go = new GameObject("Mod Profile Request Manager");
                        ModProfileRequestManager._instance = go.AddComponent<ModProfileRequestManager>();
                    }
                }

                return ModProfileRequestManager._instance;
            }
        }

        // ---------[ NESTED DATA-TYPES ]--------
        public struct RequestPageData
        {
            public int resultOffset;
            public int resultTotal;
            public int[] modIds;

            /// <summary>Appends a collection of ids to a RequestPageData.</summary>
            public static RequestPageData Append(RequestPageData pageData,
                                                 int appendCollectionOffset,
                                                 int[] appendCollection)
            {
                if(appendCollection == null
                   || appendCollection.Length == 0)
                {
                    return pageData;
                }

                // asserts
                Debug.Assert(appendCollectionOffset >= 0);
                Debug.Assert(appendCollectionOffset + appendCollection.Length <= pageData.resultTotal);

                // calc last indicies
                int newOffset = (appendCollectionOffset < pageData.resultOffset
                                 ? appendCollectionOffset
                                 : pageData.resultOffset);

                int oldLastIndex = pageData.modIds.Length + pageData.resultOffset - 1;
                int appendingLastIndex = appendCollection.Length + appendCollectionOffset - 1;

                int newLastIndex = (appendingLastIndex > oldLastIndex
                                    ? appendingLastIndex
                                    : oldLastIndex);

                // fill array
                int[] newArray = new int[newLastIndex - newOffset + 1];
                for(int i = 0; i < newArray.Length; ++i)
                {
                    newArray[i] = ModProfile.NULL_ID;
                }

                Array.Copy(pageData.modIds, 0,
                           newArray, pageData.resultOffset - newOffset,
                           pageData.modIds.Length);
                Array.Copy(appendCollection, 0,
                           newArray, appendCollectionOffset - newOffset,
                           appendCollection.Length);

                // Create appended page data
                RequestPageData retData = new RequestPageData()
                {
                    resultOffset = newOffset,
                    resultTotal = pageData.resultTotal,
                    modIds = newArray,
                };

                return retData;
            }
        }

        // ---------[ FIELDS ]---------
        /// <summary>Should the cache be cleared on disable</summary>
        public bool clearCacheOnDisable = true;

        /// <summary>If enabled, stores retrieved profiles for subscribed mods.</summary>
        public bool storeIfSubscribed = true;

        /// <summary>Minimum profile count to request from the API.</summary>
        public int minimumFetchSize = APIPaginationParameters.LIMIT_MAX;

        /// <summary>Cached requests.</summary>
        public Dictionary<string, RequestPageData> requestCache = new Dictionary<string, RequestPageData>();

        /// <summary>Cached profiles.</summary>
        public Dictionary<int, ModProfile> profileCache = new Dictionary<int, ModProfile>()
        {
            { ModProfile.NULL_ID, null },
        };

        // --- ACCESSORS ---
        public virtual bool isCachingPermitted
        {
            get { return this.isActiveAndEnabled || !this.clearCacheOnDisable; }
        }

        // ---------[ INITIALIZATION ]---------
        protected virtual void Awake()
        {
            if(ModProfileRequestManager._instance == null)
            {
                ModProfileRequestManager._instance = this;
            }
            #if DEBUG
            else if(ModProfileRequestManager._instance != this)
            {
                Debug.LogWarning("[mod.io] Second instance of a ModProfileRequestManager"
                                 + " component enabled simultaneously."
                                 + " Only one instance of a ModProfileRequestManager"
                                 + " component should be active at a time.");
                this.enabled = false;
            }
            #endif
        }

        protected virtual void OnDisable()
        {
            if(this.clearCacheOnDisable)
            {
                this.requestCache.Clear();
                this.profileCache.Clear();
                this.profileCache.Add(ModProfile.NULL_ID, null);
            }
        }

        // ---------[ FUNCTIONALITY ]---------
        /// <summary>Fetches page of ModProfiles grabbing from the cache where possible.</summary>
        public virtual void FetchModProfilePage(RequestFilter filter, int offsetIndex, int profileCount,
                                                Action<RequestPage<ModProfile>> onSuccess,
                                                Action<WebRequestError> onError)
        {
            Debug.Assert(profileCount <= APIPaginationParameters.LIMIT_MAX);
            Debug.Assert(onSuccess != null);

            // ensure indicies are positive
            if(offsetIndex < 0) { offsetIndex = 0; }
            if(profileCount < 0) { profileCount = 0; }

            // check if results already cached
            string filterString = filter.GenerateFilterString();
            RequestPageData cachedData;
            if(this.requestCache.TryGetValue(filterString, out cachedData))
            {
                // early out if no results or index beyond resultTotal
                if(offsetIndex >= cachedData.resultTotal || profileCount == 0)
                {

                    RequestPage<ModProfile> requestPage = new RequestPage<ModProfile>();
                    requestPage.size = profileCount;
                    requestPage.resultOffset = offsetIndex;
                    requestPage.resultTotal = cachedData.resultTotal;
                    requestPage.items = new ModProfile[0];

                    onSuccess(requestPage);

                    return;
                }

                // clamp last index
                int clampedLastIndex = offsetIndex + profileCount-1;
                if(clampedLastIndex >= cachedData.resultTotal)
                {
                    // NOTE(@jackson): cachedData.resultTotal > 0
                    clampedLastIndex = cachedData.resultTotal - 1;
                }

                // check if entire result set encompassed by cache
                int cachedLastIndex = cachedData.resultOffset + cachedData.modIds.Length;
                if(cachedData.resultOffset <= offsetIndex
                   && clampedLastIndex <= cachedLastIndex)
                {
                    ModProfile[] resultArray = new ModProfile[clampedLastIndex - offsetIndex + 1];

                    // copy values across
                    bool nullFound = false;
                    for(int cacheIndex = offsetIndex - cachedData.resultOffset;
                        cacheIndex < cachedData.modIds.Length
                        && cacheIndex <= clampedLastIndex - cachedData.resultOffset
                        && !nullFound;
                        ++cacheIndex)
                    {
                        int modId = cachedData.modIds[cacheIndex];
                        ModProfile profile = null;
                        profileCache.TryGetValue(modId, out profile);

                        int arrayIndex = cacheIndex - offsetIndex;
                        resultArray[arrayIndex] = profile;

                        nullFound = (profile == null);
                    }

                    // return if no nulls found
                    if(!nullFound)
                    {
                        RequestPage<ModProfile> requestPage = new RequestPage<ModProfile>();
                        requestPage.size = profileCount;
                        requestPage.resultOffset = offsetIndex;
                        requestPage.resultTotal = cachedData.resultTotal;
                        requestPage.items = resultArray;

                        onSuccess(requestPage);
                        return;
                    }
                }
            }

            // PaginationParameters
            APIPaginationParameters pagination = new APIPaginationParameters();
            pagination.offset = offsetIndex;
            pagination.limit = profileCount;
            if(profileCount < this.minimumFetchSize)
            {
                pagination.limit = this.minimumFetchSize;
            }

            // Send Request
            APIClient.GetAllMods(filter, pagination,
            (r) =>
            {
                if(this != null)
                {
                    this.CacheRequestPage(filter, r);
                }

                if(onSuccess != null)
                {
                    if(pagination.limit != profileCount)
                    {
                        var subPage = ModProfileRequestManager.CreatePageSubset(r,
                                                                                offsetIndex,
                                                                                profileCount);
                        onSuccess(subPage);
                    }
                    else
                    {
                        onSuccess(r);
                    }
                }
            }, onError);
        }

        /// <summary>Append the response page to the cached data.</summary>
        public virtual void CacheRequestPage(RequestFilter filter, RequestPage<ModProfile> page)
        {
            // early out if shouldn't cache
            if(!this.isCachingPermitted) { return; }

            // asserts
            Debug.Assert(filter != null);
            Debug.Assert(page != null);

            // cache request
            string filterString = filter.GenerateFilterString();
            RequestPageData cachedData;
            if(this.requestCache.TryGetValue(filterString, out cachedData))
            {
                cachedData.resultTotal = page.resultTotal;

                this.requestCache[filterString] = RequestPageData.Append(cachedData,
                                                                         page.resultOffset,
                                                                         Utility.MapProfileIds(page.items));
            }
            else
            {
                cachedData = new RequestPageData()
                {
                    resultOffset = page.resultOffset,
                    resultTotal = page.resultTotal,
                    modIds = Utility.MapProfileIds(page.items),
                };

                this.requestCache.Add(filterString, cachedData);
            }

            // cache profiles
            foreach(ModProfile profile in page.items)
            {
                this.profileCache[profile.id] = profile;
            }

            // store
            if(this.storeIfSubscribed)
            {
                IList<int> subMods = ModManager.GetSubscribedModIds();
                foreach(ModProfile profile in page.items)
                {
                    if(subMods.Contains(profile.id))
                    {
                        CacheClient.SaveModProfile(profile);
                    }
                }
            }
        }

        /// <summary>Requests an individual ModProfile by id.</summary>
        public virtual void RequestModProfile(int id,
                                              Action<ModProfile> onSuccess, Action<WebRequestError> onError)
        {
            Debug.Assert(onSuccess != null);

            ModProfile profile = null;
            if(profileCache.TryGetValue(id, out profile))
            {
                onSuccess(profile);
                return;
            }

            profile = CacheClient.LoadModProfile(id);
            if(profile != null)
            {
                profileCache.Add(id, profile);
                onSuccess(profile);
                return;
            }

            APIClient.GetMod(id, (p) =>
            {
                if(this != null)
                {
                    profileCache[p.id] = p;

                    if(this.storeIfSubscribed
                       && ModManager.GetSubscribedModIds().Contains(p.id))
                    {
                        CacheClient.SaveModProfile(p);
                    }
                }

                onSuccess(p);
            },
            onError);
        }

        /// <summary>Requests a collection of ModProfiles by id.</summary>
        public virtual void RequestModProfiles(IList<int> orderedIdList,
                                               bool includeHiddenMods,
                                               Action<ModProfile[]> onSuccess,
                                               Action<WebRequestError> onError)
        {
            Debug.Assert(orderedIdList != null);
            Debug.Assert(onSuccess != null);

            ModProfile[] results = new ModProfile[orderedIdList.Count];
            List<int> missingIds = new List<int>(orderedIdList.Count);

            // grab from cache
            for(int i = 0; i < orderedIdList.Count; ++i)
            {
                int modId = orderedIdList[i];
                ModProfile profile = null;
                if(this.profileCache.TryGetValue(modId, out profile))
                {
                    if((includeHiddenMods || profile.visibility != ModVisibility.Hidden))
                    {
                        results[i] = profile;
                    }
                    else
                    {
                        results[i] = null;
                    }
                }

                if(profile == null)
                {
                    missingIds.Add(modId);
                }
            }

            // check disk for any missing profiles
            foreach(ModProfile profile in CacheClient.IterateFilteredModProfiles(missingIds))
            {
                int index = orderedIdList.IndexOf(profile.id);
                if(index >= 0 && results[index] == null)
                {
                    if((includeHiddenMods || profile.visibility != ModVisibility.Hidden))
                    {
                        results[index] = profile;
                    }
                    else
                    {
                        results[index] = null;
                    }

                    results[index] = profile;
                }

                missingIds.Remove(profile.id);
            }

            // if no missing profiles, early out
            if(missingIds.Count == 0)
            {
                onSuccess(results);
                return;
            }

            // fetch missing profiles
            Action<List<ModProfile>> onFetchProfiles = (modProfiles) =>
            {
                if(this != null)
                {
                    foreach(ModProfile profile in modProfiles)
                    {
                        this.profileCache[profile.id] = profile;
                    }

                    if(this.storeIfSubscribed)
                    {
                        IList<int> subMods = ModManager.GetSubscribedModIds();
                        foreach(ModProfile profile in modProfiles)
                        {
                            if(subMods.Contains(profile.id))
                            {
                                CacheClient.SaveModProfile(profile);
                            }
                        }
                    }
                }

                foreach(ModProfile profile in modProfiles)
                {
                    int i = orderedIdList.IndexOf(profile.id);
                    if(i >= 0)
                    {
                        results[i] = profile;
                    }
                }

                onSuccess(results);
            };

            this.StartCoroutine(this.FetchAllModProfiles(missingIds.ToArray(),
                                                         includeHiddenMods,
                                                         onFetchProfiles,
                                                         onError));
        }

        // ---------[ UTILITY ]---------
        /// <summary>Recursively fetches all of the mod profiles in the array.</summary>
        protected System.Collections.IEnumerator FetchAllModProfiles(int[] modIds,
                                                                     bool includeHiddenMods,
                                                                     Action<List<ModProfile>> onSuccess,
                                                                     Action<WebRequestError> onError)
        {
            List<ModProfile> modProfiles = new List<ModProfile>();

            // create visibility filter
            List<int> visibilityFilter = new List<int>() { (int)ModVisibility.Public, };
            if(includeHiddenMods)
            {
                visibilityFilter.Add((int)ModVisibility.Hidden);
            }

            APIPaginationParameters pagination = new APIPaginationParameters()
            {
                limit = APIPaginationParameters.LIMIT_MAX,
                offset = 0,
            };

            RequestFilter filter = new RequestFilter();
            filter.fieldFilters.Add(API.GetAllModsFilterFields.id,
                new InArrayFilter<int>() { filterArray = modIds, });
            filter.fieldFilters.Add(API.GetAllModsFilterFields.visible,
                new InArrayFilter<int>() { filterArray = visibilityFilter.ToArray() });

            bool isDone = false;

            while(!isDone)
            {
                RequestPage<ModProfile> page = null;
                WebRequestError error = null;

                APIClient.GetAllMods(filter, pagination,
                                     (r) => page = r,
                                     (e) => error = e);

                while(page == null && error == null) { yield return null;}

                if(error != null)
                {
                    if(onError != null)
                    {
                        onError(error);
                    }

                    modProfiles = null;
                    isDone = true;
                }
                else
                {
                    modProfiles.AddRange(page.items);

                    if(page.resultTotal <= (page.resultOffset + page.size))
                    {
                        isDone = true;
                    }
                    else
                    {
                        pagination.offset = page.resultOffset + page.size;
                    }
                }
            }

            if(isDone && modProfiles != null)
            {
                onSuccess(modProfiles);
            }
        }

        protected static RequestPage<ModProfile> CreatePageSubset(RequestPage<ModProfile> sourcePage,
                                                                  int resultOffset,
                                                                  int profileCount)
        {
            Debug.Assert(sourcePage != null);

            if(resultOffset < sourcePage.resultOffset)
            {
                resultOffset = sourcePage.resultOffset;
            }

            RequestPage<ModProfile> subPage = new RequestPage<ModProfile>()
            {
                size = profileCount,
                resultOffset = resultOffset,
                resultTotal = sourcePage.resultTotal,
            };

            // early out for 0
            if(profileCount <= 0)
            {
                subPage.size = 0;
                subPage.items = new ModProfile[0];

                return subPage;
            }
            else
            {
                int pageOffset = resultOffset - sourcePage.resultOffset;

                int arraySize = profileCount;
                if(pageOffset + arraySize > sourcePage.items.Length)
                {
                    arraySize = sourcePage.items.Length - pageOffset;
                }

                subPage.items = new ModProfile[arraySize];

                Array.Copy(sourcePage.items, pageOffset,
                           subPage.items, 0,
                           subPage.items.Length);

                return subPage;
            }
        }

        // ---------[ EVENTS ]---------
        /// <summary>Stores any cached profiles when the mod subscriptions are updated.</summary>
        public void OnModSubscriptionsUpdated(IList<int> addedSubscriptions,
                                              IList<int> removedSubscriptions)
        {
            if(this.storeIfSubscribed
               && addedSubscriptions.Count > 0)
            {
                foreach(int modId in addedSubscriptions)
                {
                    ModProfile profile;
                    if(this.profileCache.TryGetValue(modId, out profile))
                    {
                        CacheClient.SaveModProfile(profile);
                    }
                }
            }
        }
    }
}
