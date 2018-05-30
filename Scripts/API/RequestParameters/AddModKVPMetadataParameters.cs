namespace ModIO.API
{
    public class AddModKVPMetadataParameters : RequestParameters
    {
        // ---------[ FIELDS ]---------
        /// <summary>
        /// [REQUIRED] Array containing one or more key value pairs
        /// where the the key and value are separated by a colon ':'
        /// (if the string contains multiple colons the split will
        /// occur on the first matched, i.e. pistol-dmg:800:400 will
        /// become key: pistol-dmg, value: 800:400).
        /// </summary>
        /// <remark>
        /// - Keys support alphanumeric, '_' and '-' characters only.
        /// - Keys can map to multiple values (1-to-many relationship).
        /// - Keys and values cannot exceed 255 characters in length.
        /// - Key value pairs are searchable by exact match only.
        /// </remark>
        public string[] metadata
        {
            set
            {
                this.SetStringArrayValue("metadata[]", value);
            }
        }

        // ---------[ HELPER FUNCTIONS ]---------
        /// <summary>
        /// Takes an array of <see cref="ModIO.MetadataKVP"/> and produces an array of API
        /// recognized strings to be assigned to
        /// <see cref="ModIO.AddModKVPMetadataParameters.metadata"/>
        /// </summary>
        /// <
        public static string[] ConvertMetadataKVPsToAPIStrings(MetadataKVP[] kvps)
        {
            string[] apiStrings = new string[kvps.Length];
            for(int i = 0; i < kvps.Length; ++i)
            {
                apiStrings[i] = kvps[i].key + ":" + kvps[i].value;
            }
            return apiStrings;
        }

    }
}
