using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZooKeeperNet;

namespace SolrNet.Cloud.ZooKeeperClient
{
    public abstract class SolrCloudStateProviderBase : ISolrCloudReplicaManager {

        protected readonly Random _random;

        public SolrCloudStateProviderBase() {
            _random = new Random();
        }

        /// <summary>
        /// Cluster state path constant
        /// </summary>
        public const string Aliases = "/aliases.json";

        public IList<SolrCloudReplica> SelectReplicas(bool leaders, string collectionName = null) {
            var derivedCollectionName = collectionName;
            var state = GetCloudState();

            if (state == null || state.Collections == null || state.Collections.Count == 0) {
                throw new ApplicationException("Didn't get any collection's state from zookeeper.");
            }

            if (derivedCollectionName != null && state.Aliases.ContainsKey(collectionName)) {
                derivedCollectionName = state.Aliases[collectionName];
            }

            if (derivedCollectionName != null && !state.Collections.ContainsKey(derivedCollectionName)) {
                throw new ApplicationException(string.Format("Didn't get '{0}' collection state from zookeeper.", derivedCollectionName));
            }


            var collection = derivedCollectionName == null
                ? state.Collections.Values.First()
                : state.Collections[derivedCollectionName];
            var replicas = collection.Shards.Values
                                     .Where(shard => shard.IsActive)
                                     .SelectMany(shard => shard.Replicas.Values)
                                     .Where(replica => replica.IsActive && (!leaders || replica.IsLeader))
                                     .ToList();

            return replicas;
        }

        public string GetShardUrl(bool leader, string collectionName = null, bool autoRefresh = true) {
            var replicas = SelectReplicas(leader, collectionName);

            if (leader && replicas.Count == 0) {
                replicas = SelectReplicas(false, collectionName);
            }

            if (replicas.Count == 0) {
                if (autoRefresh) {
                    //try to force zookeeper to refresh. If the core isn't there after a refresh then it really is down and its ok to throw an exception
                    GetFreshCloudState();
                    GetShardUrl(leader, collectionName, false);
                }
                throw new ApplicationException(string.Format("No appropriate node was selected to perform the operation on collection {0} and leader = {1}", collectionName, leader));
            }

            return replicas[_random.Next(replicas.Count)].Url;
        }

        public abstract SolrCloudState GetCloudState();

        public abstract SolrCloudState GetFreshCloudState();
    }


    public class SolrCloudStateProvider : SolrCloudStateProviderBase, ISolrCloudStateProvider, IWatcher
    {
        /// <summary>
        /// Cluster state path constant
        /// </summary>
        public const string ClusterState = "/clusterstate.json";

        /// <summary>
        /// External collections state file name
        /// </summary>
        public const string CollectionState = "state.json";

        /// <summary>
        /// Collection zookeeper node path
        /// </summary>
        public const string CollectionsZkNode = "/collections";

        public string Key { get; private set; }

        /// <summary>
        /// Is disposed
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// Is initialized
        /// </summary>
        private bool isInitialized;

        /// <summary>
        /// Object for lock
        /// </summary>
        private readonly object syncLock;

        /// <summary>
        /// ZooKeeper client instance
        /// </summary>
        private IZooKeeper zooKeeper;

        /// <summary>
        /// ZooKeeper connection string
        /// </summary>
        private readonly string zooKeeperConnection;

        private SolrCloudState state;

        /// <summary>
        /// Constuctor
        /// </summary>
        public SolrCloudStateProvider(string zooKeeperConnection):base()
        {
            if (string.IsNullOrEmpty(zooKeeperConnection))
                throw new ArgumentNullException("zooKeeperConnection");

            this.zooKeeperConnection = zooKeeperConnection;
            syncLock = new object();
            Key = zooKeeperConnection;
        }

        /// <summary>
        /// Initialize cloud state
        /// </summary>
        public void Init()
        {
            if (isInitialized)
            {
                return;
            }
            lock (syncLock)
            {
                if (!isInitialized)
                {
                    Update();
                    isInitialized = true;
                }
            }
        }

        /// <summary>
        /// Get cloud state
        /// </summary>
        /// <returns>Solr Cloud State</returns>
        public override SolrCloudState GetCloudState()
        {
            return state;
        }

        /// <summary>
        /// Reinitialize connection and get fresh cloud state.
        /// Not included in ISolrCloudStateProvider interface due to the testing purpose only 
        /// (causes reloading all cloud data and too slow to use in production)
        /// </summary>
        /// <returns>Solr Cloud State</returns>
        public override SolrCloudState GetFreshCloudState()
        {
            SynchronizedUpdate(cleanZookeeperConnection: true);
            return GetCloudState();
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }
            lock (syncLock)
            {
                if (!isDisposed)
                {
                    if (zooKeeper != null)
                    {
                        zooKeeper.Dispose();
                    }
                    isDisposed = true;
                }
            }
        }

        /// <summary>
        /// Watcher for zookeeper events
        /// </summary>
        /// <param name="event">zookeeper event</param>
        void IWatcher.Process(WatchedEvent @event)
        {
            if (@event.Type != EventType.None && !string.IsNullOrEmpty(@event.Path))
            {
                SynchronizedUpdate();                
            }
            else if (@event.Type == EventType.None && @event.State == KeeperState.Disconnected)
            {
                SynchronizedUpdate(cleanZookeeperConnection: true);
            }
        }

        /// <summary>
        /// Synchronized updates of zookeeper connection and actual cloud state
        /// </summary>
        /// <param name="cleanZookeeperConnection">clean zookeeper connection and create new one</param>
        private void SynchronizedUpdate(bool cleanZookeeperConnection = false)
        {
            lock (syncLock)
            {                
                try
                {
                    Update(cleanZookeeperConnection);
                }
                catch (Exception ex)
                {
                    // log exceptions here
                }
            }
        }

        /// <summary>
        /// Updates zookeeper connection and actual cloud state
        /// </summary>
        /// <param name="cleanZookeeperConnection">clean zookeeper connection and create new one</param>
        private void Update(bool cleanZookeeperConnection = false)
        {
            if (zooKeeper == null || cleanZookeeperConnection)
            {
                if (zooKeeper != null)
                {
                    zooKeeper.Dispose();
                }
                zooKeeper = new ZooKeeper(zooKeeperConnection, TimeSpan.FromSeconds(10), this);
            }

            state = GetInternalCollectionsState().Merge(GetExternalCollectionsState());
        }

        /// <summary>
        /// Returns parsed internal collections cloud state
        /// </summary>
        private SolrCloudState GetInternalCollectionsState()
        {
            byte[] data;

            byte[] aliases;
            try
            {
                data = zooKeeper.GetData(ClusterState, true, null);
                aliases = zooKeeper.GetData(Aliases, true, null);
                
            }
            catch (KeeperException ex)
            {
                return new SolrCloudState(new Dictionary<string, SolrCloudCollection>());
            }

            var collectionsState =
                data != null
                ? SolrCloudStateParser.Parse(Encoding.Default.GetString(data), aliases==null?null:Encoding.Default.GetString(aliases))
                : new SolrCloudState(new Dictionary<string, SolrCloudCollection>());

            

            return collectionsState;
        }

        /// <summary>
        /// Returns parsed external collections cloud state
        /// </summary>
        private SolrCloudState GetExternalCollectionsState()
        {
            var resultState = new SolrCloudState(new Dictionary<string, SolrCloudCollection>());
            IEnumerable<string> children;

            try
            {
                children = zooKeeper.GetChildren(CollectionsZkNode, true);
            }
            catch (KeeperException ex)
            {
                return resultState;
            }

            if (children == null || children.IsEmpty())
                return resultState;

            foreach (var child in children)
            {
                byte[] data;

                try
                {
                    data = zooKeeper.GetData(GetCollectionPath(child), true, null);
                }
                catch (KeeperException ex)
                {
                    data = null;
                }

                var collectionState = 
                    data != null 
                    ? SolrCloudStateParser.Parse(Encoding.Default.GetString(data)) 
                    : new SolrCloudState(new Dictionary<string, SolrCloudCollection>());
                resultState = resultState.Merge(collectionState);
            }

            return resultState;
        }

        /// <summary>
        /// Returns path to collection
        /// </summary>
        private string GetCollectionPath(string collectionName)
        {
            return string.Format("{0}/{1}/{2}", CollectionsZkNode, collectionName, CollectionState);
        }
    }
}
