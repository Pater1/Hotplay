using FubarDev.WebDavServer.FileSystem;
using Hotplay.Common.Extentions;
using Hotplay.Common.FileChain.ChainLinks.Network.Commands;
using Hotplay.Common.Helpers;
using Hotplay.Common.Threads;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hotplay.Common.FileChain.ChainLinks.Network {
    public sealed class NetworkClientCollection: NetworkClientEntry, ICollection {
        public NetworkClientCollection() { }
        public NetworkClientCollection(ICollection toCopy) {
            base.Copy(toCopy);
        }

        public async Task<ICollection> CreateCollectionAsync(string name, CancellationToken ct) {
            CreateEntryRequestNetworkCommand<NetworkClientCollection> ernc = new CreateEntryRequestNetworkCommand<NetworkClientCollection>(
                Path.OriginalString,
                name,
                Transport,
                Serializer
            );
            ernc = await ernc.SyncronizedExecute(Transport, Serializer);
            ernc.Result.ParentSetup(this);
            return ernc.Result;
        }

        public async Task<IDocument> CreateDocumentAsync(string name, CancellationToken ct) {
            CreateEntryRequestNetworkCommand<NetworkClientDocument> ernc = new CreateEntryRequestNetworkCommand<NetworkClientDocument>(
                Path.OriginalString,
                name,
                Transport,
                Serializer
            );
            ernc = await ernc.SyncronizedExecute(Transport, Serializer);
            ernc.Result.ParentSetup(this);
            return ernc.Result;
        }

        public async Task<IEntry> GetChildAsync(string name, CancellationToken ct) {
            IReadOnlyCollection<IEntry> children = await GetChildrenAsync(ct);
            NetworkClientEntry ret = children.Where(x => x.Name == name).FirstOrDefault() as NetworkClientEntry;
            return ret;
        }

        [JsonIgnore]
        private static TimeSpan cacheInvalidate = TimeSpan.FromHours(8);
        [JsonIgnore]
        private IReadOnlyCollection<IEntry> cachedChildren = null;
        [JsonIgnore]
        private DateTime cacheTime;
        public async Task<IReadOnlyCollection<IEntry>> GetChildrenAsync(CancellationToken ct) {
            if(cachedChildren == null || (DateTime.Now - cacheTime) > cacheInvalidate) {
                ChildEntriesRequestNetworkCommand ernc = new ChildEntriesRequestNetworkCommand(
                    $"{Path.OriginalString}",
                    Transport,
                    Serializer,
                    this
                );

                ernc = await ernc.SyncronizedExecute(Transport, Serializer);
                cachedChildren = ernc.Result;
                cacheTime = DateTime.Now;
            }
            return cachedChildren;
        }

        public override void BackgroundSetup(ICollection<(string key, SelectionResult result, DateTime accessTime)> cache) {
            ThreadPool.QueueUserWorkItem(async (x) => {
                cachedChildren = await GetChildrenAsync(CancellationToken.None);
                lock(cache){
                    foreach(IEntry e in cachedChildren){
                        SelectionResult s = e is ICollection ? SelectionResult.Create(e as ICollection) : SelectionResult.Create(this, e as IDocument);
                        cache.Add((e.FullPath(), s, DateTime.Now));
                    }
                }
            });
        }
    }
}
