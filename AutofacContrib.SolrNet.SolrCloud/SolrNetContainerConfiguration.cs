using System;
using System.Collections.Generic;
using System.Configuration;
using Autofac;
using Autofac.Core;
using AutofacContrib.SolrNet.Config;
using SolrNet;
using SolrNet.Impl;
using SolrNet.Impl.DocumentPropertyVisitors;
using SolrNet.Impl.FacetQuerySerializers;
using SolrNet.Impl.FieldParsers;
using SolrNet.Impl.FieldSerializers;
using SolrNet.Impl.QuerySerializers;
using SolrNet.Impl.ResponseParsers;
using SolrNet.Mapping;
using SolrNet.Mapping.Validation;
using SolrNet.Mapping.Validation.Rules;
using SolrNet.Schema;
using SolrNet.Utils;
using Unity.SolrNetIntegration;

namespace AutofacContrib.SolrNet.SolrCloud {
    public class SolrNetContainerConfiguration : Module
    {
        private readonly SolrServers solrServers;

        /// <summary>
        ///   Register multi-core server
        /// </summary>
        /// <param name = "solrServers"></param>
        public SolrNetContainerConfiguration(SolrServers solrServers)
        {
            this.solrServers = solrServers;
        }
        
        

        protected override void Load(ContainerBuilder builder)
        {
            if (solrServers != null)
                ConfigureContainer(solrServers, builder);
            else
                throw new ConfigurationErrorsException("SolrNetModule Configurations Error!");
        }

        public ContainerBuilder ConfigureContainer(SolrServers solrServers, ContainerBuilder container) {
            
            container.RegisterType<MemoizingMappingManager>().WithParameter(ResolvedParameter.ForKeyed<AttributesMappingManager>(new AttributesMappingManager())).AsImplementedInterfaces();
            container.RegisterGeneric(typeof (SolrDocumentActivator<>)).AsImplementedInterfaces();
            container.RegisterGeneric(typeof (SolrQueryExecuter<>)).AsImplementedInterfaces();
            container.RegisterType<DefaultDocumentVisitor>().AsImplementedInterfaces();
            container.RegisterType<MappingValidator>().AsImplementedInterfaces();
            RegisterParsers(container);
            RegisterValidationRules(container);
            RegisterSerializers(container);

            AddCoresFromConfig(solrServers, container);

            return container;
        }

        private void RegisterValidationRules(ContainerBuilder container) {
            var validationRules = new[] {
                typeof (MappedPropertiesIsInSolrSchemaRule),
                typeof (RequiredFieldsAreMappedRule),
                typeof (UniqueKeyMatchesMappingRule),
                typeof(MultivaluedMappedToCollectionRule),
            };

            foreach (var validationRule in validationRules) {
                container.RegisterType(validationRule).AsImplementedInterfaces();
            }
        }

        private void RegisterSerializers(ContainerBuilder container) {
            container.RegisterGeneric(typeof (SolrDocumentSerializer<>)).AsImplementedInterfaces();
            container.RegisterType<SolrDictionarySerializer>().AsImplementedInterfaces();
            container.RegisterType<DefaultFieldSerializer>().AsImplementedInterfaces();
            container.RegisterType<DefaultQuerySerializer>().AsImplementedInterfaces();
            container.RegisterType<DefaultFacetQuerySerializer>().AsImplementedInterfaces();
        }

        private void RegisterParsers(ContainerBuilder container) {
            container.RegisterGeneric(typeof (SolrDocumentResponseParser<>)).AsImplementedInterfaces();
            container.RegisterType<SolrDictionaryDocumentResponseParser>().AsImplementedInterfaces();
            container.RegisterGeneric(typeof(DefaultResponseParser<>)).AsImplementedInterfaces();
           // container.RegisterGeneric(typeof(DefaultResponseParser<>)).AsImplementedInterfaces(); unitfix?

            container.RegisterType<HeaderResponseParser<string>>().AsImplementedInterfaces();
            container.RegisterType<ExtractResponseParser>().AsImplementedInterfaces();
            container.RegisterGeneric(typeof (SolrMoreLikeThisHandlerQueryResultsParser<>)).AsImplementedInterfaces();
            container.RegisterType<DefaultFieldParser>().AsImplementedInterfaces();
            container.RegisterType<SolrSchemaParser>().AsImplementedInterfaces();
            container.RegisterType<SolrDIHStatusParser>().AsImplementedInterfaces();
            container.RegisterType<SolrStatusResponseParser>().AsImplementedInterfaces();
            container.RegisterType<SolrCoreAdmin>().AsImplementedInterfaces();
        }

        private void RegisterCore(SolrCore core, ContainerBuilder container) {

            string connectionId = GetCoreConnectionId(core.Id);
            container.RegisterType(typeof(SolrConnection))
                .Named(connectionId, typeof(ISolrConnection))
                .WithParameters(new[] {
                    new NamedParameter("serverURL", core.Url)
                });

            
            RegisterAll(core, container, isNamed : false);
            
            RegisterAll(core, container);
        }

        private static void RegisterAll(SolrCore core, ContainerBuilder container, bool isNamed = true) {
            RegisterSolrQueryExecuter(core, container, isNamed);
            RegisterBasicOperations(core, container, isNamed);
            RegisterSolrOperations(core, container, isNamed);
        }

        private static void RegisterSolrOperations(SolrCore core, ContainerBuilder container, bool isNamed = true) {
            var ISolrReadOnlyOperations = typeof (ISolrReadOnlyOperations<>).MakeGenericType(core.DocumentType);
            var ISolrBasicOperations = typeof (ISolrBasicOperations<>).MakeGenericType(core.DocumentType);
            var ISolrOperations = typeof (ISolrOperations<>).MakeGenericType(core.DocumentType);
            var SolrServer = typeof (SolrServer<>).MakeGenericType(core.DocumentType);
            var SolrBasicServer = typeof(SolrBasicServer<>).MakeGenericType(core.DocumentType);

            var registrationId = isNamed ? core.Id : null;
            string connectionId = GetCoreConnectionId(core.Id);

            container.RegisterType(SolrServer)
               .Named(core.Id, ISolrOperations)
               .As(ISolrOperations)
               .WithParameters(new[] {
                    new ResolvedParameter((p, c) => p.Name == "basicServer", (p, c) => c.ResolveNamed(connectionId, ISolrBasicOperations)),
               });

            container.RegisterType(SolrServer)
                .Named(core.Id, ISolrReadOnlyOperations)
                .As(ISolrReadOnlyOperations)
                .WithParameters(new[] {
                    new ResolvedParameter((p, c) => p.Name == "basicServer", (p, c) => c.ResolveNamed(core.Id + SolrBasicServer, ISolrBasicOperations)),
                });

            
        }

        private static void RegisterBasicOperations(SolrCore core, ContainerBuilder container, bool isNamed = true) {
            var ISolrQueryExecuter = typeof (ISolrQueryExecuter<>).MakeGenericType(core.DocumentType);
            var SolrQueryExecuter = typeof(SolrQueryExecuter<>).MakeGenericType(core.DocumentType);
            string coreConnectionId = GetCoreConnectionId(core.Id);

            var ISolrBasicOperations = typeof(ISolrBasicOperations<>).MakeGenericType(core.DocumentType);
            var ISolrBasicReadOnlyOperations = typeof(ISolrBasicReadOnlyOperations<>).MakeGenericType(core.DocumentType);
            var SolrBasicServer = typeof(SolrBasicServer<>).MakeGenericType(core.DocumentType);

            container.RegisterType(SolrBasicServer)
                .Named(core.Id + SolrBasicServer, ISolrBasicOperations)
                .WithParameters(new[] {
                    new ResolvedParameter((p, c) => p.Name == "connection", (p, c) => c.ResolveNamed(coreConnectionId, typeof (ISolrConnection))),
                    new ResolvedParameter((p, c) => p.Name == "queryExecuter", (p, c) => c.ResolveNamed(core.Id + SolrQueryExecuter, ISolrQueryExecuter))
                });

            container.RegisterType(SolrBasicServer)
                .Named(core.Id + SolrBasicServer, ISolrBasicReadOnlyOperations)
                .WithParameters(new[] {
                    new ResolvedParameter((p, c) => p.Name == "connection", (p, c) => c.ResolveNamed(coreConnectionId, typeof (ISolrConnection))),
                    new ResolvedParameter((p, c) => p.Name == "queryExecuter", (p, c) => c.ResolveNamed(core.Id + SolrQueryExecuter, ISolrQueryExecuter))
                });

            
        }

        private static void RegisterSolrQueryExecuter(SolrCore core, ContainerBuilder container, bool isNamed = true) {

            var ISolrQueryExecuter = typeof(ISolrQueryExecuter<>).MakeGenericType(core.DocumentType);
            var SolrQueryExecuter = typeof(SolrQueryExecuter<>).MakeGenericType(core.DocumentType);

            string coreConnectionId = GetCoreConnectionId(core.Id);
            container.RegisterType(SolrQueryExecuter)
                .Named(core.Id + SolrQueryExecuter, ISolrQueryExecuter)
                .WithParameters(new[] {
                    new ResolvedParameter((p, c) => p.Name == "connection", (p, c) => c.ResolveNamed(coreConnectionId, typeof (ISolrConnection))),
                });
            
        }

        private static string GetCoreConnectionId(string coreId) {
            return coreId + typeof (SolrConnection);
        }

        private void AddCoresFromConfig(SolrServers solrServers, ContainerBuilder container) {
            if (solrServers == null) {
                return;
            }

            
            foreach (SolrServerElement server in solrServers)
            {
                RegisterCore(GetCoreFrom(server), container);
            }
        }

        //copied directly from SolrNetModule, should i extend that or inherit from it? NL
        private static SolrCore GetCoreFrom(SolrServerElement server)
        {
            var id = server.Id ?? Guid.NewGuid().ToString();
            var documentType = GetCoreDocumentType(server);
            var coreUrl = GetCoreUrl(server);
            UriValidator.ValidateHTTP(coreUrl);
            return new SolrCore(id, documentType, coreUrl);
        }

        private static string GetCoreUrl(SolrServerElement server) {
            var url = server.Url;
            if (string.IsNullOrEmpty(url)) {
                throw new ConfigurationErrorsException("Core url missing in SolrNet core configuration");
            }
            return url;
        }

        private static Type GetCoreDocumentType(SolrServerElement server) {
            var documentType = server.DocumentType;

            if (string.IsNullOrEmpty(documentType)) {
                throw new ConfigurationErrorsException("Document type missing in SolrNet core configuration");
            }

            Type type;

            try {
                type = Type.GetType(documentType);
            } catch (Exception e) {
                throw new ConfigurationErrorsException(string.Format("Error getting document type '{0}'", documentType), e);
            }

            if (type == null)
                throw new ConfigurationErrorsException(string.Format("Error getting document type '{0}'", documentType));

            return type;
        }
    }
}