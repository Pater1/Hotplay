using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Hotplay.Common.Caches.Invalidator;
using FubarDev.WebDavServer.FileSystem;

namespace Hotplay.Common.Caches.Allocator {
    public class FileCountAllocator: ICacheAllocator {
        public int MaxAllocatedFiles { get; set; }
        private List<string> AllocatedFiles { get; set; } = new List<string>();
        private ICacheInvalidator cacheInvalidator;
        private Func<string, Task> cacheInvalidator_OnDeallocate;
        public ICacheInvalidator CacheInvalidator {
            get {
                return cacheInvalidator;    
            }
            set {
                OnDeallocate.Remove(cacheInvalidator_OnDeallocate);
                cacheInvalidator = value;
                cacheInvalidator_OnDeallocate = x => cacheInvalidator.Removed(x);
                OnDeallocate.Add(cacheInvalidator_OnDeallocate);
            }
        }
        public FileCountAllocator(int fileCount, ICacheInvalidator cacheInvalidator = null) {
            MaxAllocatedFiles = fileCount;
            CacheInvalidator = cacheInvalidator;
        }

        public void Dispose() {
            CacheInvalidator.Dispose();
        }

        public List<Func<string, Task>> OnDeallocate { get; set; } = new List<Func<string, Task>>();


        public async Task<bool> TryAllocate(string key, Func<Task<IDocument>> doc, Func<Task<Stream>> stream) {
            if(AllocatedFiles.Count >= MaxAllocatedFiles && CacheInvalidator != null) {
                int filesToDeallocate = 1 + (AllocatedFiles.Count - MaxAllocatedFiles);
                IEnumerable<Task> deallocateAwait = CacheInvalidator.DeallocatePriorityFiles.Result
                                                    .Take(filesToDeallocate)
                                                    .SelectMany(x => OnDeallocate.Select(y => y(x)));
                await Task.WhenAll(deallocateAwait);
            }


            if(!AllocatedFiles.Contains(key)){
                AllocatedFiles.Add(key);
                await CacheInvalidator.Allocated(key, doc, stream);
                return true;
            }else{
                return false;
            }
        }

        public async Task Refresh(string key, Func<Task<IDocument>> doc, Func<Task<Stream>> stream) {
            if(!AllocatedFiles.Contains(key)) {
                AllocatedFiles.Add(key);
                await CacheInvalidator.Allocated(key, doc, stream);
            } else {
                await CacheInvalidator.Refresh(key, doc, stream);
            }
        }

        public async Task Moved(string from, string to) {
            await CacheInvalidator.Moved(from, to);
        }

        public async Task Removed(string key) {
            await CacheInvalidator.Removed(key);
        }

        public async Task Copied(string from, string to) {
            await CacheInvalidator.Copied(from, to);
        }

        public Task<float> FitnessToAllocateAsync(string key, Func<Task<IDocument>> doc, Func<Task<Stream>> stream) {
            return Task.FromResult( 1 - (AllocatedFiles.Count / (float)MaxAllocatedFiles) );
        }
    }
}
