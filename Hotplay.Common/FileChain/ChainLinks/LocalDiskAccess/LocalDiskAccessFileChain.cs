using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FubarDev.WebDavServer;
using FubarDev.WebDavServer.FileSystem;
using Hotplay.Common.FileChain.Structure;
using Hotplay.Common.Helpers;

namespace Hotplay.Common.FileChain.ChainLinks.LocalDiskAccess {
    public class LocalDiskAccessFileChain: IFileChain {
        public IChainedFileSystem FileSystem { get; set; }
        internal LocalDiskAccessFileChain_Options Options { get; set; }
        public LocalDiskAccessFileChain(LocalDiskAccessFileChain_Options options, IFileSystem system) {
            Options = options;
            ColDict = new Dictionary<string, LocalDiskCollection>();
            DocDict = new Dictionary<string, LocalDiskDocument>();
            FastRoot = LocalDiskCollection.RequestTracked(new DirectoryInfo(Options.RootPath), this, system, null, ColDict, DocDict);
        }

        private string FullPath(string relativePath) => Path.Combine(Options.RootPath, relativePath).ScrubPath();

        public AsyncLazy<ICollection> Root => new AsyncLazy<ICollection>(async () => {
            while(FastRoot == null) await Task.Yield();
            return FastRoot;
        });
        private LocalDiskCollection FastRoot { get; set; }
        internal IDictionary<string, LocalDiskCollection> ColDict { get; private set; }
        internal IDictionary<string, LocalDiskDocument> DocDict { get; private set; }

        public bool SupportsRangedRead => true;

        public async Task<(bool success, SelectionResult result)> TrySelectAsync(string path, CancellationToken ct) {
            path = path.Replace(System.IO.Path.DirectorySeparatorChar, '/').Replace("//", "/");
            string fullPath = FullPath(path);
            LocalDiskEntry ldd = null;

            if(DocDict.ContainsKey(fullPath)) {
                ldd = DocDict[fullPath];
            } else if(ColDict.ContainsKey(fullPath)) {
                ldd = ColDict[fullPath];
            }

            SelectionResult sel = null;
            if(ldd == null) {
                if(path.Any()) {
                    string[] comps = path.Split('/');
                    ICollection found = FastRoot;
                    IEntry tmp = null;
                    int index = 0;
                    do {
                        tmp = await found.GetChildAsync(comps[index], ct);
                        if(tmp != null) {
                            index++;
                            if(tmp is ICollection) {
                                found = tmp as ICollection;
                            }
                        }
                    } while(tmp != null && tmp is ICollection && index < comps.Length);
                    string[] missingComps = comps.Skip(index).Select(x => x + '/').ToArray();

                    if(!missingComps.Any()) {
                        ldd = tmp as LocalDiskEntry;
                    } else {
                        if(path.Last() != '/') {
                            sel = SelectionResult.CreateMissingDocumentOrCollection(found, missingComps);
                        } else {
                            sel = SelectionResult.CreateMissingCollection(found, missingComps);
                        }
                    }
                } else {
                    ICollection col = await Root.Task;
                    sel = SelectionResult.Create(col);
                }
            }

            if(ldd != null && ldd is ICollection) {
                sel = SelectionResult.Create(ldd as ICollection);
            } else if(ldd != null) {
                sel = SelectionResult.Create((ldd as LocalDiskDocument).Parent, ldd as LocalDiskDocument);
            }

            return (ldd != null, sel);
        }
        public Task TrySelectAsync_Bubbleup(string path, SelectionResult result, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public Task TrySelectAsync_Bubbleup(SelectionResult selection) {
            return Task.CompletedTask;
        }

        public async Task<(bool success, IDocument result)> TryCopyToAsync(IDocument entry, ICollection collection, string name, CancellationToken ct) {
            LocalDiskDocument ldd = null;
            try {
                ldd = DocDict[FullPath(entry.FullPath())];
            } catch { }
            return (ldd != null, ldd == null? null: await ldd.CopyToAsync(collection, name, ct));
        }

        public Task TryCopyToAsync_Bubbleup(IDocument entry, ICollection collection, string name, IDocument result, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public async Task<(bool success, Stream result)> TryCreateAsync(IDocument entry, CancellationToken ct) {
            LocalDiskDocument ldd = null;
            try {
                ldd = DocDict[FullPath(entry.FullPath())];
            } catch { }
            return (ldd != null, ldd == null? null: await ldd.CreateAsync(ct));
        }

        public Task TryCreateAsync_Bubbleup(IDocument entry, Stream result, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public async Task<(bool success, ICollection result)> TryCreateCollectionAsync(ICollection entry, string name, CancellationToken ct) {
            LocalDiskCollection ldd = null;
            try {
                ldd = ColDict[FullPath(entry.FullPath())];
            } catch { }
            return (ldd != null, ldd == null? null: await ldd.CreateCollectionAsync(name, ct));
        }

        public Task TryCreateCollectionAsync_Bubbleup(ICollection entry, string name, ICollection result, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public async Task<(bool success, IDocument result)> TryCreateDocumentAsync(ICollection entry, string name, CancellationToken ct) {
            LocalDiskCollection ldd = null;
            try {
                ldd = ColDict[FullPath(entry.FullPath())];
            } catch { }
            return (ldd != null, ldd == null? null: await ldd.CreateDocumentAsync(name, ct));
        }

        public Task TryCreateDocumentAsync_Bubbleup(ICollection entry, string name, IDocument result, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public async Task<(bool success, DeleteResult result)> TryDeleteAsync(IDocument entry, CancellationToken ct) {
            LocalDiskEntry ldd = null;
            try {
                ldd = DocDict[FullPath(entry.FullPath())];
            } catch { }
            return (ldd != null, ldd == null? null: await ldd.DeleteAsync(ct));
        }

        public async Task<(bool success, DeleteResult result)> TryDeleteAsync(ICollection entry, CancellationToken ct) {
            LocalDiskEntry ldd = null;
            try {
                ldd = ColDict[FullPath(entry.FullPath())];
            } catch { }
            return (ldd != null, ldd == null? null: await ldd.DeleteAsync(ct));
        }

        public Task TryDeleteAsync_Bubbleup(IDocument entry, DeleteResult result, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public Task TryDeleteAsync_Bubbleup(ICollection entry, DeleteResult result, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public async Task<(bool success, IDocument result)> TryMoveToAsync(IDocument entry, ICollection collection, string name, CancellationToken ct) {
            LocalDiskDocument ldd = null;
            try {
                ldd = DocDict[FullPath(entry.FullPath())];
            } catch { }
            return (ldd != null, ldd == null? null: await ldd.MoveToAsync(collection, name, ct));
        }

        public Task TryMoveToAsync_Bubbleup(IDocument entry, ICollection collection, string name, IDocument result, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public async Task<(bool success, Stream result)> TryOpenReadAsync(IDocument entry, CancellationToken ct) {
            LocalDiskDocument ldd = null;
            string fullPath = FullPath(entry.FullPath());
            try {
                ldd = DocDict[fullPath];
            } catch {
                //Manual search?
            }
            return (ldd != null, ldd == null? null: await ldd.OpenReadAsync(ct));
        }

        public Task TryOpenReadAsync_Bubbleup(IDocument entry, Stream result, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public Task<bool> TrySetLastWriteTimeUtcAsync(IEntry entry, DateTime lastWriteTime, CancellationToken ct) {
            throw new NotImplementedException();
        }

        public Task TrySetLastWriteTimeUtcAsync_Bubbleup(IEntry entry, DateTime lastWriteTime, CancellationToken ct) {
            throw new NotImplementedException();
        }

        public Task<bool> TrySetCreationTimeUtcAsync(IEntry entry, DateTime creationTime, CancellationToken ct) {
            throw new NotImplementedException();
        }

        public Task TrySetCreationTimeUtcAsync_Bubbleup(IEntry entry, DateTime creationTime, CancellationToken ct) {
            throw new NotImplementedException();
        }

        public Task<(bool success, DeleteResult result)> TryDeleteAsync(IEntry entry, CancellationToken ct) {
            throw new NotImplementedException();
        }

        public Task TryDeleteAsync_Bubbleup(IEntry entry, DeleteResult result, CancellationToken ct) {
            throw new NotImplementedException();
        }

        public class LocalDiskAccessFileChain_Options {
            public string RootPath { get; set; }
        }
    }
}
