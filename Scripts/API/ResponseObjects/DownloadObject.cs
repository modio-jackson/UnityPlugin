namespace ModIO.API
{
    [System.Serializable]
    public struct DownloadObject
    {
        // URL to download the file from the mod.io CDN.
        public readonly string binary_url;
        // Unix timestamp of when the binary_url will expire.
        public readonly int date_expires;
    }
}