namespace ModIO.UI
{
    [System.Serializable]
    public struct ModProfileDisplayData
    {
        public int modId;
        public int gameId;
        public ModStatus status;
        public ModVisibility visibility;
        public int dateAdded;
        public int dateUpdated;
        public int dateLive;
        public ModContentWarnings contentWarnings;
        public string homepageURL;
        public string name;
        public string nameId;
        public string summary;
        public string descriptionAsHTML;
        public string descriptionAsText;
        public string metadataBlob;
        public string profileURL;
        public MetadataKVP[] metadataKVPs;

        public static ModProfileDisplayData CreateFromProfile(ModProfile profile)
        {
            ModProfileDisplayData profileData = new ModProfileDisplayData()
            {
                modId               = profile.id,
                gameId              = profile.gameId,
                status              = profile.status,
                visibility          = profile.visibility,
                dateAdded           = profile.dateAdded,
                dateUpdated         = profile.dateUpdated,
                dateLive            = profile.dateLive,
                contentWarnings     = profile.contentWarnings,
                homepageURL         = profile.homepageURL,
                name                = profile.name,
                nameId              = profile.nameId,
                summary             = profile.summary,
                descriptionAsHTML   = profile.descriptionAsHTML,
                descriptionAsText   = profile.descriptionAsText,
                metadataBlob        = profile.metadataBlob,
                profileURL          = profile.profileURL,
                metadataKVPs        = profile.metadataKVPs,
            };

            return profileData;
        }
    }

    public abstract class ModProfileDisplayComponent : UnityEngine.MonoBehaviour
    {
        public abstract event System.Action<ModProfileDisplayComponent> onClick;

        public abstract ModDisplayData data { get; set; }

        public abstract void Initialize();
        public abstract void DisplayProfile(ModProfile profile,
                                            System.Collections.Generic.IEnumerable<ModTagCategory> tagCategories);
        public abstract void DisplayLoading();
    }
}
