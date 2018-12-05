using FubarDev.WebDavServer.FileSystem;
using Hotplay.Common.Extentions;
using Hotplay.Common.FileChain.ChainLinks.Network.Commands.CommandFlowControls;
using Hotplay.Common.FileChain.ChainLinks.Network.Commands.CommandFlowControls.Receive;
using Hotplay.Common.FileChain.ChainLinks.Network.Commands.CommandFlowControls.Send;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Hotplay.Common.FileChain.ChainLinks.Network.Commands {
    public class FileOpenReadNetworkCommand: INetworkCommand<Stream> {
        [JsonProperty]
        public string EntryFullPath{ get; set; }
        [JsonProperty]
        public long RestartPointer { get; set; } = 0;

        [JsonIgnore]
        public Func<Task<WebSocket>> Transport { get; set; }

        [JsonIgnore]
        public IDocument Document { get; set; }

        [JsonIgnore]
        public Stream Result { get; private set; }

        [JsonIgnore]
        public Action LockAction { get; private set; }

        [JsonIgnore]
        public ReceiveBytes<Stream> ReceiveBytes { get; private set; }
        [JsonIgnore]
        public ReceiveBytesStream ReceiveBytesStream { get; private set; }
        [JsonIgnore]
        public JsonSerializer Serializer { get; set; }
        public async Task Retry() {
            string serializedJob;
            using(StringWriter send = new StringWriter()) {
                using(JsonWriter jw = new JsonTextWriter(send)) {
                    Serializer.Serialize(jw, this);
                    serializedJob = send.ToString();
                }
            }
            SendString sendString = new SendString(serializedJob, new SendBytes(Transport));
            await sendString.AwaitFlowControl();
        }
        public async Task ExecuteOnClient() {
            ReceiveBytes = new ReceiveBytes<Stream>(Transport, () => Task.FromResult((Stream)new MemoryStream()), 1 << 20/*1MB*/);
            Result = await ReceiveBytes.AwaitFlowResult();
            return;

            if(ReceiveBytes == null) {
                ReceiveBytes = new ReceiveBytes<Stream>(Transport, null, 1<<20/*1MB*/);
            }
            if(ReceiveBytesStream == null) {
                ReceiveBytesStream = new ReceiveBytesStream(Document, ReceiveBytes);
                ReceiveBytesStream.Command = this;
                ReceiveBytesStream.OnDispose.Add(LockAction);
            }
            Result = await ReceiveBytesStream.AwaitFlowResult();
        }
        //public IEnumerable<IFlowControl> ExecuteOnClient() {
        //    if(ReceiveBytes == null) {
        //        ReceiveBytes = new ReceiveBytes<Stream>(Transport);
        //    }
        //    if(ReceiveBytesStream == null) {

        //        ReceiveBytesStream = new ReceiveBytesStream(Document, ReceiveBytes);
        //        ReceiveBytesStream.Command = this;
        //        ReceiveBytesStream.OnDispose.Add(LockAction);
        //    }
        //    yield return ReceiveBytesStream;
        //    Result = ReceiveBytesStream.Result;

        //    //yield return receiveBytes;
        //    //Result = receiveBytes.Result;
        //}

        public bool RequestManualUnlock(Action lockAction) {
            LockAction = lockAction;
            return false;
        }

        [JsonIgnore]
        public IFileSystem FileSystem{ get; set; }
        public async Task ExecuteOnServer() {
            SelectionResult selection = await FileSystem.SelectAsync(EntryFullPath, CancellationToken.None);
            SendBytes sb = new SendBytes(Transport);
            if(selection.ResultType == SelectionResultType.FoundDocument) {
                IDocument doc = selection.Document;
                using(Result = await doc.OpenReadAsync(CancellationToken.None)) {
                    Result.Position = RestartPointer;
                    sb.Stream = Result;
                    await sb.AwaitFlowControl();
                }
            } else {
                throw new InvalidOperationException("Requested Document is not a Document!");
            }
        }
        //public IEnumerable<IFlowControl> ExecuteOnServer() {
        //    SelectionResult selection = FileSystem.SelectAsync(EntryFullPath, CancellationToken.None).RunSync();
        //    SendBytes sb = new SendBytes(Transport);
        //    if(selection.ResultType == SelectionResultType.FoundDocument) {
        //        IDocument doc = selection.Document;
        //        Result = doc.OpenReadAsync(CancellationToken.None).RunSync();
        //        Result.Position = RestartPointer;
        //        sb.Stream = Result;
        //    } else {
        //        throw new InvalidOperationException("Requested Document is not a Document!");
        //    }
        //    yield return sb;

        //    if(Result != null) Result.Dispose();
        //}

        public Task ServerSideSetup(NetworkAccessServer server) {
            FileSystem = server.FileSystem;

            return Task.CompletedTask;
        }

        public bool Equals(INetworkCommand networkCommand) {
            if(!(networkCommand is FileOpenReadNetworkCommand)) return false;

            FileOpenReadNetworkCommand other = (FileOpenReadNetworkCommand)networkCommand;
            return EntryFullPath.Equals(other.EntryFullPath);
        }
    }
}
