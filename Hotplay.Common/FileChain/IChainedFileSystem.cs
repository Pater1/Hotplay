using FubarDev.WebDavServer.FileSystem;
using Hotplay.Common.FileChain.Structure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hotplay.Common.FileChain {
    public interface IChainedFileSystem: IFileSystem {
        Task<DeleteResult> DeleteAsync(IEntry entry, CancellationToken ct);
        Task SetLastWriteTimeUtcAsync(IEntry entry, DateTime lastWriteTime, CancellationToken ct);
        Task SetCreationTimeUtcAsync(IEntry entry, DateTime creationTime, CancellationToken ct);
        Task<SelectionResult> SelectAsync(string path, CancellationToken ct, bool wrapSelection = true);

        #region Documents
        Task<IDocument> CopyToAsync(IDocument entry, ICollection collection, string name, CancellationToken ct);
        Task<Stream> CreateAsync(IDocument entry, CancellationToken ct);
        Task<IDocument> MoveToAsync(IDocument entry, ICollection collection, string name, CancellationToken ct);
        Task<Stream> OpenReadAsync(IDocument entry, CancellationToken ct);
        #endregion

        #region Collections
        Task<ICollection> CreateCollectionAsync(ICollection entry, string name, CancellationToken ct);
        Task<IDocument> CreateDocumentAsync(ICollection entry, string name, CancellationToken ct);
        #endregion
    }
}
