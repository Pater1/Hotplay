using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Hotplay.Common.DocumentStore {
    public interface IDocumentStore: IDisposable {
        Task Purge();
        Task Remove(string key);
        Task<bool> TryPut(string key, Stream data);
        Task<(bool success, Stream data)> TryGet(string key, bool dryRun = false);
        Task Move(string from, string to);
        Task Copy(string from, string to);
    }
    public static class IDocumentStoreExtentions {
        public static async Task<bool> TryPut(this IDocumentStore store, string key, byte[] data) {
            using(MemoryStream mem = new MemoryStream(data)) {
                return await store.TryPut(key, mem);
            }
        }
    }
}