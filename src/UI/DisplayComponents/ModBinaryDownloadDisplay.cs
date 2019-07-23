using System;

using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Represents the status of a mod binary download visually.</summary>
    public class ModBinaryDownloadDisplay : MonoBehaviour, IModViewElement
    {
        // ---------[ NESTED DATA-TYPES ]---------
        /// <summary>A structure used to calculate the current averaged download speed.</summary>
        [Serializable]
        private struct DownloadSpeed
        {
            // ---------[ FIELDS ]---------
            public int lastIndex;
            public int stepsRecorded;
            public float[] timeStepMarker;
            public Int64[] bytesReceived;

            // ---------[ METHODS ]---------
            public void Reset()
            {
                this.lastIndex = -1;
                this.stepsRecorded = 0;
            }

            public void AddMarker(float timeStamp, Int64 byteCount)
            {
                // ignore if not newer timeStamp
                if(lastIndex >= 0
                   && (timeStamp - this.timeStepMarker[lastIndex] <= 0f))
                {
                    return;
                }

                // overwrite if 0
                if(byteCount == 0
                   && lastIndex >= 0
                   && this.bytesReceived[lastIndex] == 0)
                {
                    this.timeStepMarker[lastIndex] = timeStamp;
                    return;
                }

                // add!
                ++lastIndex;
                lastIndex %= timeStepMarker.Length;

                this.timeStepMarker[lastIndex] = timeStamp;
                this.bytesReceived[lastIndex] = byteCount;

                ++stepsRecorded;

            }

            public Int64 GetAverageDownloadSpeed()
            {
                if(stepsRecorded <= 1)
                {
                    return 0;
                }

                Debug.Assert(lastIndex >= 0);
                Debug.Assert(this.timeStepMarker.Length == this.bytesReceived.Length);

                int firstIndex = (lastIndex + 1) % this.timeStepMarker.Length;
                if(this.stepsRecorded <= this.timeStepMarker.Length)
                {
                    firstIndex = 0;
                }

                float initialTimeStamp = this.timeStepMarker[firstIndex];
                Int64 initialByteCount = this.bytesReceived[firstIndex];

                float finalTimeStamp = this.timeStepMarker[lastIndex];
                Int64 finalByteCount = this.bytesReceived[lastIndex];

                return (Int64)((finalByteCount - initialByteCount)/(finalTimeStamp - initialTimeStamp));
            }
        }

        // ---------[ CONSTANTS ]---------
        /// <summary>Interval between download speed updates.</summary>
        public const float  DOWNLOAD_SPEED_UPDATE_INTERVAL = 0.5f;
        /// <summary>Marker count used for smoothing download speed average.</summary>
        public const int    DOWNLOAD_SPEED_MARKER_COUNT = 10;
        /// <summary>Delay before hiding the game object (if enabled).</summary>
        public const float  HIDE_DELAY_SECONDS = 1.5f;

        // ---------[ FIELDS ]---------
        // --- Components ---
        /// <summary>Component to display the total number of bytes for the download.</summary>
        public GenericTextComponent bytesTotalText;
        /// <summary>Component to display the number of bytes received.</summary>
        public GenericTextComponent bytesReceivedText;
        /// <summary>Component to display the percentage completed.</summary>
        public GenericTextComponent percentageText;
        /// <summary>Component to display the number of bytes being downloaded per second.</summary>
        public GenericTextComponent bytesPerSecondText;
        /// <summary>Component to display the estimated time remaining for the download.</summary>
        public GenericTextComponent timeRemainingText;
        /// <summary>Component to display the progress of the download.</summary>
        public HorizontalProgressBar progressBar = null;
        /// <summary>Determines whether the component should hide if not downloading.</summary>
        public bool hideIfInactive = true;

        // --- Display Data---
        /// <summary>Parent ModView.</summary>
        private ModView m_view = null;

        /// <summary>ModId of the mod currently being monitored.</summary>
        private int m_modId = ModProfile.NULL_ID;

        /// <summary>The currently running update coroutine.</summary>
        private Coroutine m_updateCoroutine = null;

        /// <summary>Download Info.</summary>
        private FileDownloadInfo m_downloadInfo = null;

        /// <summary>Current download speed.</summary>
        private DownloadSpeed m_downloadSpeed = new DownloadSpeed()
        {
            lastIndex = -1,
            stepsRecorded = 0,
            timeStepMarker = new float[DOWNLOAD_SPEED_MARKER_COUNT],
            bytesReceived = new Int64[DOWNLOAD_SPEED_MARKER_COUNT],
        };

        // ---------[ INITIALIZATION ]---------
        protected virtual void Awake()
        {
            DownloadClient.modfileDownloadStarted += OnDownloadStarted;
        }

        protected virtual void OnDestroy()
        {
            DownloadClient.modfileDownloadStarted -= OnDownloadStarted;
        }

        protected virtual void OnEnable()
        {
            // check for auto-hide and download start
            if(this.m_downloadInfo != null)
            {
                if(this.m_updateCoroutine == null)
                {
                    this.m_updateCoroutine = this.StartCoroutine(this.UpdateCoroutine());
                }
            }
            else if(this.hideIfInactive)
            {
                this.gameObject.SetActive(false);
            }
        }

        // --- IMODVIEWELEMENT INTERFACE ---
        /// <summary>IModViewElement interface.</summary>
        public void SetModView(ModView view)
        {
            // early out
            if(this.m_view == view) { return; }

            // unhook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged -= DisplayProfile;
            }

            // assign
            this.m_view = view;

            // hook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged += DisplayProfile;
                this.DisplayProfile(this.m_view.profile);
            }
            else
            {
                this.DisplayProfile(null);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays download for a given mod.</summary>
        public void DisplayProfile(ModProfile profile)
        {
            int newId = ModProfile.NULL_ID;
            if(profile != null)
            {
                newId = profile.id;
            }

            // check for change
            if(this.m_modId != newId)
            {
                // reset everything
                if(this.m_updateCoroutine != null)
                {
                    this.StopCoroutine(this.m_updateCoroutine);
                    this.m_updateCoroutine = null;
                }

                this.m_modId = newId;
                this.m_downloadInfo = null;
                this.m_downloadSpeed.Reset();

                // check if currently downloading
                bool isDownloading = false;

                if(newId != ModProfile.NULL_ID)
                {
                    foreach(var kvp in DownloadClient.modfileDownloadMap)
                    {
                        if(kvp.Key.modId == this.m_modId)
                        {
                            isDownloading = true;
                            OnDownloadStarted(kvp.Key, kvp.Value);
                        }
                    }
                }

                // set active/inactive as appropriate
                this.gameObject.SetActive(isDownloading || !this.hideIfInactive);
            }
        }

        /// <summary>Updates the UI representation every frame.</summary>
        protected virtual System.Collections.IEnumerator UpdateCoroutine()
        {
            Debug.Assert(this.m_downloadInfo != null);

            // set initial speed marker
            float lastUpdate = Time.unscaledTime;
            Int64 bytesDownloaded = 0;
            if(this.m_downloadInfo.request != null)
            {
                bytesDownloaded = (Int64)this.m_downloadInfo.request.downloadedBytes;
            }

            this.m_downloadSpeed.AddMarker(lastUpdate, bytesDownloaded);

            // loop while downloading
            while(this != null
                  && this.m_downloadInfo != null
                  && !this.m_downloadInfo.isDone)
            {
                float now = Time.unscaledTime;
                if(m_downloadInfo.request != null
                   && now - lastUpdate >= ModBinaryDownloadDisplay.DOWNLOAD_SPEED_UPDATE_INTERVAL)
                {
                    lastUpdate = now;
                    this.m_downloadSpeed.AddMarker(now,
                                                   (Int64)m_downloadInfo.request.downloadedBytes);
                }

                this.UpdateComponents();

                yield return null;
            }

            if(this.m_downloadInfo != null)
            {
                this.m_downloadSpeed.AddMarker(Time.unscaledTime, this.m_downloadInfo.fileSize);
            }

            this.UpdateComponents();

            if(this.hideIfInactive)
            {
                yield return new WaitForSecondsRealtime(HIDE_DELAY_SECONDS);
                this.gameObject.SetActive(false);
            }
        }

        /// <summary>Updates the display components.</summary>
        protected virtual void UpdateComponents()
        {
            // collect vars
            Int64 fileSize = 0;
            Int64 bytesReceived = 0;
            Int64 downloadSpeed = 0;
            float percentComplete = 0f;

            if(this.m_downloadInfo != null)
            {
                fileSize = this.m_downloadInfo.fileSize;
                bytesReceived = (Int64)this.m_downloadInfo.request.downloadedBytes;
                percentComplete = (float)bytesReceived / (float)fileSize;
                downloadSpeed = this.m_downloadSpeed.GetAverageDownloadSpeed();
            }

            // calculate time remaining
            string timeRemainingDisplayString = string.Empty;
            if(fileSize > 0 && downloadSpeed > 0)
            {
                Int64 secondsRemaining = (int)((fileSize - bytesReceived) / downloadSpeed);

                TimeSpan remaining = TimeSpan.FromSeconds(secondsRemaining);
                timeRemainingDisplayString = (remaining.TotalHours + ":"
                                              + remaining.Minutes + ":"
                                              + remaining.Seconds);
            }

            // display
            this.bytesTotalText.text = UIUtilities.ByteCountToDisplayString(fileSize);
            this.bytesReceivedText.text = UIUtilities.ByteCountToDisplayString(bytesReceived);
            this.percentageText.text = (percentComplete * 100f).ToString("0.0") + "%";
            this.bytesPerSecondText.text = (UIUtilities.ByteCountToDisplayString(downloadSpeed) + "/s");
            this.timeRemainingText.text = timeRemainingDisplayString;

            if(this.progressBar != null)
            {
                this.progressBar.percentComplete = percentComplete;
            }
        }

        // ---------[ EVENTS ]---------
        /// <summary>Initializes the component display.</summary>
        protected virtual void OnDownloadStarted(ModfileIdPair idPair, FileDownloadInfo downloadInfo)
        {
            if(this.m_modId == idPair.modId
               && downloadInfo != null)
            {
                this.m_downloadInfo = downloadInfo;

                if(!this.isActiveAndEnabled && this.hideIfInactive)
                {
                    this.gameObject.SetActive(true);
                }

                if(this.isActiveAndEnabled
                   && this.m_updateCoroutine == null)
                {
                    this.m_updateCoroutine = this.StartCoroutine(this.UpdateCoroutine());
                }
            }
        }
    }
}
