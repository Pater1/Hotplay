using Hotplay.Common.DocumentStore;
using Hotplay.Common.Extentions;
using Hotplay.Common.Streams;
using System.Collections.Generic;
using System.Linq;

namespace Hotplay.Common.FileChain.ChainLinks.Cache
{
    internal class DocumentCacheStreamListener: IPushStreamListener {
        public PushReadStream Stream { get; private set; }
        public DocumentCache Cache { get; private set; }
        public string CacheName { get; private set; }

        private List<byte> tmpCache { get; set; }

        public DocumentCacheStreamListener() { }
        public DocumentCacheStreamListener(PushReadStream stream, DocumentCache cache, string cacheName) : this() {
            Stream = stream;
            Cache = cache;
            CacheName = cacheName;
            tmpCache = new List<byte>(16);
            valid = true;
        }

        public void Dispose() {
            byte[] toCache = tmpCache.ToArray();
            tmpCache.Clear();
            Cache.Store.TryPut(CacheName, toCache).RunSync();
        }

        private bool valid;
        public void PushRead(byte[] buffer, long pushStartIndex, long pushEndIndex) {
            if(!valid) {
                return;
            } else if(pushEndIndex >= int.MaxValue || pushEndIndex >= int.MaxValue) {
                valid = false;
                tmpCache.Clear(); //release RAM to GC early
            } else {
                #region in alloc
                {
                    int inAllocStart = pushStartIndex < tmpCache.Count ? (int)pushStartIndex : tmpCache.Count
                    , inAllocEnd = pushEndIndex < tmpCache.Count ? (int)pushEndIndex : tmpCache.Count;

                    if(inAllocStart != inAllocEnd) {
                        for(int i = inAllocStart; i <= inAllocEnd; i++) {
                            int index = i - inAllocStart;
                            tmpCache[i] = buffer[index];
                        }
                    }
                }
                #endregion
                #region new alloc
                {
                    int newAllocStart = pushStartIndex < tmpCache.Count ? tmpCache.Count : (int)pushStartIndex
                    , newAllocEnd = pushEndIndex < tmpCache.Count ? tmpCache.Count : (int)pushEndIndex;

                    if(newAllocStart != newAllocEnd) {
                        if(newAllocStart > tmpCache.Count) {
                            for(int i = 0; i < (newAllocStart - tmpCache.Count); i++) {
                                tmpCache.Add(0);
                            }
                        }
                        tmpCache.AddRange(buffer.Skip(newAllocStart - tmpCache.Count));
                    }

                }
                #endregion
            }
        }

        public void PushWrite(byte[] buffer, long pushStartIndex, long pushEndIndex) { }
    }
}
