using FubarDev.WebDavServer.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hotplay.Common.FileChain.RoutedEntries {
    public class RoutedDocument: IDocument {
        private IDocument baseDocument;
        private ChainedFileSystem chainedFileSystem;

        public RoutedDocument(ChainedFileSystem chainedFileSystem, IDocument baseDocument) {
            this.baseDocument = baseDocument;
            this.chainedFileSystem = chainedFileSystem;
        }

        public long Length => baseDocument.Length;

        public string Name => baseDocument.Name;

        public IFileSystem FileSystem => chainedFileSystem;

        public ICollection Parent => new RoutedCollection(chainedFileSystem, baseDocument.Parent);

        public Uri Path => baseDocument.Path;

        public DateTime LastWriteTimeUtc => baseDocument.LastWriteTimeUtc;

        public DateTime CreationTimeUtc => baseDocument.CreationTimeUtc;

        public Task<IDocument> CopyToAsync(ICollection collection, string name, CancellationToken ct) =>
            chainedFileSystem.CopyToAsync(this, collection, name, ct);

        public Task<Stream> CreateAsync(CancellationToken ct) =>
            chainedFileSystem.CreateAsync(this, ct);

        public Task<DeleteResult> DeleteAsync(CancellationToken ct) =>
            chainedFileSystem.DeleteAsync(this, ct);

        public Task<IDocument> MoveToAsync(ICollection collection, string name, CancellationToken ct) =>
            chainedFileSystem.MoveToAsync(this, collection, name, ct);

        public Task<Stream> OpenReadAsync(CancellationToken ct) =>
            chainedFileSystem.OpenReadAsync(this, ct);

        public Task SetCreationTimeUtcAsync(DateTime creationTime, CancellationToken ct) =>
            chainedFileSystem.SetCreationTimeUtcAsync(this, creationTime, ct);

        public Task SetLastWriteTimeUtcAsync(DateTime lastWriteTime, CancellationToken ct) =>
            chainedFileSystem.SetLastWriteTimeUtcAsync(this, lastWriteTime, ct);
    }
}
