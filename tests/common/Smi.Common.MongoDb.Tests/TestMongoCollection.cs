using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;


namespace Smi.Common.MongoDB.Tests
{
    /// <summary>
    /// Abstract base class for mocking an TestMongoCollection
    /// </summary>
    public class TestMongoCollection<T> : IMongoCollection<T>
    {
        public virtual CollectionNamespace CollectionNamespace { get; }
        public virtual IMongoDatabase Database { get; }
        public virtual IBsonSerializer<T> DocumentSerializer { get; }
        public virtual IMongoIndexManager<T> Indexes { get; }
        public virtual MongoCollectionSettings Settings { get; }
        public virtual IAsyncCursor<TResult> Aggregate<TResult>(PipelineDefinition<T, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IAsyncCursor<TResult> Aggregate<TResult>(IClientSessionHandle session, PipelineDefinition<T, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(PipelineDefinition<T, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(IClientSessionHandle session, PipelineDefinition<T, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual BulkWriteResult<T> BulkWrite(IEnumerable<WriteModel<T>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual BulkWriteResult<T> BulkWrite(IClientSessionHandle session, IEnumerable<WriteModel<T>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<BulkWriteResult<T>> BulkWriteAsync(IEnumerable<WriteModel<T>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<BulkWriteResult<T>> BulkWriteAsync(IClientSessionHandle session, IEnumerable<WriteModel<T>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual long Count(FilterDefinition<T> filter, CountOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual long Count(IClientSessionHandle session, FilterDefinition<T> filter, CountOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<long> CountAsync(FilterDefinition<T> filter, CountOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<long> CountAsync(IClientSessionHandle session, FilterDefinition<T> filter, CountOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual long CountDocuments(FilterDefinition<T> filter, CountOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual long CountDocuments(IClientSessionHandle session, FilterDefinition<T> filter, CountOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<long> CountDocumentsAsync(FilterDefinition<T> filter, CountOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<long> CountDocumentsAsync(IClientSessionHandle session, FilterDefinition<T> filter, CountOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual DeleteResult DeleteMany(FilterDefinition<T> filter, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual DeleteResult DeleteMany(FilterDefinition<T> filter, DeleteOptions options, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual DeleteResult DeleteMany(IClientSessionHandle session, FilterDefinition<T> filter, DeleteOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<DeleteResult> DeleteManyAsync(FilterDefinition<T> filter, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<DeleteResult> DeleteManyAsync(FilterDefinition<T> filter, DeleteOptions options, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<DeleteResult> DeleteManyAsync(IClientSessionHandle session, FilterDefinition<T> filter, DeleteOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual DeleteResult DeleteOne(FilterDefinition<T> filter, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual DeleteResult DeleteOne(FilterDefinition<T> filter, DeleteOptions options, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual DeleteResult DeleteOne(IClientSessionHandle session, FilterDefinition<T> filter, DeleteOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<DeleteResult> DeleteOneAsync(FilterDefinition<T> filter, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<DeleteResult> DeleteOneAsync(FilterDefinition<T> filter, DeleteOptions options, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<DeleteResult> DeleteOneAsync(IClientSessionHandle session, FilterDefinition<T> filter, DeleteOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IAsyncCursor<TField> Distinct<TField>(FieldDefinition<T, TField> field, FilterDefinition<T> filter, DistinctOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IAsyncCursor<TField> Distinct<TField>(IClientSessionHandle session, FieldDefinition<T, TField> field, FilterDefinition<T> filter, DistinctOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IAsyncCursor<TField>> DistinctAsync<TField>(FieldDefinition<T, TField> field, FilterDefinition<T> filter, DistinctOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IAsyncCursor<TField>> DistinctAsync<TField>(IClientSessionHandle session, FieldDefinition<T, TField> field, FilterDefinition<T> filter, DistinctOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual long EstimatedDocumentCount(EstimatedDocumentCountOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<long> EstimatedDocumentCountAsync(EstimatedDocumentCountOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IAsyncCursor<TProjection> FindSync<TProjection>(FilterDefinition<T> filter, FindOptions<T, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IAsyncCursor<TProjection> FindSync<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, FindOptions<T, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(FilterDefinition<T> filter, FindOptions<T, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, FindOptions<T, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual TProjection FindOneAndDelete<TProjection>(FilterDefinition<T> filter, FindOneAndDeleteOptions<T, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual TProjection FindOneAndDelete<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, FindOneAndDeleteOptions<T, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<TProjection> FindOneAndDeleteAsync<TProjection>(FilterDefinition<T> filter, FindOneAndDeleteOptions<T, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<TProjection> FindOneAndDeleteAsync<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, FindOneAndDeleteOptions<T, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual TProjection FindOneAndReplace<TProjection>(FilterDefinition<T> filter, T replacement, FindOneAndReplaceOptions<T, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual TProjection FindOneAndReplace<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, T replacement, FindOneAndReplaceOptions<T, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<TProjection> FindOneAndReplaceAsync<TProjection>(FilterDefinition<T> filter, T replacement, FindOneAndReplaceOptions<T, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<TProjection> FindOneAndReplaceAsync<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, T replacement, FindOneAndReplaceOptions<T, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual TProjection FindOneAndUpdate<TProjection>(FilterDefinition<T> filter, UpdateDefinition<T> update, FindOneAndUpdateOptions<T, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual TProjection FindOneAndUpdate<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, UpdateDefinition<T> update, FindOneAndUpdateOptions<T, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<TProjection> FindOneAndUpdateAsync<TProjection>(FilterDefinition<T> filter, UpdateDefinition<T> update, FindOneAndUpdateOptions<T, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<TProjection> FindOneAndUpdateAsync<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, UpdateDefinition<T> update, FindOneAndUpdateOptions<T, TProjection> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual void InsertOne(T document, InsertOneOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual void InsertOne(IClientSessionHandle session, T document, InsertOneOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task InsertOneAsync(T document, CancellationToken _cancellationToken) => throw new NotImplementedException();
        public virtual Task InsertOneAsync(T document, InsertOneOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task InsertOneAsync(IClientSessionHandle session, T document, InsertOneOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual void InsertMany(IEnumerable<T> documents, InsertManyOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual void InsertMany(IClientSessionHandle session, IEnumerable<T> documents, InsertManyOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task InsertManyAsync(IEnumerable<T> documents, InsertManyOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task InsertManyAsync(IClientSessionHandle session, IEnumerable<T> documents, InsertManyOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IAsyncCursor<TResult> MapReduce<TResult>(BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<T, TResult> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IAsyncCursor<TResult> MapReduce<TResult>(IClientSessionHandle session, BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<T, TResult> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IAsyncCursor<TResult>> MapReduceAsync<TResult>(BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<T, TResult> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IAsyncCursor<TResult>> MapReduceAsync<TResult>(IClientSessionHandle session, BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<T, TResult> options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IFilteredMongoCollection<TDerivedDocument> OfType<TDerivedDocument>() where TDerivedDocument : T => throw new NotImplementedException();
        public virtual ReplaceOneResult ReplaceOne(FilterDefinition<T> filter, T replacement, UpdateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual ReplaceOneResult ReplaceOne(IClientSessionHandle session, FilterDefinition<T> filter, T replacement, UpdateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<ReplaceOneResult> ReplaceOneAsync(FilterDefinition<T> filter, T replacement, UpdateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<ReplaceOneResult> ReplaceOneAsync(IClientSessionHandle session, FilterDefinition<T> filter, T replacement, UpdateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual UpdateResult UpdateMany(FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual UpdateResult UpdateMany(IClientSessionHandle session, FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<UpdateResult> UpdateManyAsync(FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<UpdateResult> UpdateManyAsync(IClientSessionHandle session, FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual UpdateResult UpdateOne(FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual UpdateResult UpdateOne(IClientSessionHandle session, FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<UpdateResult> UpdateOneAsync(FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<UpdateResult> UpdateOneAsync(IClientSessionHandle session, FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IChangeStreamCursor<TResult> Watch<TResult>(PipelineDefinition<ChangeStreamDocument<T>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IChangeStreamCursor<TResult> Watch<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<T>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(PipelineDefinition<ChangeStreamDocument<T>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<T>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException();
        public virtual IMongoCollection<T> WithReadConcern(ReadConcern readConcern) => throw new NotImplementedException();
        public virtual IMongoCollection<T> WithReadPreference(ReadPreference readPreference) => throw new NotImplementedException();
        public virtual IMongoCollection<T> WithWriteConcern(WriteConcern writeConcern) => throw new NotImplementedException();
    }
}
