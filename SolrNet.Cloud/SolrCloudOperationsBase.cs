using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace SolrNet.Cloud {
    /// <summary>
    /// Solr cloud operations base
    /// </summary>
    public abstract class SolrCloudOperationsBase<T>: ISolrCloudReplicaManager<T>
    {
        /// <summary>
        /// Is post connection
        /// </summary>
        private readonly bool isPostConnection;

        /// <summary>
        /// Collection name
        /// </summary>
        private readonly string collectionName;

        /// <summary>
        /// Cloud state provider
        /// </summary>
        private readonly ISolrCloudStateProvider cloudStateProvider;

        /// <summary>
        /// Operations provider
        /// </summary>
        private readonly ISolrOperationsProvider operationsProvider;

        /// <summary>
        /// Random instance
        /// </summary>
        private readonly Random random;
        

        /// <summary>
        /// Constructor
        /// </summary>
        protected SolrCloudOperationsBase(ISolrCloudStateProvider cloudStateProvider, ISolrOperationsProvider operationsProvider, bool isPostConnection) {
            this.cloudStateProvider = cloudStateProvider;
            this.operationsProvider = operationsProvider;
            this.isPostConnection = isPostConnection;
            random = new Random();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        protected SolrCloudOperationsBase(ISolrCloudStateProvider cloudStateProvider, ISolrOperationsProvider operationsProvider, bool isPostConnection, string collectionName = null)
            : this(cloudStateProvider, operationsProvider, isPostConnection)
        {
            this.collectionName = collectionName;
        }

        /// <summary>
        /// Performs basic operataion
        /// </summary>
        protected TResult PerformBasicOperation<TResult>(Func<ISolrBasicOperations<T>, TResult> operation, bool leader = false)
        {
            var operations = operationsProvider.GetBasicOperations<T>(
                GetShardUrl(leader),
                isPostConnection);
            if (operations == null)
                throw new ApplicationException("Operations provider returned null.");
            return operation(operations);
        }

        /// <summary>
        /// Perform operation
        /// </summary>
        protected TResult PerformOperation<TResult>(Func<ISolrOperations<T>, TResult> operation, bool leader = false) {
   
            var operations = operationsProvider.GetOperations<T>(
                GetShardUrl(leader),
                isPostConnection);
            if (operations == null)
                throw new ApplicationException("Operations provider returned null.");
            return operation(operations);
        }

        public string GetShardUrl(bool leader)
        {
            return cloudStateProvider.GetShardUrl(leader, collectionName);
        }

        public IList<SolrCloudReplica> SelectReplicas(bool leaders) {
            return cloudStateProvider.SelectReplicas(leaders, collectionName);
        }
        
    }
}