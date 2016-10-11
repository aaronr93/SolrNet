using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autofac;
using AutofacContrib.SolrNet.Config;
using AutofacContrib.SolrNet.SolrCloud;
using Microsoft.Practices.Unity;
using NUnit.Framework;
using SolrNet.Cloud.ZooKeeperClient;

namespace SolrNet.Cloud.Tests
{
    public class AutofacTests
    {
        private IContainer Setup()
        {
            var builder = new ContainerBuilder();
            var module = new AutofacContrib.SolrNet.SolrCloud.SolrNetCloudModule(new FakeProvider());
            module.ConfigureContainer(new FakeProvider(), builder);
            
            return builder.Build();
        }

        [Test]
        public void Niall() {
           var temp = new SolrCloudStateProvider("172.30.10.21:2181,172.30.10.22:2181,172.30.10.23:2181/solr");
            temp.Init();
           var state = temp.GetCloudState();
            temp.GetFreshCloudState();

        }

        [Test]
        public void ShouldResolveBasicOperationsFromStartupContainer()
        {
            Assert.NotNull(
                Setup().Resolve<ISolrBasicOperations<FakeEntity>>(),
                "Should resolve basic operations from unity container");
        }

        [Test]
        public void ShouldResolveBasicReadOnlyOperationsFromStartupContainer()
        {
            Assert.NotNull(
                Setup().Resolve<ISolrBasicReadOnlyOperations<FakeEntity>>(),
                "Should resolve basic read only operations from unity container");
        }

        [Test]
        public void ShouldResolveOperationsFromStartupContainer()
        {
            Assert.NotNull(
                Setup().Resolve<ISolrOperations<FakeEntity>>(),
                "Should resolve operations from unity container");
        }

        [Test]
        public void ShouldResolveReadOnlyOperationsFromStartupContainer()
        {
            Assert.NotNull(
                Setup().Resolve<ISolrReadOnlyOperations<FakeEntity>>(),
                "Should resolve read only operations from unity container");
        }
    }
}
