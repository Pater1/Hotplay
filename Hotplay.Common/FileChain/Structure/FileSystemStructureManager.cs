using FubarDev.WebDavServer.FileSystem;
using Hotplay.Common.FileChain;
using Hotplay.Common.FileChain.Structure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hotplay.Common.Structure {
    public class FileSystemStructureManager: IFileSystemStructureManager {
        public FileSystemStructureManager(IChainedFileSystem fileSystem) {
            FileSystem = fileSystem;
        }

        public ManagedCollection Root { get; private set; }
        public IChainedFileSystem FileSystem{ get; private set; }
        public IDictionary<string, ManagedCollection> CollectionFastAccess { get; internal set; } = new Dictionary<string, ManagedCollection>();
        public IDictionary<string, ManagedDocument> DocumentFastAccess { get; internal set; } = new Dictionary<string, ManagedDocument>();

        public async Task<SelectionResult> SelectAsync(string path, CancellationToken ct) {
            if(string.IsNullOrEmpty(path)) {
                return SelectionResult.Create(CollectionFastAccess[path]);
            } else if(CollectionFastAccess.ContainsKey(path)) {
                return SelectionResult.Create(CollectionFastAccess[path]);
            } else if(CollectionFastAccess.ContainsKey(path + '\\')) {
                return SelectionResult.Create(CollectionFastAccess[path + '\\']);
            } else if(CollectionFastAccess.ContainsKey(path + '/')) {
                return SelectionResult.Create(CollectionFastAccess[path + '/']);
            } else if(DocumentFastAccess.ContainsKey(path)) {
                ManagedDocument manDoc = DocumentFastAccess[path];
                return SelectionResult.Create(manDoc.Parent, manDoc);
            } else {
                string[] pathParts = path.Split('\\','/');
                int i = 0;
                ManagedCollection mc = Root;
                for(; i < pathParts.Length; i++){
                    ManagedCollection nmc = mc.Collections.Where(x => x.Name.Equals(pathParts[i])).SingleOrDefault();
                    if(nmc == null) break;
                    mc = nmc;
                }
                return SelectionResult.CreateMissingDocumentOrCollection(mc, new ArraySegment<string>(pathParts, i, pathParts.Length - i));
            }
        }

        public async Task Initilize(IEnumerable<IFileChain> chain) {
            IEnumerable<ManagedCollection> collections = 
            (await Task.WhenAll(
                chain.Select(async x => {
                    ICollection toClone = await x.Root.Task;
                    if(toClone == null) {
                        return null;
                    } else {
                        return await toClone?.CloneManagedAsync(this, FileSystem);
                    }
                }))
            ).Where(x => x != null).Reverse();

            ManagedCollection root = collections.First();

            foreach(ManagedCollection mc in collections.Skip(1)){
                //TODO: diff-add file structure trees
            }

            Queue<ManagedCollection> colQueue = new Queue<ManagedCollection>();
            colQueue.Enqueue(root);
            CollectionFastAccess.Add(root.Name, root);
            ManagedCollection deq = null;
            while(colQueue.Count > 0 && (deq = colQueue.Dequeue()) != null){
                foreach(ManagedCollection col in deq.Collections){
                    CollectionFastAccess.Add(Uri.UnescapeDataString(col.Path.OriginalString), col);
                    colQueue.Enqueue(col);
                }
                foreach(ManagedDocument doc in deq.Documents){
                    DocumentFastAccess.Add(Uri.UnescapeDataString(doc.Path.OriginalString), doc);
                }
            }

            Root = root;
        }

        #region Documents
        public async Task<IDocument> CopyToAsync(ManagedDocument entry, ICollection collection, string name, CancellationToken ct) {
            IDocument tmp = await FileSystem.CopyToAsync(entry, collection, name, ct);
            ManagedDocument ret = await tmp.CloneManagedAsync(this, FileSystem, CollectionFastAccess[collection.Name]);
            return ret;
        }
        public Task<Stream> CreateAsync(ManagedDocument entry, CancellationToken ct) {
            return FileSystem.CreateAsync(entry, ct);
        }
        public Task<DeleteResult> DeleteAsync(ManagedDocument entry, CancellationToken ct) {
            ((ManagedCollection)entry.Parent).Documents.Remove(entry);
            return FileSystem.DeleteAsync(entry, ct);
        }
        public async  Task<IDocument> MoveToAsync(ManagedDocument entry, ICollection collection, string name, CancellationToken ct) {
            ((ManagedCollection)entry.Parent).Documents.Remove(entry);
            IDocument ret = await FileSystem.MoveToAsync(entry, collection, name, ct);
            return await ret.CloneManagedAsync(this, FileSystem, (ManagedCollection)collection);
        }
        public Task<Stream> OpenReadAsync(ManagedDocument entry, CancellationToken ct) {
            return FileSystem.OpenReadAsync(entry, ct);
        }
        #endregion

        #region Collections
        public async Task<ICollection> CreateCollectionAsync(ManagedCollection entry, string name, CancellationToken ct) {
            ICollection added = await FileSystem.CreateCollectionAsync(entry, name, ct);
            return await added.CloneManagedAsync(this, FileSystem, entry);
        }
        public async Task<IDocument> CreateDocumentAsync(ManagedCollection entry, string name, CancellationToken ct) {
            IDocument added = await FileSystem.CreateDocumentAsync(entry, name, ct);
            return await added.CloneManagedAsync(this, FileSystem, entry);
        }
        public Task<DeleteResult> DeleteAsync(ManagedCollection entry, CancellationToken ct) {
            ((ManagedCollection)entry.Parent).Collections.Remove(entry);
            return FileSystem.DeleteAsync(entry, ct);
        }
        #endregion
    }
}
