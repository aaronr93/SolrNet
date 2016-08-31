using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autofac;
using AutofacContrib.SolrNet.Config;
using AutofacContrib.SolrNet.SolrCloud;
using Microsoft.Practices.Unity;
using NUnit.Framework;

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
