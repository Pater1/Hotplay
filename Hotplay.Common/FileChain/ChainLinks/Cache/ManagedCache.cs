using FubarDev.WebDavServer;
using FubarDev.WebDavServer.FileSystem;
using Hotplay.Common.Caches.Allocator;
using Hotplay.Common.DocumentStore;
using Hotplay.Common.FileChain.Structure;
using Hotplay.Common.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hotplay.Common.FileChain.ChainLinks.Cache {
    public class ManagedCache: IDocumentCache {
        public IChainedFileSystem FileSystem { get; set; }
        private DocumentCache RootCache { get; set; }

        public AsyncLazy<ICollection> Root => RootCache.Root;

        public bool SupportsRangedRead => RootCache.SupportsRangedRead;

        public ManagedCache(IDocumentStore store, ICacheAllocator allocator): 
            this(new DocumentCache(store, allocator)) {}
        public ManagedCache(DocumentCache rootCache) {
            RootCache = rootCache;
        }

        public Task<float> FitnessToStoreAsync(string key, Func<Task<IDocument>> doc, Func<Task<Stream>> stream) => RootCache.FitnessToStoreAsync(key, doc, stream);

        public Task<(bool success, IDocument result)> TryCopyToAsync(IDocument entry, ICollection collection, string name, CancellationToken ct) 
            => RootCache.TryCopyToAsync(entry, collection, name, ct);

        public Task<(bool success, Stream result)> TryCreateAsync(IDocument entry, CancellationToken ct)
            => RootCache.TryCreateAsync(entry, ct);

        public Task<(bool success, DeleteResult result)> TryDeleteAsync(IDocument entry, CancellationToken ct)
            => RootCache.TryDeleteAsync(entry, ct);

        public Task<(bool success, IDocument result)> TryMoveToAsync(IDocument entry, ICollection collection, string name, CancellationToken ct)
            => RootCache.TryMoveToAsync(entry, collection, name, ct);

        public Task<(bool success, Stream result)> TryOpenReadAsync(IDocument entry, CancellationToken ct)
            => RootCache.TryOpenReadAsync(entry, ct);

        public Task<(bool success, ICollection result)> TryCreateCollectionAsync(ICollection entry, string name, CancellationToken ct)
            => RootCache.TryCreateCollectionAsync(entry, name, ct);

        public Task<(bool success, IDocument result)> TryCreateDocumentAsync(ICollection entry, string name, CancellationToken ct)
            => RootCache.TryCreateDocumentAsync(entry, name, ct);

        public Task<(bool success, DeleteResult result)> TryDeleteAsync(ICollection entry, CancellationToken ct)
            => RootCache.TryDeleteAsync(entry, ct);

        public Task TryCopyToAsync_Bubbleup(IDocument entry, ICollection collection, string name, IDocument result, CancellationToken ct)
            => CacheManager.CopyToAsync(entry, 
                () => Task.FromResult(0.0f), 
                () => RootCache.TryCopyToAsync_Bubbleup(entry, collection, name, result, ct));

        public Task TryCreateAsync_Bubbleup(IDocument entry, Stream result, CancellationToken ct)
            => CacheManager.CreateAsync(entry, 
                () => Task.FromResult(0.0f), 
                () => RootCache.TryCreateAsync_Bubbleup(entry, result, ct));

        public Task TryDeleteAsync_Bubbleup(IDocument entry, DeleteResult result, CancellationToken ct)
            => CacheManager.DeleteAsync(entry, 
                () => Task.FromResult(0.0f), 
                () => RootCache.TryDeleteAsync_Bubbleup(entry, result, ct));

        public Task TryMoveToAsync_Bubbleup(IDocument entry, ICollection collection, string name, IDocument result, CancellationToken ct)
            => CacheManager.MoveToAsync(entry,  
                () => Task.FromResult(0.0f), 
                () => RootCache.TryMoveToAsync_Bubbleup(entry, collection, name, result, ct));

        public Task TryOpenReadAsync_Bubbleup(IDocument entry, Stream result, CancellationToken ct)
            => CacheManager.OpenReadAsync(entry, 
                () => this.FitnessToStoreAsync(entry.FullPath(), () => Task.FromResult(entry), () => Task.FromResult(result)), 
                () => RootCache.TryOpenReadAsync_Bubbleup(entry, result, ct));

        public Task TryCreateCollectionAsync_Bubbleup(ICollection entry, string name, ICollection result, CancellationToken ct)
            => CacheManager.CreateCollectionAsync(entry, 
                () => Task.FromResult(0.0f), 
                () => RootCache.TryCreateCollectionAsync_Bubbleup(entry, name, result, ct));

        public Task TryCreateDocumentAsync_Bubbleup(ICollection entry, string name, IDocument result, CancellationToken ct)
            => CacheManager.CreateDocumentAsync(entry, 
                () => Task.FromResult(0.0f), 
                () => RootCache.TryCreateDocumentAsync_Bubbleup(entry, name, result, ct));

        public Task TryDeleteAsync_Bubbleup(ICollection entry, DeleteResult result, CancellationToken ct)
            => CacheManager.DeleteAsync(entry, 
                () => Task.FromResult(0.0f), 
                () => RootCache.TryDeleteAsync_Bubbleup(entry, result, ct));

        public void Dispose() => RootCache.Dispose();

        public Task<(bool success, SelectionResult result)> TrySelectAsync(string path, CancellationToken ct) =>
            RootCache.TrySelectAsync(path, ct);

        public Task TrySelectAsync_Bubbleup(string path, SelectionResult result, CancellationToken ct) =>
            RootCache.TrySelectAsync_Bubbleup(path, result, ct);

        public Task<bool> TrySetLastWriteTimeUtcAsync(IEntry entry, DateTime lastWriteTime, CancellationToken ct) =>
            RootCache.TrySetLastWriteTimeUtcAsync(entry, lastWriteTime, ct);

        public Task TrySetLastWriteTimeUtcAsync_Bubbleup(IEntry entry, DateTime lastWriteTime, CancellationToken ct) =>
            RootCache.TrySetLastWriteTimeUtcAsync_Bubbleup(entry, lastWriteTime, ct);

        public Task<bool> TrySetCreationTimeUtcAsync(IEntry entry, DateTime creationTime, CancellationToken ct) =>
            RootCache.TrySetCreationTimeUtcAsync(entry, creationTime, ct);

        public Task TrySetCreationTimeUtcAsync_Bubbleup(IEntry entry, DateTime creationTime, CancellationToken ct) =>
            RootCache.TrySetCreationTimeUtcAsync_Bubbleup(entry, creationTime, ct);

        public Task<(bool success, DeleteResult result)> TryDeleteAsync(IEntry entry, CancellationToken ct) =>
            RootCache.TryDeleteAsync(entry, ct);

        public Task TryDeleteAsync_Bubbleup(IEntry entry, DeleteResult result, CancellationToken ct) =>
            RootCache.TryDeleteAsync_Bubbleup(entry, result, ct);
    }
}
