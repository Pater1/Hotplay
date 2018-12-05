using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FubarDev.WebDavServer.FileSystem;
using Hotplay.Common.Extentions;
using Hotplay.Common.FileChain.ChainLinks.Network.Commands.CommandFlowControls;
using Hotplay.Common.FileChain.ChainLinks.Network.Commands.CommandFlowControls.Receive;
using Hotplay.Common.FileChain.ChainLinks.Network.Commands.CommandFlowControls.Send;
using Hotplay.Common.FileChain.ChainLinks.Network.NetworkEntries;
using Newtonsoft.Json;

namespace Hotplay.Common.FileChain.ChainLinks.Network.Commands {
    public class EntryGetNetworkCommand: INetworkCommand<SelectionResult> {
        public SelectionResult Result { get; set; }

        [JsonProperty]
        public string EntryFullPath { get; set; }

        public EntryGetNetworkCommand() { }
        public EntryGetNetworkCommand(string entryFullPath,
            Func<Task<WebSocket>> transport,
            JsonSerializer serializer) {
            EntryFullPath = entryFullPath;
            Transport = transport;
            Serializer = serializer;

            Result = null;
            FileSystem = null;
        }

        public async Task ExecuteOnClient() {
            ReceiveString rs = new ReceiveString(new ReceiveBytes(Transport));
            string ser = await rs.AwaitFlowResult();

            NetworkSelectionResult result;
            using(StringReader sr = new StringReader(ser)) {
                using(JsonReader jr = new JsonTextReader(sr)) {
                    result = Serializer.Deserialize<NetworkSelectionResult>(jr);

                    result.FileSystem = FileSystem;
                    result.Serializer = Serializer;
                    result.Transport = Transport;

                    Result = result.AsSelectionResult();
                }
            }
        }

        public async Task ExecuteOnServer() {
            SelectionResult result = await FileSystem.SelectAsync(EntryFullPath, CancellationToken.None);

            NetworkSelectionResult toSend = new NetworkSelectionResult(result);

            string send;
            using(StringWriter sw = new StringWriter()) {
                using(JsonWriter jw = new JsonTextWriter(sw)) {
                    Serializer.Serialize(jw, toSend);
                    send = sw.ToString();
                    await(new SendString(send, new SendBytes(Transport))).AwaitFlowControl();
                }
            }
        }
        //public IEnumerable<IFlowControl> ExecuteOnClient() {
        //    ReceiveString rs = new ReceiveString(new ReceiveBytes(Transport));
        //    yield return rs;
        //    string ser = rs.Result;

        //    NetworkSelectionResult result;
        //    using(StringReader sr = new StringReader(ser)) {
        //        using(JsonReader jr = new JsonTextReader(sr)) {
        //            result = Serializer.Deserialize<NetworkSelectionResult>(jr);

        //            result.FileSystem = FileSystem;
        //            result.Serializer = Serializer;
        //            result.Transport = Transport;

        //            Result = result.AsSelectionResult();
        //        }
        //    }
        //}

        //public IEnumerable<IFlowControl> ExecuteOnServer() {
        //    SelectionResult result = FileSystem.SelectAsync(EntryFullPath, CancellationToken.None).RunSync();

        //    NetworkSelectionResult toSend = new NetworkSelectionResult(result);

        //    string send;
        //    using(StringWriter sw = new StringWriter()) {
        //        using(JsonWriter jw = new JsonTextWriter(sw)) {
        //            Serializer.Serialize(jw, toSend);
        //            send = sw.ToString();
        //            yield return new SendString(send, new SendBytes(Transport));
        //        }
        //    }
        //}

        [JsonIgnore]
        public IFileSystem FileSystem { get; set; }
        [JsonIgnore]
        public JsonSerializer Serializer { get; set; }

        [JsonIgnore]
        public Func<Task<WebSocket>> Transport { get; set; }
        public Task ServerSideSetup(NetworkAccessServer server) {
            FileSystem = server.FileSystem;
            Serializer = server.Serializer;

            return Task.CompletedTask;
        }

        public bool Equals(INetworkCommand networkCommand) {
            if(!(networkCommand is EntryGetNetworkCommand)) return false;

            EntryGetNetworkCommand other = (EntryGetNetworkCommand)networkCommand;
            return EntryFullPath.Equals(other.EntryFullPath);
        }

        public bool RequestManualUnlock(Action lockAction) {
            return false;
        }
    }
}
