using FubarDev.WebDavServer;
using FubarDev.WebDavServer.FileSystem;
using FubarDev.WebDavServer.Locking;
using FubarDev.WebDavServer.Props.Store;
using Hotplay.Common.Extentions;
using Hotplay.Common.FileChain.ChainLinks.Cache;
using Hotplay.Common.FileChain.ChainLinks.Network.Commands.CommandFlowControls.Receive;
using Hotplay.Common.FileChain.RoutedEntries;
using Hotplay.Common.FileChain.Structure;
using Hotplay.Common.Helpers;
using Hotplay.Common.Streams;
using Hotplay.Common.Structure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hotplay.Common.FileChain {
    public class ChainedFileSystem: IChainedFileSystem {
        private IFileChain[] FileChain { get; set; }
        //private FileSystemStructureManager StructureManager { get; set; }
        public ChainedFileSystem() { }
        public ChainedFileSystem(IEnumerable<IFileChain> fileChain) : this() {
            SetupFileChain(fileChain);
        }

        internal void SetupFileChain(IEnumerable<IFileChain> fileChain) {
            FileChain = fileChain.ToArray();

            for(int i = 0; i < FileChain.Length; i++) {
                FileChain[i].FileSystem = this;
            }

            //StructureManager = new FileSystemStructureManager(this);
            //StructureManager.Initilize(FileChain).RunSync();

            Root = new AsyncLazy<ICollection>(async () => {
                SelectionResult selRes = await SelectAsync("", CancellationToken.None);
                return selRes.Collection;
            });
        }

        public AsyncLazy<ICollection> Root { get; private set; }

        public bool SupportsRangedRead => FileChain.Select(x => x.SupportsRangedRead).Where(x => !x).Any();

        public IPropertyStore PropertyStore { get; internal set; }

        public ILockManager LockManager { get; internal set; }



        //TODO DRYify commands
        //private async 

        Task<SelectionResult> IFileSystem.SelectAsync(string path, CancellationToken ct) => SelectAsync(path, ct, true);
        public async Task<SelectionResult> SelectAsync(string path, CancellationToken ct, bool wrapSelection = true) {
            try {
                RequestLockManager.LockAndWait(path);
                int i = 0;
                SelectionResult ret = null;
                for(; i < FileChain.Length; i++) {
                    var v = await FileChain[i].TrySelectAsync(path, ct);
                    ret = v.result;
                    if(v.success || i >= FileChain.Length - 1) {
                        break;
                    }
                }

                if(wrapSelection) {
                    switch(ret.ResultType) {
                        case SelectionResultType.FoundCollection:
                            ICollection collection = new RoutedCollection(this, ret.Collection);
                            ret = SelectionResult.Create(collection);
                            break;
                        case SelectionResultType.FoundDocument:
                            IDocument document = new RoutedDocument(this, ret.Document);
                            ret = SelectionResult.Create(document.Parent, document);
                            break;
                        case SelectionResultType.MissingCollection:
                            break;
                        case SelectionResultType.MissingDocumentOrCollection:
                            break;
                    }
                }

                for(i--; i >= 0; i--) {
                    await FileChain[i].TrySelectAsync_Bubbleup(path, ret, ct);
                }

                return ret;
            } finally {
                RequestLockManager.Unlock(path);
            }
        }

        public async Task SetLastWriteTimeUtcAsync(IEntry entry, DateTime lastWriteTime, CancellationToken ct) {
            int i = 0;
            for(; i < FileChain.Length; i++) {
                if(await FileChain[i].TrySetLastWriteTimeUtcAsync(entry, lastWriteTime, ct) || i >= FileChain.Length - 1) {
                    break;
                }
            }

            for(i--; i >= 0; i--) {
                await FileChain[i].TrySetLastWriteTimeUtcAsync_Bubbleup(entry, lastWriteTime, ct);
            }
        }

        public async Task SetCreationTimeUtcAsync(IEntry entry, DateTime creationTime, CancellationToken ct) {
            int i = 0;
            for(; i < FileChain.Length; i++) {
                if(await FileChain[i].TrySetCreationTimeUtcAsync(entry, creationTime, ct) || i >= FileChain.Length - 1) {
                    break;
                }
            }

            for(i--; i >= 0; i--) {
                await FileChain[i].TrySetCreationTimeUtcAsync_Bubbleup(entry, creationTime, ct);
            }
        }
        public async Task<DeleteResult> DeleteAsync(IEntry entry, CancellationToken ct) {
            int i = 0;
            DeleteResult ret = null;
            for(; i < FileChain.Length; i++) {
                var v = await FileChain[i].TryDeleteAsync(entry, ct);
                if(v.success || i >= FileChain.Length - 1) {
                    ret = v.result;
                    break;
                }
            }

            for(i--; i >= 0; i--) {
                await FileChain[i].TryDeleteAsync_Bubbleup(entry, ret, ct);
            }

            await CacheManager.Flush_DeleteAsync(entry);

            return ret;
        }

        #region Documents
        public async Task<IDocument> CopyToAsync(IDocument entry, ICollection collection, string name, CancellationToken ct) {
            int i = 0;
            IDocument ret = null;
            for(; i < FileChain.Length; i++) {
                var v = await FileChain[i].TryCopyToAsync(entry, collection, name, ct);
                if(v.success || i >= FileChain.Length - 1) {
                    ret = v.result;
                    break;
                }
            }

            for(i--; i >= 0; i--) {
                await FileChain[i].TryCopyToAsync_Bubbleup(entry, collection, name, ret, ct);
            }

            await CacheManager.Flush_CopyToAsync(entry);

            return ret;
        }
        public async Task<Stream> CreateAsync(IDocument entry, CancellationToken ct) {
            int i = 0;
            Stream ret = null;
            for(; i < FileChain.Length; i++) {
                var v = await FileChain[i].TryCreateAsync(entry, ct);
                if(v.success || i >= FileChain.Length - 1) {
                    ret = v.result;
                    break;
                }
            }

            ret = new PushReadStream(ret);

            for(i--; i >= 0; i--) {
                await FileChain[i].TryCreateAsync_Bubbleup(entry, ret, ct);
            }

            await CacheManager.Flush_CreateAsync(entry);

            return ret;
        }

        public async Task<IDocument> MoveToAsync(IDocument entry, ICollection collection, string name, CancellationToken ct) {
            int i = 0;
            IDocument ret = null;
            for(; i < FileChain.Length; i++) {
                var v = await FileChain[i].TryMoveToAsync(entry, collection, name, ct);
                if(v.success || i >= FileChain.Length - 1) {
                    ret = v.result;
                    break;
                }
            }

            for(i--; i >= 0; i--) {
                await FileChain[i].TryMoveToAsync_Bubbleup(entry, collection, name, ret, ct);
            }

            await CacheManager.Flush_MoveToAsync(entry);

            return ret;
        }
        public async Task<Stream> OpenReadAsync(IDocument entry, CancellationToken ct) {
            string path = entry.FullPath();
            bool manualUnlock = true;
            try {
                RequestLockManager.LockAndWait(path);

                int i = 0;
                Stream ret = null;
                for(; i < FileChain.Length; i++) {
                    var v = await FileChain[i].TryOpenReadAsync(entry, ct);
                    if(v.success || i >= FileChain.Length - 1) {
                        ret = v.result;
                        break;
                    }
                }

                if(ret is ReceiveBytesStream){
                    manualUnlock = false;
                    (ret as ReceiveBytesStream).OnDispose.Add(() => RequestLockManager.Unlock(path)); 
                }

                //ret = new PushReadStream(ret);

                for(--i; i > 0; --i) {
                    await FileChain[i].TryOpenReadAsync_Bubbleup(entry, ret, ct);
                }

                await CacheManager.Flush_OpenReadAsync(entry);

                //await Task.WhenAll(FileChain.Take(i - 1).Select(x => x.TryOpenReadAsync_Bubbleup(entry, ret, ct)));

                return ret;
            } finally {
                if(manualUnlock) {
                    RequestLockManager.Unlock(path);
                }
            }
        }
        #endregion

        #region Collections
        public async Task<ICollection> CreateCollectionAsync(ICollection entry, string name, CancellationToken ct) {
            int i = 0;
            ICollection ret = null;
            for(; i < FileChain.Length; i++) {
                var v = await FileChain[i].TryCreateCollectionAsync(entry, name, ct);
                if(v.success || i >= FileChain.Length - 1) {
                    ret = v.result;
                    break;
                }
            }

            for(i--; i >= 0; i--) {
                await FileChain[i].TryCreateCollectionAsync_Bubbleup(entry, name, ret, ct);
            }

            await CacheManager.Flush_CreateCollectionAsync(entry);

            return ret;
        }
        public async Task<IDocument> CreateDocumentAsync(ICollection entry, string name, CancellationToken ct) {
            int i = 0;
            IDocument ret = null;
            for(; i < FileChain.Length; i++) {
                var v = await FileChain[i].TryCreateDocumentAsync(entry, name, ct);
                if(v.success || i >= FileChain.Length - 1) {
                    ret = v.result;
                    break;
                }
            }

            for(i--; i >= 0; i--) {
                await FileChain[i].TryCreateDocumentAsync_Bubbleup(entry, name, ret, ct);
            }

            await CacheManager.Flush_CreateDocumentAsync(entry);

            return ret;
        }

        public Task<DeleteResult> DeleteAsync(IDocument entry, CancellationToken ct) {
            throw new NotImplementedException();
        }

        public Task<DeleteResult> DeleteAsync(ICollection entry, CancellationToken ct) {
            throw new NotImplementedException();
        }
        #endregion
    }
}
