using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;


namespace Smi.Common.MongoDb.Tests
{
    /// <summary>
    /// Abstract base class for mocking an IMongoClient
    /// </summary>
    public abstract class StubMongoClient : IMongoClient
    {
        public ICluster Cluster { get; } = null!;
        public MongoClientSettings Settings { get; } = null!;
        public virtual void DropDatabase(string name, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual void DropDatabase(IClientSessionHandle session, string name, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task DropDatabaseAsync(string name, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task DropDatabaseAsync(IClientSessionHandle session, string name, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IMongoDatabase GetDatabase(string name, MongoDatabaseSettings? settings = null) => throw new NotImplementedException();
        public virtual IAsyncCursor<string> ListDatabaseNames(CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IAsyncCursor<string> ListDatabaseNames(IClientSessionHandle session, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public IAsyncCursor<string> ListDatabaseNames(ListDatabaseNamesOptions options, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncCursor<string> ListDatabaseNames(IClientSessionHandle session, ListDatabaseNamesOptions options, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task<IAsyncCursor<string>> ListDatabaseNamesAsync(CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IAsyncCursor<string>> ListDatabaseNamesAsync(IClientSessionHandle session, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public Task<IAsyncCursor<string>> ListDatabaseNamesAsync(ListDatabaseNamesOptions options, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IAsyncCursor<string>> ListDatabaseNamesAsync(IClientSessionHandle session, ListDatabaseNamesOptions options, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual IAsyncCursor<BsonDocument> ListDatabases(CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IAsyncCursor<BsonDocument> ListDatabases(ListDatabasesOptions options, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IAsyncCursor<BsonDocument> ListDatabases(IClientSessionHandle session, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IAsyncCursor<BsonDocument> ListDatabases(IClientSessionHandle session, ListDatabasesOptions options, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(ListDatabasesOptions options, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(IClientSessionHandle session, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(IClientSessionHandle session, ListDatabasesOptions options, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IClientSessionHandle StartSession(ClientSessionOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IClientSessionHandle> StartSessionAsync(ClientSessionOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IChangeStreamCursor<TResult> Watch<TResult>(PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IChangeStreamCursor<TResult> Watch<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IMongoClient WithReadConcern(ReadConcern readConcern) => throw new NotImplementedException();
        public virtual IMongoClient WithReadPreference(ReadPreference readPreference) => throw new NotImplementedException();
        public virtual IMongoClient WithWriteConcern(WriteConcern writeConcern) => throw new NotImplementedException();
    }
}
