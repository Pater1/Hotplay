using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FubarDev.WebDavServer.FileSystem;

namespace Hotplay.Common.Caches.Invalidator {
    public class CacheFillTimeInvalidator: ICacheInvalidator {
        public Task<IEnumerable<string>> DeallocatePriorityFiles => throw new NotImplementedException();

        public Task Allocated(string name, Func<Task<IDocument>> doc, Func<Task<Stream>> result) {
            throw new NotImplementedException();
        }

        public Task Copied(string from, string to) {
            throw new NotImplementedException();
        }

        public void Dispose() {
            throw new NotImplementedException();
        }

        public Task Moved(string from, string to) {
            throw new NotImplementedException();
        }

        public Task Refresh(string name, Func<Task<IDocument>> doc, Func<Task<Stream>> result) {
            throw new NotImplementedException();
        }

        public Task Removed(string key) {
            throw new NotImplementedException();
        }
    }
}
