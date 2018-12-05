using FubarDev.WebDavServer.FileSystem;
using Hotplay.Common.Extentions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hotplay.Common.FileChain.RoutedEntries
{
    public class RoutedCollection: ICollection {
        private ICollection collection;
        private ChainedFileSystem chainedFileSystem;

        public RoutedCollection() { }
        public RoutedCollection(ChainedFileSystem chainedFileSystem, ICollection collection): this() {
            this.collection = collection;
            this.chainedFileSystem = chainedFileSystem;

            //DebugChildren = GetChildrenAsync(CancellationToken.None).RunSync().ToList();
        }

        public string Name => collection.Name;

        public IFileSystem FileSystem => chainedFileSystem;

        public ICollection Parent => collection.Parent;

        public Uri Path => collection.Path;

        public DateTime LastWriteTimeUtc => collection.LastWriteTimeUtc;

        public DateTime CreationTimeUtc => collection.CreationTimeUtc;

        public Task<ICollection> CreateCollectionAsync(string name, CancellationToken ct) =>
            chainedFileSystem.CreateCollectionAsync(this, name, ct);

        public Task<IDocument> CreateDocumentAsync(string name, CancellationToken ct) =>
            chainedFileSystem.CreateDocumentAsync(this, name, ct);

        public Task<DeleteResult> DeleteAsync(CancellationToken ct) =>
            chainedFileSystem.DeleteAsync(this, ct);

        public async Task<IEntry> GetChildAsync(string name, CancellationToken ct) {
            IEntry ret = await collection.GetChildAsync(name, ct);
            if(ret is IDocument){
                ret = new RoutedDocument(chainedFileSystem, ret as IDocument);
            }else{
                ret = new RoutedCollection(chainedFileSystem, ret as ICollection);
            }
            return ret;
        }

        public List<IEntry> DebugChildren { get; set; } 

        public async Task<IReadOnlyCollection<IEntry>> GetChildrenAsync(CancellationToken ct) {
            IReadOnlyCollection<IEntry> wrapAndRet = await collection.GetChildrenAsync(ct);
            IEntry[] ret = new IEntry[wrapAndRet.Count];
            ChainedFileSystem fileSystem = chainedFileSystem;
            
            int i = 0;
            foreach(var r in wrapAndRet){ 
                int j = i++;
                if(r is IDocument) {
                    ret[j] = new RoutedDocument(fileSystem, r as IDocument);
                } else {
                    ret[j] = new RoutedCollection(fileSystem, r as ICollection);
                }
            }

            //Parallel.ForEach(wrapAndRet, r => {
            //    int j = i++;
            //    if(r is IDocument) {
            //        ret[j] = new RoutedDocument(fileSystem, r as IDocument);
            //    } else {
            //        ret[j] = new RoutedCollection(fileSystem, r as ICollection);
            //    }
            //});
            return ret;
        }

        public Task SetCreationTimeUtcAsync(DateTime creationTime, CancellationToken ct) =>
            chainedFileSystem.SetCreationTimeUtcAsync(this, creationTime, ct);

        public Task SetLastWriteTimeUtcAsync(DateTime lastWriteTime, CancellationToken ct) =>
            chainedFileSystem.SetLastWriteTimeUtcAsync(this, lastWriteTime, ct);
    }
}
