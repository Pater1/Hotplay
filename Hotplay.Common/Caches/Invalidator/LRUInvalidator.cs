using FubarDev.WebDavServer.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Hotplay.Common.Caches.Invalidator {
    public class LRUInvalidator: ICacheInvalidator {
        private List<string> AllocatedFiles { get; set; } = new List<string>();
        public Task<IEnumerable<string>> DeallocatePriorityFiles => Task.FromResult(AllocatedFiles.Reverse<string>());

        public Task Allocated(string name, Func<Task<IDocument>> doc, Func<Task<Stream>> result) {
            if(AllocatedFiles.Contains(name)) {
                AllocatedFiles.Remove(name);
            }
            AllocatedFiles.Insert(0, name);
            return Task.CompletedTask;
        }

        public Task Copied(string from, string to) {
            AllocatedFiles.Insert(0, to);
            return Task.CompletedTask;
        }

        public Task Moved(string from, string to) {
            int index = AllocatedFiles.IndexOf(from);
            if(index >= 0) {
                AllocatedFiles[index] = to;
            }
            return Task.CompletedTask;
        }

        public Task Refresh(string name, Func<Task<IDocument>> doc, Func<Task<Stream>> result) {
            if(AllocatedFiles.Contains(name)) {
                AllocatedFiles.Remove(name);
                AllocatedFiles.Insert(0, name);
            }
            return Task.CompletedTask;
        }

        public Task Removed(string key) {
            AllocatedFiles.Remove(key);
            return Task.CompletedTask;
        }

        public void Dispose() {}
    }
}
