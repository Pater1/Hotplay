using Hotplay.Common.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hotplay.Common.FileChain.ChainLinks.Network.Commands.CommandFlowControls.Receive {
    public class ReceiveBytes: ReceiveBytes<MemoryStream> {
        public ReceiveBytes() { }
        public ReceiveBytes(Func<Task<WebSocket>> transport, int bufferSize = DEFAULT_BUFFER_SIZE) 
            :base(transport, () => Task.FromResult(new MemoryStream()), bufferSize){}
    }
    public class ReceiveBytes<T>: IFlowControl<T> where T: Stream {
        public const int DEFAULT_BUFFER_SIZE = 1 << 10;
        public T Result { get; set; }
        private Func<Task<T>> StreamFactory{ get; set; }
        private Func<Task<WebSocket>> Transport { get; set; }
        private byte[] Buffer { get; set; }

        public ReceiveBytes() { }
        public ReceiveBytes(Func<Task<WebSocket>> transport, Func<Task<T>> streamFactory = null, int bufferSize = DEFAULT_BUFFER_SIZE) {
            Transport = transport;
            StreamFactory = streamFactory;
            Buffer = new byte[bufferSize];
        }

        public async Task AwaitFlowControl() {
            if(Result == null) Result = await StreamFactory();

            bool resultIsError = false;

            WebSocketReceiveResult result;
            do {
                WebSocket webSocket = await Transport();
                result = await webSocket.ReceiveAsync(Buffer, CancellationToken.None);

                if(result.MessageType == WebSocketMessageType.Text) {
                    resultIsError = true;
                } else if(result.MessageType == WebSocketMessageType.Close) {
                    await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure,
                        "received, preplied",
                        CancellationToken.None);
                    throw new Exception("Socket close signal received unexpectedly; in ReceiveBytes");
                }

                Result.Write(Buffer, 0, result.Count);
            } while(!result.EndOfMessage);

            if(Result.CanSeek) {
                Result.Position = 0;
            }

            if(resultIsError){
                string raw;
                using(MemoryStream mem = new MemoryStream()) {
                    Result.Position = 0;
                    Result.CopyTo(mem);
                    raw = Encoding.ASCII.GetString(mem.ToArray());
                }
                Exception wrapAndThrow = RemoteExceptionHelper.StandardDeserializeException(raw);
                throw new AggregateException("Remote exception thrown!", wrapAndThrow);
            }
        }
    }
}
