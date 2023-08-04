using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;


namespace Smi.Common.MongoDB.Tests
{
    /// <summary>
    /// Abstract base class for mocking an IMongoDatabase
    /// </summary>
    public abstract class StubMongoDatabase : IMongoDatabase
    {
        public virtual IMongoClient Client { get; } = null!;
        public virtual DatabaseNamespace DatabaseNamespace { get; } = null!;
        public virtual MongoDatabaseSettings Settings { get; } = null!;
        public virtual IAsyncCursor<TResult> Aggregate<TResult>(PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IAsyncCursor<TResult> Aggregate<TResult>(IClientSessionHandle session, PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(IClientSessionHandle session, PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public void AggregateToCollection<TResult>(PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public void AggregateToCollection<TResult>(IClientSessionHandle session, PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AggregateToCollectionAsync<TResult>(PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AggregateToCollectionAsync<TResult>(IClientSessionHandle session, PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual void CreateCollection(string name, CreateCollectionOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual void CreateCollection(IClientSessionHandle session, string name, CreateCollectionOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task CreateCollectionAsync(string name, CreateCollectionOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task CreateCollectionAsync(IClientSessionHandle session, string name, CreateCollectionOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual void CreateView<TDocument, TResult>(string viewName, string viewOn, PipelineDefinition<TDocument, TResult> pipeline, CreateViewOptions<TDocument>? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual void CreateView<TDocument, TResult>(IClientSessionHandle session, string viewName, string viewOn, PipelineDefinition<TDocument, TResult> pipeline, CreateViewOptions<TDocument>? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task CreateViewAsync<TDocument, TResult>(string viewName, string viewOn, PipelineDefinition<TDocument, TResult> pipeline, CreateViewOptions<TDocument>? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task CreateViewAsync<TDocument, TResult>(IClientSessionHandle session, string viewName, string viewOn, PipelineDefinition<TDocument, TResult> pipeline, CreateViewOptions<TDocument>? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual void DropCollection(string name, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();

        public virtual void DropCollection(string name, DropCollectionOptions options,
            CancellationToken cancellationToken = new CancellationToken()) =>
            throw new NotImplementedException();

        public virtual void DropCollection(IClientSessionHandle session, string name, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();

        public virtual void DropCollection(IClientSessionHandle session, string name, DropCollectionOptions options,
            CancellationToken cancellationToken = new CancellationToken()) =>
            throw new NotImplementedException();

        public virtual Task DropCollectionAsync(string name, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();

        public virtual Task DropCollectionAsync(string name, DropCollectionOptions options,
            CancellationToken cancellationToken = new CancellationToken()) =>
            throw new NotImplementedException();

        public virtual Task DropCollectionAsync(IClientSessionHandle session, string name, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();

        public virtual Task DropCollectionAsync(IClientSessionHandle session, string name, DropCollectionOptions options,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public virtual IMongoCollection<TDocument> GetCollection<TDocument>(string name, MongoCollectionSettings? settings = null) => throw new NotImplementedException();
        public virtual IAsyncCursor<string> ListCollectionNames(ListCollectionNamesOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IAsyncCursor<string> ListCollectionNames(IClientSessionHandle session, ListCollectionNamesOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IAsyncCursor<string>> ListCollectionNamesAsync(ListCollectionNamesOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IAsyncCursor<string>> ListCollectionNamesAsync(IClientSessionHandle session, ListCollectionNamesOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IAsyncCursor<BsonDocument> ListCollections(ListCollectionsOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IAsyncCursor<BsonDocument> ListCollections(IClientSessionHandle session, ListCollectionsOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IAsyncCursor<BsonDocument>> ListCollectionsAsync(ListCollectionsOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IAsyncCursor<BsonDocument>> ListCollectionsAsync(IClientSessionHandle session, ListCollectionsOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual void RenameCollection(string oldName, string newName, RenameCollectionOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual void RenameCollection(IClientSessionHandle session, string oldName, string newName, RenameCollectionOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task RenameCollectionAsync(string oldName, string newName, RenameCollectionOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task RenameCollectionAsync(IClientSessionHandle session, string oldName, string newName, RenameCollectionOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual TResult RunCommand<TResult>(Command<TResult> command, ReadPreference? readPreference = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual TResult RunCommand<TResult>(IClientSessionHandle session, Command<TResult> command, ReadPreference? readPreference = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<TResult> RunCommandAsync<TResult>(Command<TResult> command, ReadPreference? readPreference = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<TResult> RunCommandAsync<TResult>(IClientSessionHandle session, Command<TResult> command, ReadPreference? readPreference = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IChangeStreamCursor<TResult> Watch<TResult>(PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IChangeStreamCursor<TResult> Watch<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IMongoDatabase WithReadConcern(ReadConcern readConcern) => throw new NotImplementedException();
        public virtual IMongoDatabase WithReadPreference(ReadPreference readPreference) => throw new NotImplementedException();
        public virtual IMongoDatabase WithWriteConcern(WriteConcern writeConcern) => throw new NotImplementedException();
    }
}
