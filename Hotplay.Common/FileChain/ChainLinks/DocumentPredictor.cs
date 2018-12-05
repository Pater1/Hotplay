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
using Hotplay.Common.FileChain.Structure;
using Hotplay.Common.Helpers;
using Hotplay.Common.Predictors;

namespace Hotplay.Common.FileChain.ChainLinks {
    public class DocumentPredictor: IFileChain {
        public IChainedFileSystem FileSystem { get; set; }
        private IDocumentStore Store { get; set; }
        private IDocumentPredictor Predictor { get; set; }
        private ICacheAllocator Allocator { get; set; }
        public DocumentPredictor(IDocumentStore store, IDocumentPredictor predictor, ICacheAllocator allocator, ChainedFileSystem predictOnFileSystem) {
            Store = store;
            Predictor = predictor;
            Allocator = allocator;

            Allocator.OnDeallocate.Add(async (x) => {
                await Store.Remove(x);

            });
            ThreadPool.QueueUserWorkItem(async (x) => {
                try {
                    RequestLockManager.LockAndWait("DocumentPredictor_ctor");
                    while(FileSystem == null) {
                        await Task.Delay(10);
                    }
                    await PredicitiveFillCache(0, CancellationToken.None);
                }finally{
                    RequestLockManager.Unlock("DocumentPredictor_ctor");
                }
            });
        }

        public AsyncLazy<ICollection> Root { get; } = new AsyncLazy<ICollection>(() => Task.FromResult<ICollection>(null));

        //TODO: make accurate
        public bool SupportsRangedRead => true;

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
            await Store.Remove(entry.FullPath());
            return (false, null);
        }

        public Task TryDeleteAsync_Bubbleup(IEntry entry, DeleteResult result, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public async Task<(bool success, IDocument result)> TryCopyToAsync(IDocument entry, ICollection collection, string name, CancellationToken ct) {
            string from = entry.FullPath();
            string to = collection.FullPath() + "/" + name;

            await Store.Copy(from, to);
            return (false, null);
        }

        public Task TryCopyToAsync_Bubbleup(IDocument entry, ICollection collection, string name, IDocument result, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public async Task<(bool success, Stream result)> TryCreateAsync(IDocument entry, CancellationToken ct) {
            await Store.Remove(entry.FullPath());
            return (false, null);
        }

        public Task TryCreateAsync_Bubbleup(IDocument entry, Stream result, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public Task<(bool success, ICollection result)> TryCreateCollectionAsync(ICollection entry, string name, CancellationToken ct) {
            return Task.FromResult((false, (ICollection)null));
        }

        public Task TryCreateCollectionAsync_Bubbleup(ICollection entry, string name, ICollection result, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public Task<(bool success, IDocument result)> TryCreateDocumentAsync(ICollection entry, string name, CancellationToken ct) {
            return Task.FromResult((false, (IDocument)null));
        }

        public Task TryCreateDocumentAsync_Bubbleup(ICollection entry, string name, IDocument result, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public async Task<(bool success, DeleteResult result)> TryDeleteAsync(IDocument entry, CancellationToken ct) {
            string name = entry.FullPath();
            await Store.Remove(name);
            return (false, null);
        }

        public Task<(bool success, DeleteResult result)> TryDeleteAsync(ICollection entry, CancellationToken ct) {
            return Task.FromResult((false, (DeleteResult)null));
        }

        public Task TryDeleteAsync_Bubbleup(IDocument entry, DeleteResult result, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public Task TryDeleteAsync_Bubbleup(ICollection entry, DeleteResult result, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public async Task<(bool success, IDocument result)> TryMoveToAsync(IDocument entry, ICollection collection, string name, CancellationToken ct) {
            string from = entry.FullPath();
            string to = collection.FullPath() + "/" + name;

            await Store.Move(from, to);
            return (false, null);
        }

        public Task TryMoveToAsync_Bubbleup(IDocument entry, ICollection collection, string name, IDocument result, CancellationToken ct) {
            return Task.CompletedTask;
        }

        private Random rand = new Random();
        public async Task<(bool success, Stream result)> TryOpenReadAsync(IDocument entry, CancellationToken ct) {
            string name = entry.FullPath();
            var v = await Store.TryGet(name);
            if(!RequestLockManager.GetStatus(name).Is(RequestStatus.IgnoreCache)) {
                await Predictor.Request(name);
            }

            return v;
        }
        public async Task TryOpenReadAsync_Bubbleup(IDocument entry, Stream result, CancellationToken ct) {
            string name = entry.FullPath();
            if(!RequestLockManager.GetStatus(name).Is(RequestStatus.IgnoreCache)) {
                int key = rand.Next();
                activePredicitveFillKey = key;
                await Predictor.Request_Bubbleup(name);
                ThreadPool.QueueUserWorkItem(async (x) => await PredicitiveFillCache(key, ct));
            }
        }
        private int activePredicitveFillKey = 0;
        private async Task PredicitiveFillCache(int predicitveFillKey, CancellationToken ct) {
            IEnumerable<string> predictedFiles = await Predictor.GetLikelyhoodSortedDocuments();
            predictedFiles = predictedFiles.ToArray();
            IEnumerator<string> predictedFilesEnumerator = predictedFiles.GetEnumerator();

            string currentDocKey = null;
            IDocument currentDoc = null;
            string currentStreamKey = null;
            Stream currentStream = null;
            Func<Task<IDocument>> CurrentDoc = async () => {
                if(predictedFilesEnumerator.Current != currentDocKey) {
                    SelectionResult selectionResult = await FileSystem.SelectAsync(predictedFilesEnumerator.Current, ct, false);
                    currentDoc = selectionResult.Document;
                    currentDocKey = predictedFilesEnumerator.Current;
                }
                return currentDoc;
            };
            Func<Task<Stream>> CurrentStream = async () => {
                if(predictedFilesEnumerator.Current != currentStreamKey) {
                    IDocument doc = await CurrentDoc();
                    currentStream = await doc.OpenReadAsync(ct);
                    currentStreamKey = predictedFilesEnumerator.Current;
                }
                return currentStream;
            };

            //List<string> Cached = new List<string>();

            while(predicitveFillKey == activePredicitveFillKey && predictedFilesEnumerator.MoveNext()) {
                if(RequestLockManager.GetStatus_Chainable(predictedFilesEnumerator.Current).Is(RequestStatus.IgnoreCache)){
                    continue;
                }
                RequestLockManager.AddStatus(predictedFilesEnumerator.Current, RequestStatus.PredicitiveRequest);
                try {
                    if(!(await Store.TryGet(predictedFilesEnumerator.Current, true)).success) {
                        if(!await Allocator.TryAllocate(predictedFilesEnumerator.Current, CurrentDoc, CurrentStream)) {
                            break;
                        }
                        if(!await Store.TryPut(predictedFilesEnumerator.Current, await CurrentStream())) {
                            break;
                        }
                    }else{
                        await Allocator.TryAllocate(predictedFilesEnumerator.Current, CurrentDoc, CurrentStream);
                    }
                    RequestLockManager.AddStatus(predictedFilesEnumerator.Current, RequestStatus.Cached);
                } finally{
                    RequestLockManager.RemoveStatus(predictedFilesEnumerator.Current, RequestStatus.PredicitiveRequest);
                }
            }

            //string log = $"Predicted and Cached:\n    {(Cached.Any() ? (Cached.Count == 1 ? Cached.First() : Cached.Aggregate((x, y) => $"{x}\n    {y}")) : "None")}";
            //Console.WriteLine(log);
        }
    }
}
