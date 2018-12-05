using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FubarDev.WebDavServer;
using FubarDev.WebDavServer.FileSystem;
using Hotplay.Common.Extentions;
using Hotplay.Common.FileChain.ChainLinks.Network.Commands;
using Hotplay.Common.FileChain.Structure;
using Hotplay.Common.Helpers;
using Newtonsoft.Json;

namespace Hotplay.Common.FileChain.ChainLinks.Network {
    public class NetworkAccessFileChain: IFileChain {
        private NetworkAccessFileChainOptions Options { get; set; }
        //private ClientWebSocket transport;
        private Dictionary<long, ClientWebSocket> Transports { get; set; } = new Dictionary<long, ClientWebSocket>();

        public IChainedFileSystem FileSystem { get; set; }

        private async Task<ClientWebSocket> OpenTransport(long? key = 0) {
            long _key = key.HasValue ? key.Value : await Options.KeyGenerator();
            ClientWebSocket transport = Transports.ContainsKey(_key) ? Transports[_key] : null;
            if(transport != null && transport.State != WebSocketState.Open) {
                if(transport.State != WebSocketState.CloseSent && transport.State != WebSocketState.Aborted) {
                    try {
                        await transport.CloseOutputAsync(WebSocketCloseStatus.NormalClosure,
                                "Reopening socket",
                                CancellationToken.None);
                    } catch(Exception e) {

                    }
                }
                Transports.Remove(_key);
                transport.Dispose();
            }
            if(transport == null || transport.State != WebSocketState.Open) {
                transport = new ClientWebSocket();
                Uri connectTo = new Uri(Options.BaseConnectionUri, _key.ToString());
                await transport.ConnectAsync(connectTo, CancellationToken.None);
                if(Transports.ContainsKey(_key)) {
                    Transports[_key] = transport;
                } else {
                    Transports.Add(_key, transport);
                }
            }
            return transport;
        }

        public NetworkAccessFileChain(NetworkAccessFileChainOptions options) {
            Options = options;
        }

        JsonSerializer Serializer { get; set; } = new JsonSerializer() {
            TypeNameHandling = TypeNameHandling.All
        };
        public AsyncLazy<ICollection> Root => new AsyncLazy<ICollection>(async () => {
            long key = await Options.KeyGenerator();
            Func<Task<WebSocket>> ftw = async () => await OpenTransport(key);

            EntryGetNetworkCommand ernc = new EntryGetNetworkCommand("", ftw, Serializer);
            ernc = await ernc.SyncronizedExecute(ftw, Serializer, key);
            NetworkClientCollection col = (NetworkClientCollection)ernc.Result.Collection;
            col.FileSystem = FileSystem;
            col.Transport = ftw;
            col.Serializer = Serializer;
            return col;
        });

        public bool SupportsRangedRead => true;


        private const int cacheLimit = 50;
        private static readonly TimeSpan cacheTimeout = TimeSpan.FromHours(8);
        private List<(string key, SelectionResult result, DateTime accessTime)> cache =
            new List<(string key, SelectionResult result, DateTime accessTime)>(cacheLimit + 5);
        private bool lockedCacheUpdate = false;
        private object cacheUpdateLock = new object();

        private async Task<(string key, SelectionResult result, DateTime accessTime)> UpdateCache(string key, bool bypassLock = false) {
            if(!bypassLock) {
                lock(cacheUpdateLock) {
                    if(lockedCacheUpdate) {
                        return default;
                    } else {
                        lockedCacheUpdate = true;
                    }
                }
            }

            long opKey = await Options.KeyGenerator();
            Func<Task<WebSocket>> ftw = async () => await OpenTransport(opKey);

            EntryGetNetworkCommand ernc = new EntryGetNetworkCommand(key, ftw, Serializer);
            ernc = await ernc.SyncronizedExecute(ftw, Serializer, opKey);
            SelectionResult result = ernc.Result;

            var v = (key, result, DateTime.Now);
            lock(cache) {
                cache.Add(v);
                if(cache.Count > cacheLimit) {
                    cache = cache.Skip(cache.Count - cacheLimit).ToList();
                }
            }


            if(v.result.ResultType == SelectionResultType.FoundDocument && v.result.Document is NetworkClientDocument) {
                (v.result.Document as NetworkClientDocument).BackgroundSetup(cache);
            } else if(v.result.ResultType == SelectionResultType.FoundCollection && v.result.Collection is NetworkClientCollection) {
                (v.result.Collection as NetworkClientCollection).BackgroundSetup(cache);
            }

            if(!bypassLock) {
                lock(cacheUpdateLock) {
                    lockedCacheUpdate = false;
                }
            }

            return v;
        }
        async Task<(bool success, SelectionResult result)> IFileChain.TrySelectAsync(string path, CancellationToken ct) {
            (string key, SelectionResult result, DateTime accessTime) v;
            lock(cache) {
                v = cache.Where(x => x.key == path).FirstOrDefault();
            }

            if(v.key == null || (DateTime.Now - v.accessTime) > cacheTimeout) {
                v = await UpdateCache(path, true);
            } else {
                lock(cache) {
                    cache.Remove(v);
                }
                ThreadPool.QueueUserWorkItem(async (x) => await UpdateCache(path));
            }

            bool successfulFind =
                v.result.ResultType == SelectionResultType.FoundCollection ||
                v.result.ResultType == SelectionResultType.FoundDocument;

            return (successfulFind, v.result);
        }

        public Task TrySelectAsync_Bubbleup(string path, SelectionResult result, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public async Task<(bool success, IEntry result)> TrySelectAsync(string path, CancellationToken ct) {
            (bool success, SelectionResult result) = await ((IFileChain)this).TrySelectAsync(path, ct);

            switch(result.ResultType) {
                case SelectionResultType.FoundCollection:
                    return (true, result.Collection);
                case SelectionResultType.FoundDocument:
                    return (true, result.Document);
                default:
                    return (false, result.Collection);
            }
        }
        private async Task<T> TryGet<T>(string pathName, CancellationToken ct) where T : NetworkClientEntry {
            var v = await TrySelectAsync(pathName, ct);
            T ldd = null;
            if(v.success) {
                ldd = v.result as T;
            }
            return ldd;
        }

        public async Task<(bool success, IDocument result)> TryCopyToAsync(IDocument entry, ICollection collection, string name, CancellationToken ct) {
            NetworkClientDocument ldd = await TryGet<NetworkClientDocument>(entry.FullPath(), ct);
            return (ldd != null, ldd == null ? null : await ldd.CopyToAsync(collection, name, ct));
        }

        public Task TryCopyToAsync_Bubbleup(IDocument entry, ICollection collection, string name, IDocument result, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public async Task<(bool success, Stream result)> TryCreateAsync(IDocument entry, CancellationToken ct) {
            NetworkClientDocument ldd = await TryGet<NetworkClientDocument>(entry.FullPath(), ct);
            return (ldd != null, ldd == null ? null : await ldd.CreateAsync(ct));
        }

        public Task TryCreateAsync_Bubbleup(IDocument entry, Stream result, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public async Task<(bool success, ICollection result)> TryCreateCollectionAsync(ICollection entry, string name, CancellationToken ct) {
            NetworkClientCollection ldd = await TryGet<NetworkClientCollection>(entry.FullPath(), ct);
            return (ldd != null, ldd == null ? null : await ldd.CreateCollectionAsync(name, ct));
        }

        public Task TryCreateCollectionAsync_Bubbleup(ICollection entry, string name, ICollection result, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public async Task<(bool success, IDocument result)> TryCreateDocumentAsync(ICollection entry, string name, CancellationToken ct) {
            NetworkClientCollection ldd = await TryGet<NetworkClientCollection>(entry.FullPath(), ct);
            return (ldd != null, ldd == null ? null : await ldd.CreateDocumentAsync(name, ct));
        }

        public Task TryCreateDocumentAsync_Bubbleup(ICollection entry, string name, IDocument result, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public async Task<(bool success, DeleteResult result)> TryDeleteAsync(IDocument entry, CancellationToken ct) {
            NetworkClientDocument ldd = await TryGet<NetworkClientDocument>(entry.FullPath(), ct);
            return (ldd != null, ldd == null ? null : await ldd.DeleteAsync(ct));
        }

        public async Task<(bool success, DeleteResult result)> TryDeleteAsync(ICollection entry, CancellationToken ct) {
            NetworkClientDocument ldd = await TryGet<NetworkClientDocument>(entry.FullPath(), ct);
            return (ldd != null, ldd == null ? null : await ldd.DeleteAsync(ct));
        }

        public Task TryDeleteAsync_Bubbleup(IDocument entry, DeleteResult result, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public Task TryDeleteAsync_Bubbleup(ICollection entry, DeleteResult result, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public async Task<(bool success, IDocument result)> TryMoveToAsync(IDocument entry, ICollection collection, string name, CancellationToken ct) {
            NetworkClientDocument ldd = await TryGet<NetworkClientDocument>(entry.FullPath(), ct);
            return (ldd != null, ldd == null ? null : await ldd.MoveToAsync(collection, name, ct));
        }

        public Task TryMoveToAsync_Bubbleup(IDocument entry, ICollection collection, string name, IDocument result, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public async Task<(bool success, Stream result)> TryOpenReadAsync(IDocument entry, CancellationToken ct) {
            NetworkClientDocument ldd = await TryGet<NetworkClientDocument>(entry.FullPath(), ct);
            return (ldd != null, ldd == null ? null : await ldd.OpenReadAsync(ct));
        }

        public Task TryOpenReadAsync_Bubbleup(IDocument entry, Stream result, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public async Task<bool> TrySetLastWriteTimeUtcAsync(IEntry entry, DateTime lastWriteTime, CancellationToken ct) {
            NetworkClientDocument ldd = await TryGet<NetworkClientDocument>(entry.FullPath(), ct);
            await ldd?.SetLastWriteTimeUtcAsync(lastWriteTime, ct);
            return ldd != null;
        }

        public Task TrySetLastWriteTimeUtcAsync_Bubbleup(IEntry entry, DateTime lastWriteTime, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public async Task<bool> TrySetCreationTimeUtcAsync(IEntry entry, DateTime creationTime, CancellationToken ct) {
            NetworkClientDocument ldd = await TryGet<NetworkClientDocument>(entry.FullPath(), ct);
            await ldd?.SetCreationTimeUtcAsync(creationTime, ct);
            return ldd != null;
        }

        public Task TrySetCreationTimeUtcAsync_Bubbleup(IEntry entry, DateTime creationTime, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public async Task<(bool success, DeleteResult result)> TryDeleteAsync(IEntry entry, CancellationToken ct) {
            NetworkClientEntry ldd = await TryGet<NetworkClientEntry>(entry.FullPath(), ct);
            return (ldd != null, ldd == null ? null : await ldd.DeleteAsync(ct));
        }

        public Task TryDeleteAsync_Bubbleup(IEntry entry, DeleteResult result, CancellationToken ct) {
            return Task.CompletedTask;
        }
    }
    public class NetworkAccessFileChainOptions {
        public Uri BaseConnectionUri { get; set; }
        public long ConcurrentRequestLimit { get; set; }
        private Func<Task<long>> _KeyGenerator = null;
        public Func<Task<long>> KeyGenerator {
            get {
                if(_KeyGenerator == null) {
                    return () => Task.FromResult(ThreadKey());
                } else {
                    return _KeyGenerator;
                }
            }
            set {
                _KeyGenerator = value;
            }
        }

        private long localThreadKey = 0;
        private long ThreadKey() => (localThreadKey = (++localThreadKey % ConcurrentRequestLimit)) + 1;
    }
}
