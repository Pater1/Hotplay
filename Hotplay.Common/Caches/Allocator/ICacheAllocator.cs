using FubarDev.WebDavServer.FileSystem;
using Hotplay.Common.Caches.Invalidator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Hotplay.Common.Caches.Allocator {
    public interface ICacheAllocator: IDisposable {
        Task<float> FitnessToAllocateAsync(string key, Func<Task<IDocument>> doc, Func<Task<Stream>> stream);

        ICacheInvalidator CacheInvalidator{ get; set; }

        Task<bool> TryAllocate(string key, Func<Task<IDocument>> doc, Func<Task<Stream>> stream);

        List<Func<string, Task>> OnDeallocate { get; }

        Task Refresh(string name, Func<Task<IDocument>> doc, Func<Task<Stream>> result);
        Task Moved(string from, string to);
        Task Removed(string key);
        Task Copied(string from, string to);
    }
    public static class ICacheAllocatorExtentions {
        public static Task<float> FitnessToAllocateAsync(this ICacheAllocator inv, string name, Func<Task<IDocument>> doc) =>
            inv.FitnessToAllocateAsync(name, doc, async () => {
                IDocument d = await doc();
                return await d.OpenReadAsync(CancellationToken.None);
            });
        public static Task<bool> TryAllocate(this ICacheAllocator inv, string name, Func<Task<IDocument>> doc, Func<Task<Stream>> result) =>
            inv.TryAllocate(name, doc, async () => {
                IDocument d = await doc();
                return await d.OpenReadAsync(CancellationToken.None);
            });
        public static Task Refresh(this ICacheAllocator inv, string name, Func<Task<IDocument>> doc, Func<Task<Stream>> result) =>
            inv.Refresh(name, doc, async () => {
                IDocument d = await doc();
                return await d.OpenReadAsync(CancellationToken.None);
            });
    }
}
