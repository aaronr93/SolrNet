using System;
using System.Collections.Generic;

namespace SolrNet.Cloud
{
    /// <summary>
    /// Solr cloud state provider interface
    /// </summary>
    public interface ISolrCloudStateProvider : IDisposable, ISolrCloudReplicaManager
    {
        /// <summary>
        /// Provider key
        /// </summary>
        string Key { get; }

        /// <summary>
        /// Returns actual solr cloud state
        /// </summary>
        SolrCloudState GetCloudState();

        /// <summary>
        /// Provider initialization
        /// </summary>
        void Init();
    }
}
