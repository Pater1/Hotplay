using Hotplay.Common.FileChain.ChainLinks.Network.Commands;
using Hotplay.Common.Threads;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;
using Hotplay.Common.Extentions;
using Hotplay.Common.FileChain.ChainLinks.Network.Commands.CommandFlowControls.Receive;
using Hotplay.Common.FileChain.ChainLinks.Network.Commands.CommandFlowControls;
using FubarDev.WebDavServer.FileSystem;
using Hotplay.Common.FileChain.ChainLinks.LocalDiskAccess;
using Hotplay.Common.FileChain.ChainLinks.Network.Commands.CommandFlowControls.Send;

namespace Hotplay.Common.FileChain.ChainLinks.Network {
    public class NetworkAccessServer: IDisposable {
        private static Dictionary<ulong, NetworkAccessServer> Tracker { get; set; } =
            new Dictionary<ulong, NetworkAccessServer>();
        public static NetworkAccessServer NewTracked(ulong key, Func<Task<WebSocket>> webSocket) {
            NetworkAccessServer ret = new NetworkAccessServer(webSocket().RunSync(), key);
            if(!Tracker.ContainsKey(key)) {
                Tracker.Add(key, ret);
            } else {
                Tracker[key].Dispose();
                Tracker[key] = ret;
            }
            return ret;
        }

        public ulong Key { get; private set; }
        private WebSocket Socket { get; set; }

        public NetworkAccessServer(WebSocket socket, ulong key = 0) {
            Key = key;
            Socket = socket;

            ChainedFileSystem FileSystem = new ChainedFileSystem();
            IFileChain[] chains = new IFileChain[]{
                new LocalDiskAccessFileChain(new LocalDiskAccessFileChain.LocalDiskAccessFileChain_Options(){
                        RootPath = @"E:\\Testing"
                    }, FileSystem)
            };
            FileSystem.SetupFileChain(chains);
            this.FileSystem = FileSystem;
        }
        public void Start() {
            //DaemonThreadPool.QueueUserWorkItem((x) =>
            AsyncExtentions.RunSync(() =>
                RunSafeDaemon(() =>
                    Process()))
                    //)
                    ;
        }
        public Task StartAsync() {
            return RunSafeDaemon(() => Process());
        }

        private async Task RunSafeDaemon(Func<Task> toRun) {
            try {
                while(!disposed) {
                    await toRun();
                }
            } catch(Exception e) {
                if(!disposed && Socket.State == WebSocketState.Open) {
                    SendBytes sb = new SendBytes(() => Task.FromResult(Socket));
                    sb.Exception = e;
                    await sb.AwaitFlowControl();
                }
            } finally {
                Socket.Abort();
                Dispose();
            }
        }


        private async Task Process() {
            INetworkCommand networkCommand = await Receive();
            networkCommand.Transport = () => Task.FromResult(Socket);
            await networkCommand.ServerSideSetup(this);
            await networkCommand.ExecuteOnServer();//.ExecuteSyncronizedCommand();
        }
        public JsonSerializer Serializer { get; set; } = new JsonSerializer() {
            TypeNameHandling = TypeNameHandling.All
        };
        public IFileSystem FileSystem { get; internal set; }

        ArraySegment<byte> readArraySegment = WebSocket.CreateClientBuffer(512, 512);
        IFlowControl<string> ReceiveString = null;
        private async Task<INetworkCommand> Receive() {
            if(ReceiveString == null) {
                ReceiveString = new ReceiveString(
                    new ReceiveBytes(() => Task.FromResult(Socket))
                );
            }

            string json = await ReceiveString.AwaitFlowResult();
            using(JsonReader jr = new JsonTextReader(new StringReader(json))) {
                return Serializer.Deserialize<INetworkCommand>(jr);
            }
        }

        private bool disposed = false;
        public void Dispose() {
            Tracker.Remove(Key);

            try {
                Socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure,
                    "Network Access Server Disposed.", CancellationToken.None)
                    .RunSync();
            } catch(Exception e) {

            } finally {
                Socket.Dispose();
                disposed = true;
            }
        }
    }
}
