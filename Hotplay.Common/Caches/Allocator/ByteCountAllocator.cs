using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FubarDev.WebDavServer.FileSystem;
using Hotplay.Common.Caches.Invalidator;

namespace Hotplay.Common.Caches.Allocator {
    public class ByteCountAllocator: ICacheAllocator {
        public ICacheInvalidator CacheInvalidator {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public List<Func<string, Task>> OnDeallocate => throw new NotImplementedException();

        public Task Copied(string from, string to) {
            throw new NotImplementedException();
        }

        public void Dispose() {
            throw new NotImplementedException();
        }

        public Task<float> FitnessToAllocateAsync(string key, Func<Task<IDocument>> doc, Func<Task<Stream>> stream) {
            throw new NotImplementedException();
        }

        public Task Moved(string from, string to) {
            throw new NotImplementedException();
        }

        public Task Refresh(string key, Func<Task<IDocument>> doc, Func<Task<Stream>> stream) {
            throw new NotImplementedException();
        }

        public Task Removed(string key) {
            throw new NotImplementedException();
        }

        public Task<bool> TryAllocate(string key, Func<Task<IDocument>> doc, Func<Task<Stream>> stream) {
            throw new NotImplementedException();
        }
    }
}
