//using FubarDev.WebDavServer.FileSystem;
//using Hotplay.Common.Extentions;
//using Hotplay.Common.FileChain.ChainLinks.Network.Commands.CommandFlowControls;
//using Hotplay.Common.FileChain.ChainLinks.Network.Commands.CommandFlowControls.Receive;
//using Hotplay.Common.FileChain.ChainLinks.Network.Commands.CommandFlowControls.Send;
//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Net.WebSockets;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Hotplay.Common.FileChain.ChainLinks.Network.Commands {
//    public class EntryRequestNetworkCommand: INetworkCommand<NetworkClientEntry> {
//        [JsonProperty]
//        public string EntryFullPath{ get; set; }
//        [JsonIgnore]
//        public Func<Task<WebSocket>> Transport { get; set; }
//        [JsonIgnore]
//        public JsonSerializer Serializer { get; set; }

//        public EntryRequestNetworkCommand(string entryFullPath, 
//            Func<Task<WebSocket>> transport,
//            JsonSerializer serializer)
//        {
//            EntryFullPath = entryFullPath;
//            Transport = transport;
//            Serializer = serializer;

//            Result = null;
//            FileSystem = null;
//        }
//        public EntryRequestNetworkCommand() { }

//        [JsonIgnore]
//        public NetworkClientEntry Result { get; set; }
//        [JsonIgnore]
//        public IFileSystem FileSystem{ get; set; }

//        public IEnumerable<IFlowControl> ExecuteOnClient() {
//            ReceiveBytes byteTransport = new ReceiveBytes(Transport);
//            ReceiveString stringTransport = new ReceiveString(byteTransport);

//            yield return new WaitForSync(Transport);
            
//            ReceiveEnum<SelectionResultType> receiveEnum = new ReceiveEnum<SelectionResultType>(stringTransport);
//            yield return receiveEnum;
//            SelectionResultType selResType = receiveEnum.Result;

//            Result = null;
//            switch(selResType) {
//                case SelectionResultType.FoundCollection:
//                    NetworkClientCollection collection = new NetworkClientCollection();
//                    foreach(IFlowControl flow in ReceiveCollection(collection, byteTransport)) {
//                        yield return flow;
//                    }
//                    Result = collection;
//                    break;

//                case SelectionResultType.FoundDocument:
//                    NetworkClientDocument document = new NetworkClientDocument();
//                    foreach(IFlowControl flow in ReceiveDocument(document, byteTransport)) {
//                        yield return flow;
//                    }
//                    Result = document;
//                    break;

//                case SelectionResultType.MissingCollection:
//                    break;

//                case SelectionResultType.MissingDocumentOrCollection:
//                    break;
//            }
//        }
//        private IEnumerable<IFlowControl> ReceiveCollection(NetworkClientCollection col, ReceiveBytes rawTransport = null) {
//            if(rawTransport != null) {
//                rawTransport = new ReceiveBytes(Transport);
//            }

//            foreach(IFlowControl flow in ReceiveEntry(col, rawTransport)) {
//                yield return flow;
//            }
//        }
//        private IEnumerable<IFlowControl> ReceiveDocument(NetworkClientDocument doc, ReceiveBytes rawTransport = null) {
//            if(rawTransport != null) {
//                rawTransport = new ReceiveBytes(Transport);
//            }

//            foreach(IFlowControl flow in ReceiveEntry(doc, rawTransport)) {
//                yield return flow;
//            }
//            ReceiveNumber receiveLength = new ReceiveNumber(rawTransport);
//            yield return receiveLength;
//            doc.Length = (long)receiveLength.Result;
//        }
//        private IEnumerable<IFlowControl> ReceiveEntry(NetworkClientEntry entry, ReceiveBytes rawTransport = null) {
//            if(rawTransport != null) {
//                rawTransport = new ReceiveBytes(Transport);
//            }
//            ReceiveBytes rawTrans = rawTransport;

//            ReceiveString receiveName = new ReceiveString(rawTrans);
//            yield return receiveName;
//            entry.Name = receiveName.Result;

//            ReceiveString receivePath = receiveName;
//            yield return receivePath;
//            entry.Path = new Uri(receivePath.Result, UriKind.Relative);


//            ReceiveNumber receiveLastWriteTimeUtc = new ReceiveNumber(rawTrans);
//            yield return receiveLastWriteTimeUtc;
//            entry.LastWriteTimeUtc = (DateTime)receiveLastWriteTimeUtc.Result;

//            ReceiveNumber receiveCreationTimeUtc = receiveLastWriteTimeUtc;
//            yield return receiveCreationTimeUtc;
//            entry.CreationTimeUtc = (DateTime)receiveCreationTimeUtc.Result;
//        }

//        public IEnumerable<IFlowControl> ExecuteOnServer() {
//            ArraySegment<byte> buffer = WebSocket.CreateServerBuffer(512);
//            yield return new WaitForSync(Transport);

//            SelectionResult selRes;
//            if(string.IsNullOrEmpty(EntryFullPath)) {
//                selRes = SelectionResult.Create(FileSystem.Root.Task.RunSync());
//            } else {
//                selRes = FileSystem.SelectAsync(EntryFullPath, CancellationToken.None).RunSync();
//            }
//            yield return new SendEnum<SelectionResultType>(selRes.ResultType, buffer, Transport);
            
//            SendBytes rawTransport = new SendBytes(Transport);
//            switch(selRes.ResultType) {
//                case SelectionResultType.FoundCollection:
//                    //yield return new SendString(COLLECTION_DELIMITER, rawTransport.Value);
//                    foreach(IFlowControl flow in SendFoundCollection(selRes.Collection)){
//                        yield return flow;
//                    }
//                    break;

//                case SelectionResultType.FoundDocument:
//                    //yield return new SendString(DOCUMENT_DELIMITER, rawTransport.Value);
//                    foreach(IFlowControl flow in SendFoundDocument(selRes.Document)) {
//                        yield return flow;
//                    }
//                    break;

//                case SelectionResultType.MissingCollection:
//                    //yield return new SendString(MISSING_COLLECTION_DELIMITER, rawTransport.Value);
//                    break;

//                case SelectionResultType.MissingDocumentOrCollection:
//                    //yield return new SendString(MISSING_DOCUMENT_OR_COLLECTION_DELIMITER, rawTransport.Value);
//                    break;
//            }
//        }
        
//        private IEnumerable<IFlowControl> SendFoundCollection(ICollection col, SendBytes rawTransport = null) {
//            if(rawTransport != null) {
//                rawTransport = new SendBytes(Transport);
//            }

//            foreach(IFlowControl flow in SendFoundEntry(col, rawTransport)) {
//                yield return flow;
//            }
//        }
//        private IEnumerable<IFlowControl> SendFoundDocument(IDocument doc, SendBytes rawTransport = null) {
//            if(rawTransport != null) {
//                rawTransport = new SendBytes(Transport);
//            }

//            foreach(IFlowControl flow in SendFoundEntry(doc, rawTransport)) {
//                yield return flow;
//            }
//            yield return new SendNumber(doc.Length, rawTransport);
//        }
//        private IEnumerable<IFlowControl> SendFoundEntry(IEntry entry, SendBytes rawTransport = null) {
//            if(rawTransport != null) {
//                rawTransport = new SendBytes(Transport);
//            }
//            SendBytes rawTrans = rawTransport;

//            yield return new SendString(entry.Name, rawTrans);
//            yield return new SendString(entry.Path.OriginalString, rawTrans);

//            yield return new SendNumber(entry.LastWriteTimeUtc, rawTrans);
//            yield return new SendNumber(entry.CreationTimeUtc, rawTrans);
//        }

//        public Task ServerSideSetup(NetworkAccessServer server) {
//            FileSystem = server.FileSystem;
//            Serializer = server.Serializer;

//            return Task.CompletedTask;
//        }

//        public bool Equals(INetworkCommand networkCommand) {
//            if(!(networkCommand is EntryRequestNetworkCommand)) return false;

//            EntryRequestNetworkCommand other = (EntryRequestNetworkCommand)networkCommand;
//            return EntryFullPath.Equals(other.EntryFullPath);
//        }

//        public bool RequestManualUnlock(Action lockAction) {
//            return false;
//        }
//    }
//}
