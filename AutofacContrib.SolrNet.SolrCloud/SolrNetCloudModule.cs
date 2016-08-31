using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Autofac;
using Autofac.Core;
using SolrNet;
using SolrNet.Cloud;

namespace AutofacContrib.SolrNet.SolrCloud {
    public class SolrNetCloudModule : Module {

        public SolrNetCloudModule(ISolrCloudStateProvider cloudStateProvider) {
            CloudStateProvider = cloudStateProvider;
        }

        public ISolrCloudStateProvider CloudStateProvider { get; set; }

        protected override void Load(ContainerBuilder builder)
        {
                ConfigureContainer(CloudStateProvider, builder);
        }

        /// <summary>
        /// Returns configured unity container
        /// </summary>
        public ContainerBuilder ConfigureContainer(ISolrCloudStateProvider cloudStateProvider, ContainerBuilder container)
        {
            if (cloudStateProvider == null)
                throw new ArgumentNullException("cloudStateProvider");
            if (container == null)
                throw new ArgumentNullException("container");

            cloudStateProvider.Init();
            container.RegisterInstance(cloudStateProvider).Named<ISolrCloudStateProvider>(cloudStateProvider.Key).AsImplementedInterfaces();


            //RegisterFirstCollection(cloudStateProvider, container);

            //foreach (var collection in cloudStateProvider.GetCloudState().Collections.Keys)
            //{ 
            //    RegisterCollection(cloudStateProvider, collection, container);
            //}

            //unlike the unity provider, I want the user to have to be explicit with their collection name
            //by implementing ICollectionResolver
            container.RegisterGeneric(typeof (SolrCloudOperations<>))
                .FindConstructorsWith(x => x.GetConstructors().Where(y => y.GetParameters().Any(z => z.Name == "collectionResolver")).ToArray())
                     //.UsingConstructor(typeof (ISolrCloudStateProvider),
                     //                  typeof (ISolrOperationsProvider),
                     //                  typeof (ICollectionResolver<>))
                     .AsSelf().AsImplementedInterfaces();

            container.RegisterGeneric(typeof (SolrCloudBasicOperations<>))
                .FindConstructorsWith(x => x.GetConstructors().Where(y => y.GetParameters().Any(z => z.Name == "collectionResolver")).ToArray())
                     //.UsingConstructor(typeof (ISolrCloudStateProvider),
                     //                  typeof (ISolrOperationsProvider),
                     //                  typeof (ICollectionResolver<>))
                     .AsSelf().AsImplementedInterfaces();


            container.RegisterInstance(cloudStateProvider).AsSelf().AsImplementedInterfaces();

            container.RegisterInstance<ISolrOperationsProvider>(new OperationsProvider()).AsSelf().AsImplementedInterfaces();
            return container;
        }
        


        private class OperationsProvider : ISolrOperationsProvider
        {
            public ISolrBasicOperations<T> GetBasicOperations<T>(string url, bool isPostConnection = false)
            {
                return global::SolrNet.SolrNet.GetBasicServer<T>(url, isPostConnection);
            }

            public ISolrOperations<T> GetOperations<T>(string url, bool isPostConnection = false)
            {
                return global::SolrNet.SolrNet.GetServer<T>(url, isPostConnection);
            }
        }
    }
}