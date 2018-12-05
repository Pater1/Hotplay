using FubarDev.WebDavServer.FileSystem;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hotplay.Common.FileChain.ChainLinks.Network {
    [System.Serializable]
    public abstract class NetworkClientEntry: IEntry {
        [JsonIgnore]
        public Func<Task<WebSocket>> Transport { get; set; }
        [JsonIgnore]
        public JsonSerializer Serializer { get; set; }
        [JsonIgnore]
        public IFileSystem FileSystem { get; internal set; }

        [JsonIgnore]
        protected internal ICollection CachedParent { get; set; }
        [JsonIgnore]
        public ICollection Parent {
            get {
                if(CachedParent == null) {
                    //Wesocket Entry Request -> CachedParent
                }
                return CachedParent;
            }
        }

        protected void Copy(IEntry toCopy) {
            this.Path = toCopy.Path;
            this.Name = toCopy.Name;
            this.LastWriteTimeUtc = toCopy.LastWriteTimeUtc;
            this.CreationTimeUtc = toCopy.CreationTimeUtc;
        }
        [JsonProperty]
        public string Name { get; internal set; }

        [JsonProperty]
        private string PathString { get; set; }
        [JsonIgnore]
        public Uri Path {
            get {
                return new Uri(PathString, UriKind.Relative);
            }
            set {
                PathString = value.OriginalString;
            }
        }

        [JsonProperty]
        public DateTime LastWriteTimeUtc { get; internal set; }

        [JsonProperty]
        public DateTime CreationTimeUtc { get; internal set; }

        public Task<DeleteResult> DeleteAsync(CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        public Task SetCreationTimeUtcAsync(DateTime creationTime, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        public Task SetLastWriteTimeUtcAsync(DateTime lastWriteTime, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        protected internal void ParentSetup(NetworkClientCollection parent) {
            CachedParent = parent;
            Transport = parent.Transport;
            Serializer = parent.Serializer;
            FileSystem = parent.FileSystem;
        }

        public abstract void BackgroundSetup(ICollection<(string key, SelectionResult result, DateTime accessTime)> cache);
    }
}
