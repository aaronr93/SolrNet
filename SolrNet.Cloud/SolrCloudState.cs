using System.Collections.Generic;
using System.Linq;

namespace SolrNet.Cloud {
    /// <summary>
    /// Represents cloud state
    /// </summary>
    public class SolrCloudState {
        /// <summary>
        /// State collections
        /// </summary>
        public IDictionary<string, SolrCloudCollection> Collections { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public SolrCloudState(IDictionary<string, SolrCloudCollection> collections)
        {
            Collections = collections;
            Aliases = new Dictionary<string, string>();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public SolrCloudState(IDictionary<string, SolrCloudCollection> collections, IDictionary<string, string> aliases) {
            Collections = collections;
            Aliases = aliases;
        }

        public IDictionary<string, string> Aliases { get; set; }

        /// <summary>
        /// Returns merged cloud states
        /// </summary>
        public SolrCloudState Merge(SolrCloudState state)
        {
            if (state == null || state.Collections == null || !state.Collections.Any())
                return this;

            foreach (var element in state.Collections)
            {
                Collections.Add(element);
            }

            return this;
        }
    }
}