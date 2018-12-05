using FubarDev.WebDavServer.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hotplay.Common.FileChain.ChainLinks.LocalDiskAccess {
    internal class LocalDiskDocument: LocalDiskEntry, IDocument {
        public static LocalDiskDocument RequestTracked(FileInfo di, LocalDiskAccessFileChain chain, IFileSystem fileSystem, LocalDiskCollection localDiskCollection, IDictionary<string, LocalDiskCollection> colDict, IDictionary<string, LocalDiskDocument> docDict) {
            LocalDiskDocument ret;
            string name = di.FullName.ScrubPath();
            if(docDict != null && docDict.ContainsKey(name)) {
                ret = docDict[name];
            } else {
                ret = new LocalDiskDocument(di, chain, fileSystem, localDiskCollection, colDict, docDict);
                if(docDict != null) {
                    docDict.Add(ret.AbsolutePath, ret);
                }
            }
            ret.FileSystemInfo.Refresh();
            return ret;
        }

        private FileInfo Info {
            get {
                return (FileInfo)FileSystemInfo;
            }
            set {
                FileSystemInfo = value;
            }
        }
        private LocalDiskDocument(string path, LocalDiskAccessFileChain chain, IFileSystem system, LocalDiskCollection parent = null, IDictionary<string, LocalDiskCollection> colDict = null, IDictionary<string, LocalDiskDocument> docDict = null) 
            : this(new FileInfo(path), chain, system, parent, colDict, docDict) { }

        private LocalDiskDocument(FileInfo info, LocalDiskAccessFileChain chain, IFileSystem system, LocalDiskCollection parent, IDictionary<string, LocalDiskCollection> colDict, IDictionary<string, LocalDiskDocument> docDict)
            :base (info, chain, system, parent, colDict, docDict){}

        public long Length {
            get {
                try {
                    return Info.Length;
                } catch(FileNotFoundException ) {
                    return 0;
                }
            }
        }

        public async Task<IDocument> CopyToAsync(ICollection collection, string name, CancellationToken cancellationToken) {
            IDocument destinationDoc = null;
            Stream[] from0to1 = await Task.WhenAll<Stream>(this.OpenReadAsync(cancellationToken),
                collection.CreateDocumentAsync(name, cancellationToken).ContinueWith(async (x) => {
                    destinationDoc = await x;
                    return await destinationDoc.CreateAsync(cancellationToken);
                }).Result);

            //byte[] buffer = new byte[256];
            //int read = 0;
            //while((read = await from0to1[0].ReadAsync(buffer, 0, buffer.Length)) > 0) {
            //    await from0to1[1].WriteAsync(buffer, 0, read);
            //}
            await from0to1[0].CopyToAsync(from0to1[1]);

            from0to1[0].Close();
            from0to1[1].Close();

            return destinationDoc;
        }

        public Task<Stream> CreateAsync(CancellationToken cancellationToken) {
            return Task.FromResult<Stream>(Info.Open(FileMode.Create, FileAccess.Write));
        }

        public override Task<DeleteResult> DeleteAsync(CancellationToken cancellationToken) {
            Dispose();
            Info.Delete();
            return Task.FromResult(new DeleteResult(FubarDev.WebDavServer.Model.WebDavStatusCode.OK, null));
        }

        public async Task<IDocument> MoveToAsync(ICollection collection, string name, CancellationToken cancellationToken) {
            name = Uri.UnescapeDataString(name);
            string folderPath = Uri.UnescapeDataString(collection.Path.OriginalString);
            string newDest = System.IO.Path.Combine(folderPath, name);

            IDocument destinationDoc = null;
            Stream[] from0to1 = await Task.WhenAll<Stream>(this.OpenReadAsync(cancellationToken),
                collection.CreateDocumentAsync(name, cancellationToken).ContinueWith(async (x) => {
                    destinationDoc = await x;
                    return await destinationDoc.CreateAsync(cancellationToken);
                }).Result);

            from0to1[0].CopyTo(from0to1[1]);

            from0to1[0].Close();
            from0to1[1].Close();

            await DeleteAsync(cancellationToken);

            return destinationDoc;
        }

        public Task<Stream> OpenReadAsync(CancellationToken cancellationToken) {
            return Task.FromResult<Stream>(Info.OpenRead());
        }

        internal override void RemoveFromDict() {
            DocDict?.Remove(RelativePath);
        }

        public override void Dispose() {
            RemoveFromDict();
        }
    }
}
