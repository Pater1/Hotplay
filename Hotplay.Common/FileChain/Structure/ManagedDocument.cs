using FubarDev.WebDavServer.FileSystem;
using Hotplay.Common.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hotplay.Common.FileChain.Structure {
    public class ManagedDocument: IDocument {
        public ManagedDocument() { }
        public ManagedDocument( IFileSystemStructureManager manager, 
                                string name, 
                                IChainedFileSystem fileSystem, 
                                ManagedCollection parent, 
                                Uri path, 
                                DateTime lastWriteTimeUtc, 
                                DateTime creationTimeUtc, 
                                long length) : this()
        {
            Manager = manager;
            Name = name;
            _FileSystem = fileSystem;
            _Parent = parent;
            Path = path;
            LastWriteTimeUtc = lastWriteTimeUtc;
            CreationTimeUtc = creationTimeUtc;
            Length = length;
        }

        internal IFileSystemStructureManager Manager { get; set; }

        public string Name { get; internal set; }
        public string PathName => Uri.UnescapeDataString(Path.OriginalString);

        public IFileSystem FileSystem => _FileSystem;
        public IChainedFileSystem _FileSystem { get; internal set; }

        public ICollection Parent => _Parent;
        public ManagedCollection _Parent { get; internal set; }

        public Uri Path { get; internal set; }

        public DateTime LastWriteTimeUtc { get; internal set; }

        public DateTime CreationTimeUtc { get; internal set; }

        public long Length { get; internal set; }

        public async Task<DeleteResult> DeleteAsync(CancellationToken cancellationToken) {
            DeleteResult ret = await Manager.DeleteAsync(this, cancellationToken);
            if(ret.StatusCode == FubarDev.WebDavServer.Model.WebDavStatusCode.OK) {
                _Parent.Documents.Remove(this);
                Manager.DocumentFastAccess.Remove(this.PathName);
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

        public async Task<IDocument> CopyToAsync(ICollection collection, string name, CancellationToken ct) {
            ManagedDocument doc = await(await Manager.CopyToAsync(this, collection, name, ct)).CloneManagedAsync(Manager, _FileSystem, collection as ManagedCollection);
            Manager.DocumentFastAccess.Add(doc.PathName, doc);
            return doc;
            //return Manager.CopyToAsync(this, collection, name, cancellationToken);
        }

        public async Task<Stream> CreateAsync(CancellationToken cancellationToken) {
            //TODO: update metadata
            Stream s = await Manager.CreateAsync(this, cancellationToken);
            return s;
        }

        public async Task<IDocument> MoveToAsync(ICollection collection, string name, CancellationToken cancellationToken) {
            IDocument doc = await Manager.MoveToAsync(this, collection, name, cancellationToken);
            Manager.DocumentFastAccess.Remove(this.PathName);
            return await doc.CloneManagedAsync(Manager, _FileSystem, collection as ManagedCollection);
            //return Manager.MoveToAsync(this, collection, name, cancellationToken);
        }

        public async Task<Stream> OpenReadAsync(CancellationToken cancellationToken) {
            return await Manager.OpenReadAsync(this, cancellationToken);
        }
    }
}
