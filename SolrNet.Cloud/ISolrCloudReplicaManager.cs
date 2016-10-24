using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SolrNet.Cloud {
    /// <summary>
    /// Provides the Ability to get the replicas or an appropriate solr url
    /// for the collection matching T.
    /// Note, if using dependency injection, this is only going to be useful when implementing ICollectionResolver so that T corresponds to a collection name.
    /// </summary>
    public interface ISolrCloudReplicaManager {
        /// <summary>
        /// Returns collection of replicas
        /// </summary>
        IList<SolrCloudReplica> SelectReplicas(bool leaders, string collection = null);

        string GetShardUrl(bool leader, string collection = null, bool autoRefresh = true);
    }

    public interface ISolrCloudReplicaManager<T>
    {
        /// <summary>
        /// Returns collection of replicas
        /// </summary>
        IList<SolrCloudReplica> SelectReplicas(bool leaders);

        string GetShardUrl(bool leader);
    }
}