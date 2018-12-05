using Hotplay.Common.Extentions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hotplay.Common.Threads {
    public static class StackBasedThreadPool {
        public static int ThreadCount { get; set; } = 4;
        private static List<Thread> pool = null;
        private static Stack<(WaitCallback callback, object obj)> jobs = new Stack<(WaitCallback callback, object obj)>();
        private static object jobsLock = new object();
        public static void QueueUserWorkItem(WaitCallback task, object with = null) {
            if(pool == null){
                InitializePool();
            }
            lock(jobsLock){
                jobs.Push((task, with));
            }
        }

        private static void InitializePool(){
            pool = new List<Thread>(ThreadCount);
            for(int i = 0; i < ThreadCount; i++){
                Thread t = new Thread(() => Process());
                pool.Add(t);
                t.Start();
            }
        }

        private static void Process(){
            while(true) {
                (WaitCallback callback, object obj) job;
                while(true) {
                    lock(jobsLock) {
                        if(jobs.Any()) {
                            job = jobs.Pop();
                            break;
                        }
                    }
                    Thread.Sleep(10);
                }
                    
                job.callback(job.obj);
            }
        }
    }
}
