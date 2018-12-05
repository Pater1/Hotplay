using Hotplay.Common.Extentions;
using Hotplay.Common.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hotplay.Common.FileChain.ChainLinks.Network.Commands.CommandFlowControls.Send {
    public class SendBytes: IFlowControl {
        public const int DEFAULT_BUFFER_SIZE = 1 << 10;
        public Stream Stream { get; set; }
        public byte[] Bytes {
            set {
                if(value == null) return;
                Stream = new MemoryStream(value);
            }
        }
        public Exception Exception{
            set{
                if(value == null) return;
                string ser = RemoteExceptionHelper.StandardSerializeException(value);
                Bytes = Encoding.ASCII.GetBytes(ser);
                IsError = true;
            }
        }
        private byte[] Buffer { get; set; }
        private Func<Task<WebSocket>> Transport { get; set; }
        public bool IsError { get; set; } = false;

        public SendBytes() { }
        public SendBytes(Func<Task<WebSocket>> transport):
            this((Stream)null, transport){ }
        public SendBytes(byte[] toSend, Func<Task<WebSocket>> transport) :
            this(new MemoryStream(toSend), transport) { }
        public SendBytes(Stream toSend, Func<Task<WebSocket>> transport) {
            Stream = toSend;
            Buffer = new byte[DEFAULT_BUFFER_SIZE];
            Transport = transport;
        }

        public async Task AwaitFlowControl() {
            if(Stream == null) {
                Exception e = new ArgumentNullException("Stream of data to send must not be null!");
                //TODO: Send Exception
                throw e;
            }

            int read = 0;
            do {
                read = await Stream.ReadAsync(Buffer, 0, Buffer.Length);
                WebSocket wb = await Transport();
                await wb.SendAsync(Buffer.Take(read).ToArray(), IsError? WebSocketMessageType.Text : WebSocketMessageType.Binary, read <= 0, CancellationToken.None);
            } while(read > 0);
        }
    }
}
