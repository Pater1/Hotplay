using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FubarDev.WebDavServer;
using FubarDev.WebDavServer.FileSystem;
using Hotplay.Common.Caches.Allocator;
using Hotplay.Common.DocumentStore;
using Hotplay.Common.Extentions;
using Hotplay.Common.FileChain;
using Hotplay.Common.FileChain.Structure;
using Hotplay.Common.Helpers;
using Hotplay.Common.Streams;

namespace Hotplay.Common.FileChain.ChainLinks.Cache {
    public class DocumentCache: IDocumentCache {
        public IChainedFileSystem FileSystem { get; set; }
        internal IDocumentStore Store { get; set; }
        private ICacheAllocator Allocator { get; set; }
        private bool IgnoreIgnoreCache{ get; set; }
        public DocumentCache(IDocumentStore store, ICacheAllocator cacheAllocator, bool ignoreIgnoreCache = false) {
            Store = store;
            Allocator = cacheAllocator;
            Root = new AsyncLazy<ICollection>(() => Task.FromResult<ICollection>(null));
            IgnoreIgnoreCache = ignoreIgnoreCache;
            cacheAllocator.OnDeallocate.Add((x) => Store.Remove(x));
        }

        public Task<float> FitnessToStoreAsync(string key, Func<Task<IDocument>> doc, Func<Task<Stream>> stream) => Allocator.FitnessToAllocateAsync(key, doc, stream);

        //TODO: make accurate
        public bool SupportsRangedRead => true;

        public AsyncLazy<ICollection> Root { get; }

        #region Documents
        public async Task<(bool success, IDocument result)> TryCopyToAsync(IDocument entry, ICollection collection, string name, CancellationToken ct) {
            string from = entry.FullPath();
            string to = collection.FullPath() + "/" + name;

            RequestLockManager.AddStatus(from, RequestStatus.Cached);
            RequestLockManager.AddStatus(to, RequestStatus.Cached);

            await Store.Copy(from, to);
            await Allocator.Copied(from, to);
            return (false, null);
        }
        public async Task<(bool success, Stream result)> TryCreateAsync(IDocument entry, CancellationToken ct) {
            await Store.Remove(entry.FullPath());
            return (false, null);
        }
        public async Task<(bool success, DeleteResult result)> TryDeleteAsync(IDocument entry, CancellationToken ct) {
            string name = entry.FullPath();
            await Store.Remove(name);
            await Allocator.Removed(name);
            RequestLockManager.RemoveStatus(name, RequestStatus.Cached);
            return (false, null); ;
        }
        public async Task<(bool success, IDocument result)> TryMoveToAsync(IDocument entry, ICollection collection, string name, CancellationToken ct) {
            string from = entry.FullPath();
            string to = collection.FullPath() + "/" + name;

            RequestLockManager.RemoveStatus(from, RequestStatus.Cached);
            RequestLockManager.AddStatus(to, RequestStatus.Cached);

            await Store.Move(from, to);
            await Allocator.Moved(from, to);
            return (false, null);
        }
        public async Task<(bool success, Stream result)> TryOpenReadAsync(IDocument entry, CancellationToken ct) {
            string name = entry.FullPath();
            var v = await Store.TryGet(name);
            if(v.success) {
                await Allocator.Refresh(name, () => Task.FromResult(entry), () => Task.FromResult(v.data));
                await CacheManager.Ignore_OpenReadAsync(entry);
                RequestLockManager.AddStatus(name, RequestStatus.Cached);
            }
            return v;
        }
        #endregion

        #region Collections
        public Task<(bool success, ICollection result)> TryCreateCollectionAsync(ICollection entry, string name, CancellationToken ct) {
            return Task.FromResult<(bool, ICollection)>((false, null));
        }
        public Task<(bool success, IDocument result)> TryCreateDocumentAsync(ICollection entry, string name, CancellationToken ct) {
            return Task.FromResult<(bool, IDocument)>((false, null));
        }
        public Task<(bool success, DeleteResult result)> TryDeleteAsync(ICollection entry, CancellationToken ct) {
            return Task.FromResult<(bool, DeleteResult)>((false, null));
        }
        #endregion

        #region Documents_Bubbleup
        public Task TryCopyToAsync_Bubbleup(IDocument entry, ICollection collection, string name, IDocument result, CancellationToken ct) {
            return Task.CompletedTask;
        }
        public Task TryCreateAsync_Bubbleup(IDocument entry, Stream result, CancellationToken ct) {
            return Task.CompletedTask;
        }
        public Task TryDeleteAsync_Bubbleup(IDocument entry, DeleteResult result, CancellationToken ct) {
            return Task.CompletedTask;
        }
        public Task TryMoveToAsync_Bubbleup(IDocument entry, ICollection collection, string name, IDocument result, CancellationToken ct) {
            return Task.CompletedTask;
        }
        public async Task TryOpenReadAsync_Bubbleup(IDocument entry, Stream result, CancellationToken ct) {
            string name = entry.FullPath();

            if(!IgnoreIgnoreCache && RequestLockManager.GetStatus(name).Is(RequestStatus.IgnoreCache)) return;

            if(result.CanSeek) {
                if(result is IDelayedDisposable) {
                    (result as IDelayedDisposable).OnDisposeAsync.Add(async () => {
                        result.Position = 0;
                        await Store.TryPut(name, result);
                    });
                } else {
                    await Store.TryPut(name, result);
                    result.Position = 0;
                }

                RequestLockManager.AddStatus(name, RequestStatus.Cached);
            } else if(result is PushReadStream &&
                 !RequestLockManager.GetStatus(name).Is(RequestStatus.IgnoreCache) &&
                 await Allocator.TryAllocate(name, () => Task.FromResult(entry), () => Task.FromResult(result))
            ) {
                PushReadStream prs = result as PushReadStream;
                prs.AddListener(new DocumentCacheStreamListener(prs, this, name));
                RequestLockManager.AddStatus(name, RequestStatus.Cached);
            }


        }
        #endregion

        #region Collections_Bubbleup
        public Task TryCreateCollectionAsync_Bubbleup(ICollection entry, string name, ICollection result, CancellationToken ct) {
            return Task.CompletedTask;
        }
        public Task TryCreateDocumentAsync_Bubbleup(ICollection entry, string name, IDocument result, CancellationToken ct) {
            return Task.CompletedTask;
        }
        public Task TryDeleteAsync_Bubbleup(ICollection entry, DeleteResult result, CancellationToken ct) {
            return Task.CompletedTask;
        }
        #endregion

        public void Dispose() {
            Store.Dispose();
            Allocator.Dispose();
        }

        public Task<(bool success, SelectionResult result)> TrySelectAsync(string path, CancellationToken ct) {
            return Task.FromResult<(bool success, SelectionResult result)>((false, null));
        }

        public Task TrySelectAsync_Bubbleup(string path, SelectionResult result, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public Task<bool> TrySetLastWriteTimeUtcAsync(IEntry entry, DateTime lastWriteTime, CancellationToken ct) {
            return Task.FromResult(false);
        }

        public Task TrySetLastWriteTimeUtcAsync_Bubbleup(IEntry entry, DateTime lastWriteTime, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public Task<bool> TrySetCreationTimeUtcAsync(IEntry entry, DateTime creationTime, CancellationToken ct) {
            return Task.FromResult(false);
        }

        public Task TrySetCreationTimeUtcAsync_Bubbleup(IEntry entry, DateTime creationTime, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public async Task<(bool success, DeleteResult result)> TryDeleteAsync(IEntry entry, CancellationToken ct) {
            string name = entry.FullPath();
            await Store.Remove(name);
            await Allocator.Removed(name);
            RequestLockManager.RemoveStatus(name, RequestStatus.Cached);
            return (false, null);
        }

        public Task TryDeleteAsync_Bubbleup(IEntry entry, DeleteResult result, CancellationToken ct) {
            return Task.CompletedTask;
        }
    }
}
