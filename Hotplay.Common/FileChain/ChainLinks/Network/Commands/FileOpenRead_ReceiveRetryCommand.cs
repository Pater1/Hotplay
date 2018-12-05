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
//    public class FileOpenRead_ReceiveRetryCommand: INetworkCommand {
//        [JsonProperty]
//        public string EntryFullPath { get; set; }
//        [JsonProperty]
//        public long RestartPointer { get; set; }

//        [JsonIgnore]
//        public Func<Task<WebSocket>> Transport { get; set; }
//        [JsonIgnore]
//        public JsonSerializer Serializer { get; set; }

//        [JsonIgnore]
//        public ReceiveBytesStream ReceiveBytesStream { get; set; }
//        public IEnumerable<IFlowControl> ExecuteOnClient() {
//            string serializedJob;
//            using(StringWriter send = new StringWriter()) {
//                using(JsonWriter jw = new JsonTextWriter(send)) {
//                    Serializer.Serialize(jw, this);
//                    serializedJob = send.ToString();
//                }
//            }

//            SendString sendString = new SendString(serializedJob, new SendBytes(Transport));
//            yield return sendString;//.AwaitFlowControl();

//            ReceiveBytes<Stream> receiveBytes = new ReceiveBytes<Stream>(Transport);
//            ReceiveBytesStream.InternalReceive = receiveBytes;
//            yield return receiveBytes;
//        }

//        public bool RequestManualUnlock(Action lockAction) {
//            return false;
//        }

//        [JsonIgnore]
//        public IFileSystem FileSystem { get; set; }
//        public IEnumerable<IFlowControl> ExecuteOnServer() {
//            SelectionResult selection = FileSystem.SelectAsync(EntryFullPath, CancellationToken.None).RunSync();
//            SendBytes sb = new SendBytes(Transport);
//            Stream stream;
//            if(selection.ResultType == SelectionResultType.FoundDocument) {
//                IDocument doc = selection.Document;
//                stream = doc.OpenReadAsync(CancellationToken.None).RunSync();
//                stream.Position = RestartPointer;
//                sb.Stream = stream;
//            } else {
//                throw new InvalidOperationException("Requested Document is not a Document!");
//            }
//            yield return sb;

//            if(stream != null) stream.Dispose();
//        }

//        public Task ServerSideSetup(NetworkAccessServer server) {
//            FileSystem = server.FileSystem;

//            return Task.CompletedTask;
//        }

//        public bool Equals(INetworkCommand networkCommand) {
//            if(!(networkCommand is FileOpenReadNetworkCommand)) return false;

//            FileOpenReadNetworkCommand other = (FileOpenReadNetworkCommand)networkCommand;
//            return EntryFullPath.Equals(other.EntryFullPath);
//        }
//    }
//}