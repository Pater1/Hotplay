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
using Newtonsoft.Json;

namespace Hotplay.Common.FileChain.ChainLinks.Network.Commands
{
    public class CreateEntryRequestNetworkCommand<T>: INetworkCommand<T> where T: NetworkClientEntry {
        [JsonProperty]
        public string ParentPath { get; set; }
        [JsonProperty]
        public string DocumentName { get; set; }

        [JsonIgnore]
        public JsonSerializer Serializer { get; set; }
        [JsonIgnore]
        public IFileSystem FileSystem { get; set; }
        [JsonIgnore]
        public Func<Task<WebSocket>> Transport { get; set; }

        [JsonIgnore]
        public T Result { get; private set; }

        public CreateEntryRequestNetworkCommand() { }
        public CreateEntryRequestNetworkCommand(string parentPath, string documentName, Func<Task<WebSocket>> transport, JsonSerializer serializer) {
            ParentPath = parentPath;
            DocumentName = documentName;
            Transport = transport;
            Serializer = serializer;
        }


        public async Task ExecuteOnClient() {
            ReceiveString rs = new ReceiveString(new ReceiveBytes(Transport));
            string ser = await rs.AwaitFlowResult();

            using(StringReader sr = new StringReader(ser)) {
                using(JsonReader jr = new JsonTextReader(sr)) {
                    Result = Serializer.Deserialize<T>(jr);
                }
            }
        }
        //public IEnumerable<IFlowControl> ExecuteOnClient() {
        //    ReceiveString rs = new ReceiveString(new ReceiveBytes(Transport));
        //    yield return rs;
        //    string ser = rs.Result;

        //    using(StringReader sr = new StringReader(ser)) {
        //        using(JsonReader jr = new JsonTextReader(sr)) {
        //            Result = Serializer.Deserialize<T>(jr);
        //        }
        //    }
        //}

        public async Task ExecuteOnServer() {
            ICollection root = (await FileSystem.SelectAsync(ParentPath, CancellationToken.None))
                                            .Collection;

            IEntry toSend;
            if(typeof(IDocument).IsAssignableFrom(typeof(T))) {
                toSend = new NetworkClientDocument(await root.CreateDocumentAsync(DocumentName, CancellationToken.None));
            } else {
                toSend = new NetworkClientCollection(await root.CreateCollectionAsync(DocumentName, CancellationToken.None));
            }

            using(StringWriter sw = new StringWriter()) {
                using(JsonWriter jw = new JsonTextWriter(sw)) {
                    Serializer.Serialize(jw, toSend);
                    await(new SendString(sw.ToString(), new SendBytes(Transport))).AwaitFlowControl();
                }
            }
        }
        //public IEnumerable<IFlowControl> ExecuteOnServer() {
        //    ICollection root = FileSystem.SelectAsync(ParentPath, CancellationToken.None)
        //                                    .RunSync()
        //                                    .Collection;

        //    IEntry toSend;
        //    if(typeof(IDocument).IsAssignableFrom(typeof(T))) {
        //        toSend = new NetworkClientDocument(root.CreateDocumentAsync(DocumentName, CancellationToken.None).RunSync());
        //    } else {
        //        toSend = new NetworkClientCollection(root.CreateCollectionAsync(DocumentName, CancellationToken.None).RunSync());
        //    }

        //    using(StringWriter sw = new StringWriter()) {
        //        using(JsonWriter jw = new JsonTextWriter(sw)) {
        //            Serializer.Serialize(jw, toSend);
        //            yield return new SendString(sw.ToString(), new SendBytes(Transport));
        //        }
        //    }
        //}

        public Task ServerSideSetup(NetworkAccessServer server) {
            FileSystem = server.FileSystem;
            Serializer = server.Serializer;

            return Task.CompletedTask;
        }

        public bool Equals(INetworkCommand networkCommand) {
            if(!(networkCommand is CreateEntryRequestNetworkCommand<T>)) return false;

            CreateEntryRequestNetworkCommand<T> other = (CreateEntryRequestNetworkCommand<T>)networkCommand;
            return ParentPath.Equals(other.ParentPath) && DocumentName.Equals(other.DocumentName);
        }

        public bool RequestManualUnlock(Action lockAction) {
            return false;
        }
    }
}
