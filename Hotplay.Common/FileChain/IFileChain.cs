using FubarDev.WebDavServer;
using FubarDev.WebDavServer.FileSystem;
using Hotplay.Common.FileChain.Structure;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hotplay.Common.FileChain {
    public interface IFileChain {
        AsyncLazy<ICollection> Root{ get; }

        bool SupportsRangedRead { get; }

        IChainedFileSystem FileSystem{ get; set; }

        Task<(bool success, SelectionResult result)> TrySelectAsync(string path, CancellationToken ct);
        Task TrySelectAsync_Bubbleup(string path, SelectionResult result, CancellationToken ct);

        Task<bool> TrySetLastWriteTimeUtcAsync(IEntry entry, DateTime lastWriteTime, CancellationToken ct);
        Task TrySetLastWriteTimeUtcAsync_Bubbleup(IEntry entry, DateTime lastWriteTime, CancellationToken ct);
        Task<bool> TrySetCreationTimeUtcAsync(IEntry entry, DateTime creationTime, CancellationToken ct);
        Task TrySetCreationTimeUtcAsync_Bubbleup(IEntry entry, DateTime creationTime, CancellationToken ct);
        Task<(bool success, DeleteResult result)> TryDeleteAsync(IEntry entry, CancellationToken ct);
        Task TryDeleteAsync_Bubbleup(IEntry entry, DeleteResult result, CancellationToken ct);

        #region Documents
        Task<(bool success, IDocument result)> TryCopyToAsync(IDocument entry, ICollection collection, string name, CancellationToken ct);
        Task<(bool success, Stream result)> TryCreateAsync(IDocument entry, CancellationToken ct);
        Task<(bool success, IDocument result)> TryMoveToAsync(IDocument entry, ICollection collection, string name, CancellationToken ct);
        Task<(bool success, Stream result)> TryOpenReadAsync(IDocument entry, CancellationToken ct);
        #endregion

        #region Collections
        Task<(bool success, ICollection result)> TryCreateCollectionAsync(ICollection entry, string name, CancellationToken ct);
        Task<(bool success, IDocument result)> TryCreateDocumentAsync(ICollection entry, string name, CancellationToken ct);
        #endregion

        #region Documents_Bubbleup
        Task TryCopyToAsync_Bubbleup(IDocument entry, ICollection collection, string name, IDocument result, CancellationToken ct);
        Task TryCreateAsync_Bubbleup(IDocument entry, Stream result, CancellationToken ct);
        Task TryMoveToAsync_Bubbleup(IDocument entry, ICollection collection, string name, IDocument result, CancellationToken ct);
        Task TryOpenReadAsync_Bubbleup(IDocument entry, Stream result, CancellationToken ct);
        #endregion

        #region Collections_Bubbleup
        Task TryCreateCollectionAsync_Bubbleup(ICollection entry, string name, ICollection result, CancellationToken ct);
        Task TryCreateDocumentAsync_Bubbleup(ICollection entry, string name, IDocument result, CancellationToken ct);
        #endregion
    }
}
