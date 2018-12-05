using Hotplay.Common.Extentions;
using Hotplay.Common.FileChain.ChainLinks.Network.Commands.CommandFlowControls;
using Hotplay.Common.FileChain.ChainLinks.Network.Commands.CommandFlowControls.Send;
using Hotplay.Common.Helpers;
using Hotplay.Common.Threads;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hotplay.Common.FileChain.ChainLinks.Network.Commands {
    public interface INetworkCommand {
        //IEnumerable<IFlowControl> ExecuteOnClient();
        //IEnumerable<IFlowControl> ExecuteOnServer();
        Task ExecuteOnClient();
        Task ExecuteOnServer();
        Func<Task<WebSocket>> Transport { get; set; }
        Task ServerSideSetup(NetworkAccessServer server);

        bool Equals(INetworkCommand networkCommand);

        bool RequestManualUnlock(Action lockAction);
    }
    public interface INetworkCommand<T>: INetworkCommand {
        T Result { get; }
    }
    public static class INetworkCommandExtentions {
        public static async Task<T> SyncronizedExecute<T>(this T command, Func<Task<WebSocket>> transport, JsonSerializer serializer, long queueKey = 0) where T : INetworkCommand {
            string key = $"SyncronizedExecute_{queueKey}";
            bool retry = true, manualUnlock = false;
            while(true) {
                try {
                    RequestLockManager.LockAndWait(key);
                    bool tmpManualUnlock = command.RequestManualUnlock(() => RequestLockManager.Unlock(key));
                    await command.ExecuteClientSyncronizedCommand(serializer, transport);
                    manualUnlock = tmpManualUnlock;
                    retry = false;
                    return command;
                } catch {
                    
                } finally {
                    if(!manualUnlock) {
                        RequestLockManager.Unlock(key);
                    }
                    if(retry){
                        await Task.Yield();
                    }
                }
            }
        }

        public static async Task ExecuteClientSyncronizedCommand(this INetworkCommand command, JsonSerializer serializer, Func<Task<WebSocket>> transport) {
            string serializedJob;
            using(StringWriter send = new StringWriter()) {
                using(JsonWriter jw = new JsonTextWriter(send)) {
                    serializer.Serialize(jw, command);
                    serializedJob = send.ToString();
                }
            }
            SendString sendString = new SendString(serializedJob, new SendBytes(transport));
            await sendString.AwaitFlowControl();

            await command.ExecuteOnClient();//.ExecuteSyncronizedCommand();
        }
        public static Task ExecuteSyncronizedCommand(this IEnumerable<IFlowControl> flowControls) {
            return ExecuteSyncronizedCommand(flowControls.GetEnumerator());
        }
        public static async Task ExecuteSyncronizedCommand(this IEnumerator<IFlowControl> flowControls) {
            while(flowControls.MoveNext()) {
                await flowControls.Current.AwaitFlowControl();
            }
        }

        //private static object networkProcessesLock = new object();
        //private static Dictionary<long, NetworkProcessQueue> networkProcesses = new Dictionary<long, NetworkProcessQueue>();

        //private class NetworkProcessQueue {
        //    public long queueKey;
        //    public Queue<NetworkProcessJob> toProcess = new Queue<NetworkProcessJob>();

        //    public NetworkProcessQueue(long queueKey) {
        //        this.queueKey = queueKey;
        //    }

        //    private bool running = false;
        //    private Thread Thread;
        //    public void StartOnDaemonThread() {
        //        if(!running) {
        //            //DaemonThreadPool.QueueUserWorkItem(async (x) => await Process());

        //            //Thread = new Thread(async () => await Process());
        //            //Thread.Start();

        //            ThreadPool.QueueUserWorkItem(async (x) => await Process());

        //            running = true;
        //        }
        //    }

        //    public async Task Process() {
        //        while(true) {
        //            bool any;
        //            do {
        //                lock(toProcess) {
        //                    any = toProcess.Any();
        //                }
        //                if(!any) {
        //                    await Task.Yield();
        //                }
        //            } while(!any);


        //            NetworkProcessJob next;
        //            lock(toProcess) {
        //                next = toProcess.Dequeue();
        //            }
        //            try {
        //                await next.Process();
        //            } catch {
        //                toProcess.Enqueue(next);
        //            }

        //            lock(toProcess) {
        //                any = toProcess.Any();
        //                if(!any) {
        //                    running = false;
        //                    break;
        //                }
        //            }
        //        }
        //    }

        //    public NetworkProcessJob FindSimilar(NetworkProcessJob waitOn) {
        //        return toProcess.Where(x => x.Equals(waitOn)).FirstOrDefault();
        //    }
        //}
        //private class NetworkProcessJob {
        //    public INetworkCommand job;
        //    public Func<Task<WebSocket>> transport;
        //    public JsonSerializer serializer;
        //    public bool done = false;

        //    public Exception e = null;

        //    public NetworkProcessJob(INetworkCommand job, Func<Task<WebSocket>> transport, JsonSerializer serializer) {
        //        this.job = job;
        //        this.transport = transport;
        //        this.serializer = serializer;
        //    }

        //    public override bool Equals(object obj) {
        //        if(!(obj is NetworkProcessJob)) return false;

        //        NetworkProcessJob other = (NetworkProcessJob)obj;
        //        return other.job.Equals(job);
        //    }

        //    private int retryLimit = -1;//<0 = basically infinite
        //    public async Task Process() {
        //        try {
        //            await job.ExecuteClientSyncronizedCommand(serializer, transport);
        //            done = true;
        //        } catch(Exception ex) {
        //            if(retryLimit == 0) {
        //                e = ex;
        //                done = true;
        //            } else {
        //                retryLimit--;
        //            }
        //        } finally {
        //        }
        //    }
        //}

        //public static async Task<T> SyncronizedExecute<T>(this T command, Func<Task<WebSocket>> transport, JsonSerializer serializer, long queueKey = 0) where T : INetworkCommand {
        //    NetworkProcessJob waitOn = new NetworkProcessJob(command, transport, serializer);
        //    NetworkProcessQueue executionQueue;
        //    lock(networkProcessesLock) {
        //        if(networkProcesses.ContainsKey(queueKey)) {
        //            executionQueue = networkProcesses[queueKey];
        //        } else {
        //            executionQueue = new NetworkProcessQueue(queueKey);
        //            networkProcesses.Add(queueKey, executionQueue);
        //        }
        //    }
        //    lock(executionQueue.toProcess) {
        //        NetworkProcessJob waitLike = executionQueue.FindSimilar(waitOn);
        //        if(waitLike == null) {
        //            executionQueue.toProcess.Enqueue(waitOn);
        //        } else {
        //            waitOn = waitLike;
        //        }
        //    }
        //    executionQueue.StartOnDaemonThread();

        //    while(!waitOn.done) {
        //        await Task.Yield();
        //    }

        //    if(waitOn.e != null) {
        //        throw waitOn.e;
        //    }

        //    return (T)waitOn.job;
        //}
    }
}
