using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Hotplay.Common.Helpers {
    public static class MutexManager {
        private static Dictionary<string, SemaphoreSlim> TrackedMutexes { get; set; } = new Dictionary<string, SemaphoreSlim>();
    
        public static SemaphoreSlim RequestTrackedSemaphore(string trackingKey, int count = 1){
            lock(TrackedMutexes){
                if(TrackedMutexes.ContainsKey(trackingKey)){
                    return TrackedMutexes[trackingKey];
                }else{
                    SemaphoreSlim o = new SemaphoreSlim(count, count);
                    TrackedMutexes.Add(trackingKey, o);
                    return o;
                }
            }
        }
    }
}
