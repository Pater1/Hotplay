using FubarDev.WebDavServer.FileSystem;
using Hotplay.Common.Extentions;
using Hotplay.Common.FileChain.ChainLinks.Network.Commands;
using Hotplay.Common.Helpers;
using Hotplay.Common.Threads;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hotplay.Common.FileChain.ChainLinks.Network {
    public sealed class NetworkClientDocument: NetworkClientEntry, IDocument {
        [JsonProperty]
        public long Length { get; internal set; }

        public NetworkClientDocument() { }
        public NetworkClientDocument(IDocument toCopy) {
            base.Copy(toCopy);
            Length = toCopy.Length;
        }

        public Task<IDocument> CopyToAsync(ICollection collection, string name, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        public Task<Stream> CreateAsync(CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        public Task<IDocument> MoveToAsync(ICollection collection, string name, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        [JsonIgnore]
        private static TimeSpan cacheInvalidate = TimeSpan.FromHours(8);
        [JsonIgnore]
        private MemoryStream cachedData = null;
        [JsonIgnore]
        private DateTime cacheTime;
        [JsonIgnore]
        private object cacheLock = new object();
        public async Task<Stream> OpenReadAsync(CancellationToken cancellationToken) {
            //if(cachedData == null || (DateTime.Now - cacheTime) > cacheInvalidate) {
                FileOpenReadNetworkCommand cmd = new FileOpenReadNetworkCommand();
                cmd.Document = this;
                cmd.Serializer = Serializer;
                cmd.EntryFullPath = Uri.UnescapeDataString(Path.OriginalString);
                cmd.Transport = Transport;
                cmd = await cmd.SyncronizedExecute(Transport, Serializer);
                //lock(cacheLock) {
                    //if(cachedData != null) {
                    //    cachedData.Dispose();
                    //}
                    //cachedData = new MemoryStream();
                    //cmd.Result.CopyTo(cachedData);
                //}
            //}

            //cachedData.Position = 0;
            //MemoryStream mem = new MemoryStream();
            //cachedData.CopyTo(mem);
            return cmd.Result;
        }

        public override void BackgroundSetup(ICollection<(string key, SelectionResult result, DateTime accessTime)> cache) {
            ThreadPool.QueueUserWorkItem(async (x) => {
                //SelectionResult selres = await FileSystem.SelectAsync(this.FullPath(), CancellationToken.None);
                //await selres.Document.OpenReadAsync(CancellationToken.None);
            });
        }
    }
}
