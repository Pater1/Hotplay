using FubarDev.WebDavServer.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hotplay.Common.FileChain.Structure {
    public interface IFileSystemStructureManager {
        Task Initilize(IEnumerable<IFileChain> chain);
        IDictionary<string, ManagedCollection> CollectionFastAccess { get; } 
        IDictionary<string, ManagedDocument> DocumentFastAccess { get; }

        Task<DeleteResult> DeleteAsync(ManagedDocument entry, CancellationToken ct);
        Task<IDocument> CopyToAsync(ManagedDocument entry, ICollection collection, string name, CancellationToken ct);
        Task<Stream> OpenReadAsync(ManagedDocument entry, CancellationToken ct);
        Task<Stream> CreateAsync(ManagedDocument entry, CancellationToken ct);
        Task<IDocument> MoveToAsync(ManagedDocument entry, ICollection collection, string name, CancellationToken ct);

        Task<DeleteResult> DeleteAsync(ManagedCollection entry, CancellationToken ct);
        Task<ICollection> CreateCollectionAsync(ManagedCollection entry, string name, CancellationToken ct);
        Task<IDocument> CreateDocumentAsync(ManagedCollection entry, string name, CancellationToken ct);
    }
    public static class FileSystemStructureManagerExtentions{
        public static async Task<ManagedCollection> CloneManagedAsync(this ICollection collection, IFileSystemStructureManager manager, IChainedFileSystem fileSystem, ManagedCollection parent = null){
            if(collection is ManagedCollection){
                return (ManagedCollection)collection;
            }

            ManagedCollection ret = new ManagedCollection();
            ret.Manager = manager;
            ret.Name = collection.Name;
            ret._FileSystem = fileSystem;//
            ret._Parent = parent;//
            ret.Path = collection.Path;
            ret.LastWriteTimeUtc = collection.LastWriteTimeUtc;
            ret.CreationTimeUtc = collection.CreationTimeUtc;

            ret.Collections = new LinkedList<ManagedCollection>();
            ret.Documents = new LinkedList<ManagedDocument>();
            IReadOnlyCollection<IEntry> entries = await collection.GetChildrenAsync(new CancellationToken());
            List<Task> ts = new List<Task>(entries.Count);
            foreach(IEntry e in entries){
                if(e is ICollection){
                    ts.Add(((ICollection)e).CloneManagedAsync(manager, fileSystem, ret));
                } else {
                    ts.Add(((IDocument)e).CloneManagedAsync(manager, fileSystem, ret));
                }
            }
            Task.WaitAll(ts.ToArray());

            parent?.Collections.Add(ret);
            manager.CollectionFastAccess.Add(Uri.UnescapeDataString(ret.Path.OriginalString), ret);

            return ret;
        }
        public static Task<ManagedDocument> CloneManagedAsync(this IDocument document, IFileSystemStructureManager manager, IChainedFileSystem fileSystem, ManagedCollection parent = null) {
            if(document is ManagedDocument) {
                return Task.FromResult((ManagedDocument)document);
            }

            ManagedDocument ret = new ManagedDocument();
            ret.Manager = manager;
            ret.Name = document.Name;
            ret._FileSystem = fileSystem;//
            ret._Parent = parent;//
            ret.Path = document.Path;
            ret.LastWriteTimeUtc = document.LastWriteTimeUtc;
            ret.CreationTimeUtc = document.CreationTimeUtc;
            ret.Length = document.Length;

            parent.Documents.Add(ret);
            manager.DocumentFastAccess.Add(Uri.UnescapeDataString(ret.Path.OriginalString), ret);

            return Task.FromResult(ret);
        }
    }
}