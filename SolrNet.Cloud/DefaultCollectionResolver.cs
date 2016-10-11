using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolrNet.Cloud
{
    public class DefaultCollectionResolver<T>: ICollectionResolver<T>
    {
        public string CollectionName => "";

        public bool IsPostConnection => false;
    }
}
