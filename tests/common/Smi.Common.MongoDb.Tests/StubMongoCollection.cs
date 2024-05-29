using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Search;

// MongoDB upstream doesn't have nullable annotations, so cleanest to disable them for this file to match
#nullable disable

namespace Smi.Common.MongoDB.Tests
{
    /// <summary>
    /// Abstract base class for mocking an IMongoCollection, parameterised by a key type and value type
    /// </summary>
    public abstract class StubMongoCollection<TKey, TVal> : IMongoCollection<TVal> where TKey : struct
    {
        public virtual CollectionNamespace CollectionNamespace => throw new NotImplementedException();
        public virtual IMongoDatabase Database => throw new NotImplementedException();
        public virtual IBsonSerializer<TVal> DocumentSerializer => throw new NotImplementedException();
        public virtual IMongoIndexManager<TVal> Indexes => throw new NotImplementedException();

        /// <inheritdoc />
        public IMongoSearchIndexManager SearchIndexes => throw new NotImplementedException();

        public virtual MongoCollectionSettings Settings => throw new NotImplementedException();
        public virtual IAsyncCursor<TResult> Aggregate<TResult>(PipelineDefinition<TVal, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IAsyncCursor<TResult> Aggregate<TResult>(IClientSessionHandle session, PipelineDefinition<TVal, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(PipelineDefinition<TVal, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(IClientSessionHandle session, PipelineDefinition<TVal, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual BulkWriteResult<TVal> BulkWrite(IEnumerable<WriteModel<TVal>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual BulkWriteResult<TVal> BulkWrite(IClientSessionHandle session, IEnumerable<WriteModel<TVal>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<BulkWriteResult<TVal>> BulkWriteAsync(IEnumerable<WriteModel<TVal>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<BulkWriteResult<TVal>> BulkWriteAsync(IClientSessionHandle session, IEnumerable<WriteModel<TVal>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual long Count(FilterDefinition<TVal> filter, CountOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual long Count(IClientSessionHandle session, FilterDefinition<TVal> filter, CountOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<long> CountAsync(FilterDefinition<TVal> filter, CountOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<long> CountAsync(IClientSessionHandle session, FilterDefinition<TVal> filter, CountOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual long CountDocuments(FilterDefinition<TVal> filter, CountOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual long CountDocuments(IClientSessionHandle session, FilterDefinition<TVal> filter, CountOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<long> CountDocumentsAsync(FilterDefinition<TVal> filter, CountOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<long> CountDocumentsAsync(IClientSessionHandle session, FilterDefinition<TVal> filter, CountOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual DeleteResult DeleteMany(FilterDefinition<TVal> filter, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual DeleteResult DeleteMany(FilterDefinition<TVal> filter, DeleteOptions options, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual DeleteResult DeleteMany(IClientSessionHandle session, FilterDefinition<TVal> filter, DeleteOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<DeleteResult> DeleteManyAsync(FilterDefinition<TVal> filter, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<DeleteResult> DeleteManyAsync(FilterDefinition<TVal> filter, DeleteOptions options, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<DeleteResult> DeleteManyAsync(IClientSessionHandle session, FilterDefinition<TVal> filter, DeleteOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual DeleteResult DeleteOne(FilterDefinition<TVal> filter, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual DeleteResult DeleteOne(FilterDefinition<TVal> filter, DeleteOptions options, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual DeleteResult DeleteOne(IClientSessionHandle session, FilterDefinition<TVal> filter, DeleteOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<DeleteResult> DeleteOneAsync(FilterDefinition<TVal> filter, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<DeleteResult> DeleteOneAsync(FilterDefinition<TVal> filter, DeleteOptions options, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<DeleteResult> DeleteOneAsync(IClientSessionHandle session, FilterDefinition<TVal> filter, DeleteOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IAsyncCursor<TField> Distinct<TField>(FieldDefinition<TVal, TField> field, FilterDefinition<TVal> filter, DistinctOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IAsyncCursor<TField> Distinct<TField>(IClientSessionHandle session, FieldDefinition<TVal, TField> field, FilterDefinition<TVal> filter, DistinctOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IAsyncCursor<TField>> DistinctAsync<TField>(FieldDefinition<TVal, TField> field, FilterDefinition<TVal> filter, DistinctOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IAsyncCursor<TField>> DistinctAsync<TField>(IClientSessionHandle session, FieldDefinition<TVal, TField> field, FilterDefinition<TVal> filter, DistinctOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();

        public IAsyncCursor<TItem> DistinctMany<TItem>(FieldDefinition<TVal, IEnumerable<TItem>> field, FilterDefinition<TVal> filter, DistinctOptions options = null,
            CancellationToken cancellationToken = new CancellationToken()) =>
            throw new NotImplementedException();

        public IAsyncCursor<TItem> DistinctMany<TItem>(IClientSessionHandle session, FieldDefinition<TVal, IEnumerable<TItem>> field, FilterDefinition<TVal> filter,
            DistinctOptions options = null, CancellationToken cancellationToken = new CancellationToken()) =>
            throw new NotImplementedException();

        public Task<IAsyncCursor<TItem>> DistinctManyAsync<TItem>(FieldDefinition<TVal, IEnumerable<TItem>> field, FilterDefinition<TVal> filter, DistinctOptions options = null,
            CancellationToken cancellationToken = new CancellationToken()) =>
            throw new NotImplementedException();

        public Task<IAsyncCursor<TItem>> DistinctManyAsync<TItem>(IClientSessionHandle session, FieldDefinition<TVal, IEnumerable<TItem>> field, FilterDefinition<TVal> filter,
            DistinctOptions options = null, CancellationToken cancellationToken = new CancellationToken()) =>
            throw new NotImplementedException();

        public virtual long EstimatedDocumentCount(EstimatedDocumentCountOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<long> EstimatedDocumentCountAsync(EstimatedDocumentCountOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IAsyncCursor<TProjection> FindSync<TProjection>(FilterDefinition<TVal> filter, FindOptions<TVal, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IAsyncCursor<TProjection> FindSync<TProjection>(IClientSessionHandle session, FilterDefinition<TVal> filter, FindOptions<TVal, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(FilterDefinition<TVal> filter, FindOptions<TVal, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(IClientSessionHandle session, FilterDefinition<TVal> filter, FindOptions<TVal, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual TProjection FindOneAndDelete<TProjection>(FilterDefinition<TVal> filter, FindOneAndDeleteOptions<TVal, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual TProjection FindOneAndDelete<TProjection>(IClientSessionHandle session, FilterDefinition<TVal> filter, FindOneAndDeleteOptions<TVal, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<TProjection> FindOneAndDeleteAsync<TProjection>(FilterDefinition<TVal> filter, FindOneAndDeleteOptions<TVal, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<TProjection> FindOneAndDeleteAsync<TProjection>(IClientSessionHandle session, FilterDefinition<TVal> filter, FindOneAndDeleteOptions<TVal, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual TProjection FindOneAndReplace<TProjection>(FilterDefinition<TVal> filter, TVal replacement, FindOneAndReplaceOptions<TVal, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual TProjection FindOneAndReplace<TProjection>(IClientSessionHandle session, FilterDefinition<TVal> filter, TVal replacement, FindOneAndReplaceOptions<TVal, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<TProjection> FindOneAndReplaceAsync<TProjection>(FilterDefinition<TVal> filter, TVal replacement, FindOneAndReplaceOptions<TVal, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<TProjection> FindOneAndReplaceAsync<TProjection>(IClientSessionHandle session, FilterDefinition<TVal> filter, TVal replacement, FindOneAndReplaceOptions<TVal, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual TProjection FindOneAndUpdate<TProjection>(FilterDefinition<TVal> filter, UpdateDefinition<TVal> update, FindOneAndUpdateOptions<TVal, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual TProjection FindOneAndUpdate<TProjection>(IClientSessionHandle session, FilterDefinition<TVal> filter, UpdateDefinition<TVal> update, FindOneAndUpdateOptions<TVal, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<TProjection> FindOneAndUpdateAsync<TProjection>(FilterDefinition<TVal> filter, UpdateDefinition<TVal> update, FindOneAndUpdateOptions<TVal, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<TProjection> FindOneAndUpdateAsync<TProjection>(IClientSessionHandle session, FilterDefinition<TVal> filter, UpdateDefinition<TVal> update, FindOneAndUpdateOptions<TVal, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual void InsertOne(TVal document, InsertOneOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual void InsertOne(IClientSessionHandle session, TVal document, InsertOneOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task InsertOneAsync(TVal document, CancellationToken cancellationToken) => throw new NotImplementedException();
        public virtual Task InsertOneAsync(TVal document, InsertOneOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task InsertOneAsync(IClientSessionHandle session, TVal document, InsertOneOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual void InsertMany(IEnumerable<TVal> documents, InsertManyOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual void InsertMany(IClientSessionHandle session, IEnumerable<TVal> documents, InsertManyOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task InsertManyAsync(IEnumerable<TVal> documents, InsertManyOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task InsertManyAsync(IClientSessionHandle session, IEnumerable<TVal> documents, InsertManyOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        [Obsolete]
        public virtual IAsyncCursor<TResult> MapReduce<TResult>(BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TVal, TResult> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        [Obsolete]
        public virtual IAsyncCursor<TResult> MapReduce<TResult>(IClientSessionHandle session, BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TVal, TResult> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        [Obsolete]
        public virtual Task<IAsyncCursor<TResult>> MapReduceAsync<TResult>(BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TVal, TResult> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        [Obsolete]
        public virtual Task<IAsyncCursor<TResult>> MapReduceAsync<TResult>(IClientSessionHandle session, BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TVal, TResult> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IFilteredMongoCollection<TDerivedDocument> OfType<TDerivedDocument>() where TDerivedDocument : TVal => throw new NotImplementedException();
        public virtual ReplaceOneResult ReplaceOne(FilterDefinition<TVal> filter, TVal replacement, ReplaceOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual ReplaceOneResult ReplaceOne(FilterDefinition<TVal> filter, TVal replacement, UpdateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual ReplaceOneResult ReplaceOne(IClientSessionHandle session, FilterDefinition<TVal> filter, TVal replacement, ReplaceOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual ReplaceOneResult ReplaceOne(IClientSessionHandle session, FilterDefinition<TVal> filter, TVal replacement, UpdateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<ReplaceOneResult> ReplaceOneAsync(FilterDefinition<TVal> filter, TVal replacement, ReplaceOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<ReplaceOneResult> ReplaceOneAsync(FilterDefinition<TVal> filter, TVal replacement, UpdateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<ReplaceOneResult> ReplaceOneAsync(IClientSessionHandle session, FilterDefinition<TVal> filter, TVal replacement, ReplaceOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<ReplaceOneResult> ReplaceOneAsync(IClientSessionHandle session, FilterDefinition<TVal> filter, TVal replacement, UpdateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual UpdateResult UpdateMany(FilterDefinition<TVal> filter, UpdateDefinition<TVal> update, UpdateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual UpdateResult UpdateMany(IClientSessionHandle session, FilterDefinition<TVal> filter, UpdateDefinition<TVal> update, UpdateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<UpdateResult> UpdateManyAsync(FilterDefinition<TVal> filter, UpdateDefinition<TVal> update, UpdateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<UpdateResult> UpdateManyAsync(IClientSessionHandle session, FilterDefinition<TVal> filter, UpdateDefinition<TVal> update, UpdateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual UpdateResult UpdateOne(FilterDefinition<TVal> filter, UpdateDefinition<TVal> update, UpdateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual UpdateResult UpdateOne(IClientSessionHandle session, FilterDefinition<TVal> filter, UpdateDefinition<TVal> update, UpdateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<UpdateResult> UpdateOneAsync(FilterDefinition<TVal> filter, UpdateDefinition<TVal> update, UpdateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<UpdateResult> UpdateOneAsync(IClientSessionHandle session, FilterDefinition<TVal> filter, UpdateDefinition<TVal> update, UpdateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IChangeStreamCursor<TResult> Watch<TResult>(PipelineDefinition<ChangeStreamDocument<TVal>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IChangeStreamCursor<TResult> Watch<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<TVal>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(PipelineDefinition<ChangeStreamDocument<TVal>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<TVal>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IMongoCollection<TVal> WithReadConcern(ReadConcern readConcern) => throw new NotImplementedException();
        public virtual IMongoCollection<TVal> WithReadPreference(ReadPreference readPreference) => throw new NotImplementedException();
        public virtual IMongoCollection<TVal> WithWriteConcern(WriteConcern writeConcern) => throw new NotImplementedException();
        public void AggregateToCollection<TResult>(PipelineDefinition<TVal, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public void AggregateToCollection<TResult>(IClientSessionHandle session, PipelineDefinition<TVal, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AggregateToCollectionAsync<TResult>(PipelineDefinition<TVal, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AggregateToCollectionAsync<TResult>(IClientSessionHandle session, PipelineDefinition<TVal, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
