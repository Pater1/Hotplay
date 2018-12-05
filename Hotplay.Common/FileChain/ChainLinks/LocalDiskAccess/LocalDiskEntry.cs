using FubarDev.WebDavServer.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hotplay.Common.FileChain.ChainLinks.LocalDiskAccess {
    internal abstract class LocalDiskEntry: IEntry, IDisposable {
        protected FileSystemInfo FileSystemInfo{ get; set; }
        public LocalDiskAccessFileChain Chain { get; protected set; }
        public LocalDiskEntry(FileSystemInfo info, LocalDiskAccessFileChain chain, IFileSystem system, LocalDiskCollection parent = null, IDictionary<string, LocalDiskCollection> colDict = null, IDictionary<string, LocalDiskDocument> docDict = null) {
            FileSystemInfo = info;

            ColDict = colDict;
            DocDict = docDict;

            Chain = chain;
            FileSystem = system;
            
            _Parent = parent;

            //if(name.Contains(".txt")) {
            //    int i = 0;
            //}
        }

        protected IDictionary<string, LocalDiskCollection> ColDict { get; set; } = null;
        protected IDictionary<string, LocalDiskDocument> DocDict { get; set; } = null;

        public string Name => new string(AbsolutePath.Skip(_Parent != null ? _Parent.AbsolutePath.Length+1 : Chain.Options.RootPath.Length).ToArray());
        public string RelativePath => new string(AbsolutePath.Skip(Chain.Options.RootPath.Length).ToArray());
        public string AbsolutePath => FileSystemInfo.FullName.ScrubPath();

        public IFileSystem FileSystem { get; protected set; }

        public ICollection Parent => _Parent;
        protected LocalDiskCollection _Parent { get; set; }

        public Uri Path => new Uri(RelativePath, UriKind.Relative);

        public DateTime LastWriteTimeUtc => FileSystemInfo.LastWriteTimeUtc;//Directory.GetLastWriteTimeUtc(AbsolutePath);

        public DateTime CreationTimeUtc => FileSystemInfo.CreationTimeUtc;

        public abstract Task<DeleteResult> DeleteAsync(CancellationToken cancellationToken);
        internal abstract void RemoveFromDict();
        
        public Task SetCreationTimeUtcAsync(DateTime creationTime, CancellationToken cancellationToken) {
            creationTime = creationTime.ToUniversalTime();
            FileSystemInfo.CreationTimeUtc = creationTime;
            return Task.CompletedTask;
        }
        
        public Task SetLastWriteTimeUtcAsync(DateTime lastWriteTime, CancellationToken cancellationToken) {
            lastWriteTime = lastWriteTime.ToUniversalTime();
            FileSystemInfo.LastWriteTimeUtc = lastWriteTime;
            return Task.CompletedTask;
        }

        public abstract void Dispose();
    }
}
