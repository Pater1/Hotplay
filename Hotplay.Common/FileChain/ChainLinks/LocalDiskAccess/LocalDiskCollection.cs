using FubarDev.WebDavServer.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hotplay.Common.FileChain.ChainLinks.LocalDiskAccess
{
    internal class LocalDiskCollection: LocalDiskEntry, ICollection {
        public static LocalDiskCollection RequestTracked(DirectoryInfo di, LocalDiskAccessFileChain chain, IFileSystem fileSystem, LocalDiskCollection localDiskCollection, IDictionary<string, LocalDiskCollection> colDict, IDictionary<string, LocalDiskDocument> docDict) {
            LocalDiskCollection ret;
            string name = di.FullName.ScrubPath();
            if(colDict != null && colDict.ContainsKey(name)){
                ret = colDict[name];
            }else{
                ret = new LocalDiskCollection(di, chain, fileSystem, localDiskCollection, colDict, docDict);
                if(colDict != null){
                    colDict.Add(ret.AbsolutePath, ret);
                }
            }
            ret.FileSystemInfo.Refresh();
            return ret;
        }
        private DirectoryInfo Info {
            get {
                return (DirectoryInfo)FileSystemInfo;
            }
            set {
                FileSystemInfo = value;
            }
        }

        private LocalDiskCollection(string path, LocalDiskAccessFileChain chain, IFileSystem system, LocalDiskCollection parent = null, IDictionary<string, LocalDiskCollection> colDict = null, IDictionary<string, LocalDiskDocument> docDict = null)
        : this(new DirectoryInfo(path), chain, system, parent, colDict, docDict) { }

        public LocalDiskCollection(DirectoryInfo info, LocalDiskAccessFileChain chain, IFileSystem system, LocalDiskCollection parent, IDictionary<string, LocalDiskCollection> colDict, IDictionary<string, LocalDiskDocument> docDict)
            : base(info, chain, system, parent, colDict, docDict){}

        public Task<ICollection> CreateCollectionAsync(string name, CancellationToken ct) {
            name = Uri.UnescapeDataString(name);
            Info.CreateSubdirectory(name);
            string path = System.IO.Path.Combine(AbsolutePath, name);
            LocalDiskCollection ldc = LocalDiskCollection.RequestTracked(new DirectoryInfo(path), Chain, FileSystem, _Parent, ColDict, DocDict);
            return Task.FromResult<ICollection>(ldc);
        }

        public Task<IDocument> CreateDocumentAsync(string name, CancellationToken ct) {
            string path = System.IO.Path.Combine(AbsolutePath, name);

            LocalDiskDocument ldd = DocDict.Select(x => x.Value).Where(x => x.AbsolutePath == path).FirstOrDefault();

            if(ldd == null) {
                ldd = LocalDiskDocument.RequestTracked(new FileInfo(path), Chain, FileSystem, _Parent, ColDict, DocDict);
            }

            return Task.FromResult<IDocument>(ldd);
        }

        public override Task<DeleteResult> DeleteAsync(CancellationToken cancellationToken) {
            Dispose();
            Info.Delete(true);
            return Task.FromResult( new DeleteResult(FubarDev.WebDavServer.Model.WebDavStatusCode.OK, null) );
        }

        internal IEnumerable<LocalDiskEntry> Children {
            get {
                foreach(DirectoryInfo di in Info.EnumerateDirectories()) {
                    yield return LocalDiskCollection.RequestTracked(di, Chain, FileSystem, this, ColDict, DocDict);
                }
                foreach(FileInfo fi in Info.EnumerateFiles()) {
                    yield return LocalDiskDocument.RequestTracked(fi, Chain, FileSystem, this, ColDict, DocDict);
                }
            }
        }

        public Task<IEntry> GetChildAsync(string name, CancellationToken ct) {
            return Task.FromResult<IEntry>(Children.Where(x => x.Name == name).FirstOrDefault());
        }

        public Task<IReadOnlyCollection<IEntry>> GetChildrenAsync(CancellationToken ct) {
            return Task.FromResult<IReadOnlyCollection<IEntry>>(Children.ToArray());
        }

        internal override void RemoveFromDict() {
            ColDict?.Remove(AbsolutePath);
        }

        public override void Dispose() {
            RemoveFromDict();
            foreach(LocalDiskEntry lde in Children) {
                lde.Dispose();
            }
        }
    }
}
