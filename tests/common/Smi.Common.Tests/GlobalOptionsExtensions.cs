﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using FAnsi;
using MongoDB.Driver;
using RabbitMQ.Client;
using Smi.Common.Options;

namespace Smi.Common.Tests
{
    public static class GlobalOptionsExtensions
    {
        /// <summary>
        /// Updates the <see cref="GlobalOptions"/> to reference the provided arguments.  Passing null for arguments results
        /// in the associated settings being set to null.
        /// </summary>
        /// <param name="g"></param>
        /// <param name="rabbit"></param>
        /// <param name="mongo"></param>
        /// <param name="relational"></param>
        /// <param name="catalogueConnectionString">Connection string to RDMP catalogue database e.g. TEST_Catalogue</param>
        /// <param name="dataExportConnectionStringBuilder">Connection string to RDMP data export database e.g. TEST_DataExport</param>
        public static void UseTestValues(this GlobalOptions g, ConnectionFactory rabbit,MongoClientSettings mongo,RequiresRelationalDb.ConStrs relational, DbConnectionStringBuilder catalogueConnectionString, DbConnectionStringBuilder dataExportConnectionStringBuilder)
        {
            //Rabbit
            g.RabbitOptions.RabbitMqHostName = rabbit?.HostName;
            g.RabbitOptions.RabbitMqHostPort = rabbit?.Port ?? -1;
            g.RabbitOptions.RabbitMqVirtualHost = rabbit?.VirtualHost;
            g.RabbitOptions.RabbitMqUserName = rabbit?.UserName;
            g.RabbitOptions.RabbitMqPassword = rabbit?.Password;

            //RDMP
            g.RDMPOptions.CatalogueConnectionString = catalogueConnectionString?.ConnectionString;
            g.RDMPOptions.DataExportConnectionString = dataExportConnectionStringBuilder?.ConnectionString;
            
            //Mongo Db
            g.MongoDatabases.DicomStoreOptions.HostName = mongo?.Server?.Host;
            g.MongoDatabases.ExtractionStoreOptions.HostName = mongo?.Server?.Host;
            g.MongoDatabases.DeadLetterStoreOptions.HostName = mongo?.Server?.Host;

            g.MongoDatabases.DicomStoreOptions.Port = mongo?.Server?.Port ?? -1;
            g.MongoDatabases.ExtractionStoreOptions.Port = mongo?.Server?.Port ?? -1;
            g.MongoDatabases.DeadLetterStoreOptions.Port = mongo?.Server?.Port ?? -1;

            g.MongoDatabases.DicomStoreOptions.UserName = mongo?.Credential?.Username;
            g.MongoDatabases.ExtractionStoreOptions.UserName = mongo?.Credential?.Username;
            g.MongoDatabases.DeadLetterStoreOptions.UserName = mongo?.Credential?.Username;

            //Relational Databases
            var mappingDb = relational?.GetServer(DatabaseType.MicrosoftSQLServer)?.ExpectDatabase("TEST_MappingDatabase");

            g.IdentifierMapperOptions.MappingConnectionString = mappingDb?.Server?.Builder?.ConnectionString;
            g.IdentifierMapperOptions.MappingDatabaseType = mappingDb?.Server?.DatabaseType ?? DatabaseType.MicrosoftSQLServer;
            g.IdentifierMapperOptions.MappingTableName = mappingDb?.ExpectTable("MappingTable").GetFullyQualifiedName();


            g.DeadLetterReprocessorOptions.DeadLetterConsumerOptions.QoSPrefetchCount = 1;
            g.DicomRelationalMapperOptions.QoSPrefetchCount = 1;
            g.CohortExtractorOptions.QoSPrefetchCount = 1;
            g.CohortPackagerOptions.ExtractRequestInfoOptions.QoSPrefetchCount = 1;
            g.CohortPackagerOptions.FileCollectionInfoOptions.QoSPrefetchCount = 1;
            g.CohortPackagerOptions.AnonFailedOptions.QoSPrefetchCount = 1;
            g.CohortPackagerOptions.VerificationStatusOptions.QoSPrefetchCount = 1;
            g.DicomTagReaderOptions.QoSPrefetchCount = 1;
            g.IdentifierMapperOptions.QoSPrefetchCount = 1;
            g.MongoDbPopulatorOptions.SeriesQueueConsumerOptions.QoSPrefetchCount = 1;
            g.MongoDbPopulatorOptions.ImageQueueConsumerOptions.QoSPrefetchCount = 1;
        }
    }
}
