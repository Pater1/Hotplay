using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FubarDev.WebDavServer;
using FubarDev.WebDavServer.FileSystem;
using Hotplay.Common.FileChain;
using Hotplay.Common.FileChain.Structure;

namespace Hotplay.Common {
    public class FileSystemChainWrapper: IFileChain {
        public IChainedFileSystem FileSystem { get; set; }
        public FileSystemChainWrapper(IFileSystem wrapped) {
            Wrapped = wrapped;
        }

        private IFileSystem Wrapped{ get; set; }
        public bool SupportsRangedRead => Wrapped.SupportsRangedRead;

        public AsyncLazy<ICollection> Root => Wrapped.Root;

        public Task TrySelectAsync_Bubbleup(string path, SelectionResult result, CancellationToken ct) { return Task.CompletedTask; }

        public async Task<(bool success, SelectionResult result)> TrySelectAsync(string path, CancellationToken ct) {
            SelectionResult res = await Wrapped.SelectAsync(path, ct);

            return (res.ResultType == SelectionResultType.FoundCollection || 
                        res.ResultType == SelectionResultType.FoundDocument,
                    res);
        }

        #region Documents
        public Task<(bool success, IDocument result)> TryCopyToAsync(IDocument entry, ICollection collection, string name, CancellationToken ct) {
            throw new NotImplementedException();
        }
        public async Task<(bool success, Stream result)> TryCreateAsync(IDocument entry, CancellationToken ct) {
            var v = await TrySelectAsync(Uri.UnescapeDataString(entry.Path.OriginalString), ct);
            if(v.success && v.result.ResultType == SelectionResultType.FoundDocument) {
                return (true, await v.result.Document.CreateAsync(ct));
            } else {
                return (false, null);
            }
        }
        //public async Task<(bool success, DeleteResult result)> TryDeleteAsync(IDocument entry, CancellationToken ct) {
        //    var v = await TrySelectAsync(Uri.UnescapeDataString(entry.Path.OriginalString), ct);
        //    if(v.success && v.result.ResultType == SelectionResultType.FoundDocument) {
        //        return (true, await v.result.Document.DeleteAsync(ct));
        //    } else {
        //        return (false, null);
        //    }
        //}
        public async Task<(bool success, IDocument result)> TryMoveToAsync(IDocument entry, ICollection collection, string name, CancellationToken ct) {
            Task<(bool success, SelectionResult result)> tv = TrySelectAsync(Uri.UnescapeDataString(entry.Path.OriginalString), ct);
            Task<(bool success, SelectionResult result)> tw = TrySelectAsync(Uri.UnescapeDataString(collection.Path.OriginalString), ct);
            (bool success, SelectionResult result)[] vs = await Task.WhenAll(tv, tw);

            if(vs[0].success && vs[0].result.ResultType == SelectionResultType.FoundDocument &&
                vs[1].success && vs[1].result.ResultType == SelectionResultType.FoundCollection) {
                return (true, await vs[0].result.Document.MoveToAsync(vs[1].result.Collection, name, ct));
            } else {
                return (false, null);
            }
        }
        public async Task<(bool success, Stream result)> TryOpenReadAsync(IDocument entry, CancellationToken ct) {
            var v = await TrySelectAsync(Uri.UnescapeDataString(entry.Path.OriginalString), ct);
            if(v.success && v.result.ResultType == SelectionResultType.FoundDocument){
                return (true, await v.result.Document.OpenReadAsync(ct));
            }else{
                return (false, null);
            }
        }
        #endregion

        #region Collections
        public async Task<(bool success, ICollection result)> TryCreateCollectionAsync(ICollection entry, string name, CancellationToken ct) {
            var v = await TrySelectAsync(Uri.UnescapeDataString(entry.Path.OriginalString), ct);
            if(v.success && v.result.ResultType == SelectionResultType.FoundCollection) {
                return (true, await v.result.Collection.CreateCollectionAsync(name, ct));
            } else {
                return (false, null);
            }
        }
        public async Task<(bool success, IDocument result)> TryCreateDocumentAsync(ICollection entry, string name, CancellationToken ct) {
            var v = await TrySelectAsync(Uri.UnescapeDataString(entry.Path.OriginalString), ct);
            if(v.success && v.result.ResultType == SelectionResultType.FoundCollection) {
                return (true, await v.result.Collection.CreateDocumentAsync(name, ct));
            } else {
                return (false, null);
            }
        }
        //public async Task<(bool success, DeleteResult result)> TryDeleteAsync(ICollection entry, CancellationToken ct) {
        //    var v = await TrySelectAsync(Uri.UnescapeDataString(entry.Path.OriginalString), ct);
        //    if(v.success && v.result.ResultType == SelectionResultType.FoundCollection) {
        //        return (true, await v.result.Collection.DeleteAsync(ct));
        //    } else {
        //        return (false, null);
        //    }
        //}
        #endregion

        #region Documents_Bubbleup
        public Task TryCopyToAsync_Bubbleup(IDocument entry, ICollection collection, string name, IDocument result, CancellationToken ct) {
            return Task.CompletedTask;
        }
        public Task TryCreateAsync_Bubbleup(IDocument entry, Stream result, CancellationToken ct) {
            return Task.CompletedTask;
        }
        public Task TryMoveToAsync_Bubbleup(IDocument entry, ICollection collection, string name, IDocument result, CancellationToken ct) {
            return Task.CompletedTask;
        }
        public Task TryOpenReadAsync_Bubbleup(IDocument entry, Stream result, CancellationToken ct) {
            return Task.CompletedTask;
        }
        #endregion

        #region Collections_Bubbleup
        public Task TryCreateCollectionAsync_Bubbleup(ICollection entry, string name, ICollection result, CancellationToken ct) {
            return Task.CompletedTask;
        }
        public Task TryCreateDocumentAsync_Bubbleup(ICollection entry, string name, IDocument result, CancellationToken ct) {
            return Task.CompletedTask;
        }
        #endregion

        public async Task<bool> TrySetLastWriteTimeUtcAsync(IEntry entry, DateTime lastWriteTime, CancellationToken ct) {
            var v = await TrySelectAsync(Uri.UnescapeDataString(entry.Path.OriginalString), ct);
            if(v.success && (v.result.ResultType == SelectionResultType.FoundDocument || v.result.ResultType == SelectionResultType.FoundCollection)) {
                if(v.result.ResultType == SelectionResultType.FoundDocument) {
                    await v.result.Document.SetLastWriteTimeUtcAsync(lastWriteTime, ct);
                } else {
                    await v.result.Collection.SetLastWriteTimeUtcAsync(lastWriteTime, ct);
                }
                return true;
            } else {
                return false;
            }
        }

        public Task TrySetLastWriteTimeUtcAsync_Bubbleup(IEntry entry, DateTime lastWriteTime, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public async Task<bool> TrySetCreationTimeUtcAsync(IEntry entry, DateTime creationTime, CancellationToken ct) {
            var v = await TrySelectAsync(Uri.UnescapeDataString(entry.Path.OriginalString), ct);
            if(v.success && (v.result.ResultType == SelectionResultType.FoundDocument || v.result.ResultType == SelectionResultType.FoundCollection)) {
                if(v.result.ResultType == SelectionResultType.FoundDocument) {
                    await v.result.Document.SetCreationTimeUtcAsync(creationTime, ct);
                } else {
                    await v.result.Collection.SetCreationTimeUtcAsync(creationTime, ct);
                }
                return true;
            } else {
                return false;
            }
        }

        public Task TrySetCreationTimeUtcAsync_Bubbleup(IEntry entry, DateTime creationTime, CancellationToken ct) {
            return Task.CompletedTask;
        }

        public async Task<(bool success, DeleteResult result)> TryDeleteAsync(IEntry entry, CancellationToken ct) {
            var v = await TrySelectAsync(Uri.UnescapeDataString(entry.Path.OriginalString), ct);
            if(v.success && (v.result.ResultType == SelectionResultType.FoundDocument || v.result.ResultType == SelectionResultType.FoundCollection)) {
                if(v.result.ResultType == SelectionResultType.FoundDocument) {
                    return (true, await v.result.Document.DeleteAsync(ct));
                }else{
                    return (true, await v.result.Collection.DeleteAsync(ct));
                }
            } else {
                return (false, null);
            }
        }

        public Task TryDeleteAsync_Bubbleup(IEntry entry, DeleteResult result, CancellationToken ct) {
            return Task.CompletedTask;
        }
    }
}
