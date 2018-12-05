using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hotplay.Common.Threads {
    public static class DaemonThreadPool {
        private static object avaliableDaemonsLock = new object();
        private static List<WrappedThread> avaliableDaemons = new List<WrappedThread>();
        private static List<WrappedThread> busyDaemons = new List<WrappedThread>();

        private static object exceptionsLock = new object();
        private static List<Exception> exceptions = new List<Exception>();
        public static void QueueUserWorkItem(WaitCallback task, object with = null) {
            lock(exceptionsLock){
                if(exceptions.Any()){
                    exceptions.Clear();
                }
            }

            WrappedThread wt;
            lock(avaliableDaemonsLock) {
                if(avaliableDaemons.Any()) {
                    wt = avaliableDaemons.First();
                    avaliableDaemons.Remove(wt);
                } else {
                    wt = new WrappedThread();
                }
                busyDaemons.Add(wt);
            }

            wt.Run(task, with);
        }

        private class WrappedThread: IDisposable{
            public Thread thread{ get; private set; }

            public (WaitCallback, object)? callback { get; set; }

            public WrappedThread(){
                thread = new Thread(async () => await Run());
                thread.Start();
                DisposalAssistant._TrackForDisposal(this);
            }

            public void Run(WaitCallback task, object with) {
                callback = (task, with);
            }
            private async Task Run() {
                while(!disposed) {
                    while(!callback.HasValue) {
                        await Task.Delay(10);
                    }

                    try {
                        callback.Value.Item1.Invoke(callback.Value.Item2);
                    } catch(Exception e) {
                        lock(exceptionsLock) {
                            exceptions.Add(e);
                        }
                    }

                    callback = null;

                    lock(avaliableDaemonsLock) {
                        avaliableDaemons.Add(this);
                        busyDaemons.Remove(this);
                    }
                }
            }

            private bool disposed = false;
            public void Dispose() {
                disposed = true;
            }
        }
    }
}
