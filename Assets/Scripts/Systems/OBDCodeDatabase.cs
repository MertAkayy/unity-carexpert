using System.Collections.Generic;
using UnityEngine;

namespace Systems
{
    /// <summary>
    /// Database of OBD codes for the reference book.
    /// Created as a ScriptableObject asset that contains all OBD code entries.
    /// </summary>
    [CreateAssetMenu(fileName = "OBD Code Database", menuName = "Vehicle/OBD Code Database")]
    public class OBDCodeDatabase : ScriptableObject
    {
        [SerializeField] private List<OBDCodeEntry> codes = new List<OBDCodeEntry>();

        public List<OBDCodeEntry> Codes => codes;

        /// <summary>
        /// Gets an OBD code entry by code string
        /// </summary>
        public OBDCodeEntry GetCode(string code)
        {
            foreach (var entry in codes)
            {
                if (entry != null && entry.code == code)
                    return entry;
            }
            return null;
        }

        /// <summary>
        /// Gets all codes of a specific category
        /// </summary>
        public List<OBDCodeEntry> GetCodesByCategory(OBDCodeCategory category)
        {
            List<OBDCodeEntry> result = new List<OBDCodeEntry>();
            foreach (var entry in codes)
            {
                if (entry != null && entry.category == category)
                    result.Add(entry);
            }
            return result;
        }

        /// <summary>
        /// Searches for codes matching the search term
        /// </summary>
        public List<OBDCodeEntry> SearchCodes(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return new List<OBDCodeEntry>(codes);

            List<OBDCodeEntry> result = new List<OBDCodeEntry>();
            string lowerSearch = searchTerm.ToLower();

            foreach (var entry in codes)
            {
                if (entry == null) continue;

                if (entry.code.ToLower().Contains(lowerSearch) ||
                    entry.description.ToLower().Contains(lowerSearch))
                {
                    result.Add(entry);
                }
            }
            return result;
        }
    }
}
