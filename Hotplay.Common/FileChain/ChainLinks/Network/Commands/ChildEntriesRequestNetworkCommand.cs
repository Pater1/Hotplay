using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace Hotplay.Common.FileChain.ChainLinks.Network.Commands {
    public class ChildEntriesRequestNetworkCommand: INetworkCommand<IReadOnlyCollection<NetworkClientEntry>> {

        [JsonProperty]
        public string EntryFullPath { get; set; }

        public ChildEntriesRequestNetworkCommand() { }
        public ChildEntriesRequestNetworkCommand(string entryFullPath,
            Func<Task<WebSocket>> transport,
            JsonSerializer serializer,
            NetworkClientCollection root) 
        {
            EntryFullPath = entryFullPath;
            Transport = transport;
            Serializer = serializer;
            Root = root;

            Result = null;
            FileSystem = null;
        }

        [JsonIgnore]
        public IReadOnlyCollection<NetworkClientEntry> Result { get; private set; }

        [JsonIgnore]
        public Func<Task<WebSocket>> Transport { get; set; }
        [JsonIgnore]
        public ICollection Root { get; set; }

        public async Task ExecuteOnClient() {
            ReceiveString rs = new ReceiveString(new ReceiveBytes(Transport));
            string ser = await rs.AwaitFlowResult();

            IEnumerable<NetworkClientEntry> entries;
            using(StringReader sr = new StringReader(ser)) {
                using(JsonReader jr = new JsonTextReader(sr)) {
                    entries = Serializer.Deserialize<NetworkClientEntry[]>(jr).Where(x => x != null);
                }
            }

            NetworkClientCollection ncc = Root as NetworkClientCollection;
            //Parallel.ForEach(entries, nce => {
            //    nce.ParentSetup(ncc);
            //});
            foreach(var nce in entries) {
                nce.ParentSetup(ncc);
            }
            Result = entries.ToArray();
        }
        //public IEnumerable<IFlowControl> ExecuteOnClient() {
        //    ReceiveString rs = new ReceiveString(new ReceiveBytes(Transport));
        //    yield return rs;
        //    string ser = rs.Result;

        //    IEnumerable<NetworkClientEntry> entries;
        //    using(StringReader sr = new StringReader(ser)) {
        //        using(JsonReader jr = new JsonTextReader(sr)) {
        //            entries = Serializer.Deserialize<NetworkClientEntry[]>(jr).Where(x => x != null);
        //        }
        //    }

        //    NetworkClientCollection ncc = Root as NetworkClientCollection;
        //    //Parallel.ForEach(entries, nce => {
        //    //    nce.ParentSetup(ncc);
        //    //});
        //    foreach(var nce in entries) {
        //        nce.ParentSetup(ncc);
        //    }
        //    Result = entries.ToArray();
        //}

        public async Task ExecuteOnServer() {
            Root = (await FileSystem.SelectAsync(EntryFullPath, CancellationToken.None))
                                            .Collection;

            IReadOnlyCollection<IEntry> serAndSend = await Root.GetChildrenAsync(CancellationToken.None);
            NetworkClientEntry[] toSend = new NetworkClientEntry[serAndSend.Count];
            int i = 0;
            foreach(var x in serAndSend) {
                int j = i++;
                if(x is ICollection) {
                    toSend[j] = (new NetworkClientCollection(x as ICollection));
                } else {
                    toSend[j] = (new NetworkClientDocument(x as IDocument));
                }
            }
            //Parallel.ForEach(serAndSend, x => {
            //    int j = i++;
            //    if(x is ICollection) {
            //        toSend[j] = (new NetworkClientCollection(x as ICollection));
            //    } else {
            //        toSend[j] = (new NetworkClientDocument(x as IDocument));
            //    }
            //});

            using(StringWriter sw = new StringWriter()) {
                using(JsonWriter jw = new JsonTextWriter(sw)) {
                    Serializer.Serialize(jw, toSend);
                    await(new SendString(sw.ToString(), new SendBytes(Transport))).AwaitFlowControl();
                }
            }
        }
        //public IEnumerable<IFlowControl> ExecuteOnServer() {
        //    Root = FileSystem.SelectAsync(EntryFullPath, CancellationToken.None)
        //                                    .RunSync()
        //                                    .Collection;

        //    IReadOnlyCollection<IEntry> serAndSend = Root.GetChildrenAsync(CancellationToken.None)
        //                                    .RunSync();
        //    NetworkClientEntry[] toSend = new NetworkClientEntry[serAndSend.Count];
        //    int i = 0;
        //    foreach(var x in serAndSend) {
        //        int j = i++;
        //        if(x is ICollection) {
        //            toSend[j] = (new NetworkClientCollection(x as ICollection));
        //        } else {
        //            toSend[j] = (new NetworkClientDocument(x as IDocument));
        //        }
        //    }
        //    //Parallel.ForEach(serAndSend, x => {
        //    //    int j = i++;
        //    //    if(x is ICollection) {
        //    //        toSend[j] = (new NetworkClientCollection(x as ICollection));
        //    //    } else {
        //    //        toSend[j] = (new NetworkClientDocument(x as IDocument));
        //    //    }
        //    //});

        //    using(StringWriter sw = new StringWriter()) {
        //        using(JsonWriter jw = new JsonTextWriter(sw)) {
        //            Serializer.Serialize(jw, toSend);
        //            yield return new SendString(sw.ToString(), new SendBytes(Transport));
        //        }
        //    }
        //}

        [JsonIgnore]
        public IFileSystem FileSystem { get; set; }
        [JsonIgnore]
        public JsonSerializer Serializer { get; set; }
        public Task ServerSideSetup(NetworkAccessServer server) {
            FileSystem = server.FileSystem;
            Serializer = server.Serializer;

            return Task.CompletedTask;
        }

        public bool Equals(INetworkCommand networkCommand) {
            if(!(networkCommand is ChildEntriesRequestNetworkCommand)) return false;

            ChildEntriesRequestNetworkCommand other = (ChildEntriesRequestNetworkCommand)networkCommand;
            return EntryFullPath.Equals(other.EntryFullPath);
        }

        public bool RequestManualUnlock(Action lockAction) {
            return false;
        }
    }
}
