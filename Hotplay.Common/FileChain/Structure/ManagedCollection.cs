using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FubarDev.WebDavServer.FileSystem;

namespace Hotplay.Common.FileChain.Structure {
    public class ManagedCollection: ICollection {
        public ManagedCollection() { }
        public ManagedCollection(   IFileSystemStructureManager manager, 
                                    string name, 
                                    IChainedFileSystem fileSystem, 
                                    ManagedCollection parent, 
                                    Uri path, 
                                    DateTime lastWriteTimeUtc,
                                    DateTime creationTimeUtc) : this() 
        {
            Manager = manager;
            Name = name;
            _FileSystem = fileSystem;
            _Parent = parent;
            Path = path;
            LastWriteTimeUtc = lastWriteTimeUtc;
            CreationTimeUtc = creationTimeUtc;
        }

        internal IFileSystemStructureManager Manager { get; set; }

        public string Name { get; internal set; }
        public string PathName => Uri.UnescapeDataString(Path.OriginalString);

        public IFileSystem FileSystem => _FileSystem;
        public IChainedFileSystem _FileSystem { get; internal set; }

        public ICollection Parent => _Parent;
        public unsafe ManagedCollection _Parent { get; internal set; }

        public Uri Path { get; internal set; }

        public DateTime LastWriteTimeUtc { get; internal set; }

        public DateTime CreationTimeUtc { get; internal set; }

        public ICollection<ManagedDocument> Documents { get; internal set; }
        public ICollection<ManagedCollection> Collections { get; internal set; }
        public IEnumerable<IEntry> Entries => ((IEnumerable<IEntry>)Collections).Concat(Documents);

        public async Task<DeleteResult> DeleteAsync(CancellationToken cancellationToken) {
            DeleteResult ret = await Manager.DeleteAsync(this, cancellationToken);
            if(ret.StatusCode == FubarDev.WebDavServer.Model.WebDavStatusCode.OK) {
                _Parent.Collections.Remove(this);
            }
            return ret;
        }

        public Task SetCreationTimeUtcAsync(DateTime creationTime, CancellationToken cancellationToken) {
            CreationTimeUtc = creationTime.ToUniversalTime();
            return Task.CompletedTask;
        }

        public Task SetLastWriteTimeUtcAsync(DateTime lastWriteTime, CancellationToken cancellationToken) {
            LastWriteTimeUtc = lastWriteTime.ToUniversalTime();
            return Task.CompletedTask;
        }

        public async Task<ICollection> CreateCollectionAsync(string name, CancellationToken ct) {
            return (ManagedCollection)await Manager.CreateCollectionAsync(this, name, ct);
        }

        public async Task<IDocument> CreateDocumentAsync(string name, CancellationToken ct) {
            return await Manager.CreateDocumentAsync(this, name, ct);
        }

        public Task<IEntry> GetChildAsync(string name, CancellationToken ct) {
            foreach(IEntry e in Entries){
                if(e.Name == name){
                    return Task.FromResult(e);
                }
            }
            return Task.FromResult((IEntry)null);
        }

        public Task<IReadOnlyCollection<IEntry>> GetChildrenAsync(CancellationToken ct) {
            return Task.FromResult((IReadOnlyCollection<IEntry>)(new ArraySegment<IEntry>(Entries.ToArray())));
        }
    }
}
