namespace SolrNet.Cloud {
    public interface ICollectionResolver<T> {
        string CollectionName { get; }
        bool IsPostConnection { get; }
    }
}