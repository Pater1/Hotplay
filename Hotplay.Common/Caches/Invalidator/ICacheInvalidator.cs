using FubarDev.WebDavServer.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hotplay.Common.Caches.Invalidator {
    public interface ICacheInvalidator: IDisposable {
        Task Allocated(string name, Func<Task<IDocument>> doc, Func<Task<Stream>> result);
        Task Refresh(string name, Func<Task<IDocument>> doc, Func<Task<Stream>> result);
        Task Moved(string from, string to);
        Task Removed(string key);
        Task Copied(string from, string to);

        /// <summary>
        /// Returns the enumerable of all files known to the invalidator in order the order in which they should be deleted (from most deserving of deletion to least deserving)
        /// </summary>
        Task<IEnumerable<string>> DeallocatePriorityFiles{ get; }
    }
    public static class ICacheInvalidatorExtentions{
        public static Task Allocated(this ICacheInvalidator inv, string name, Func<Task<IDocument>> doc) =>
            inv.Allocated(name, doc, async () => {
                IDocument d = await doc();
                return await d.OpenReadAsync(CancellationToken.None);
            });
        public static Task Refresh(this ICacheInvalidator inv, string name, Func<Task<IDocument>> doc, Func<Task<Stream>> result) =>
            inv.Refresh(name, doc, async () => {
                IDocument d = await doc();
                return await d.OpenReadAsync(CancellationToken.None);
            });
    }
}
