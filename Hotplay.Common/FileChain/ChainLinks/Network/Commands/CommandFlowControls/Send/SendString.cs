using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Hotplay.Common.FileChain.ChainLinks.Network.Commands.CommandFlowControls.Send {
    public class SendString: IFlowControl {
        public string ToSend { get; set; }

        private SendBytes SendBytes { get; set; }
        private Encoding Encoding { get; set; }

        public SendString() { }
        public SendString(string toSend, SendBytes sendBytes, Encoding encoding = null) {
            ToSend = toSend;
            SendBytes = sendBytes;
            Encoding = encoding != null ? encoding : Encoding.ASCII;
        }
        public SendString(string toSend, ArraySegment<byte> buffer, Func<Task<WebSocket>> transport, Encoding encoding = null) :
            this(toSend, new SendBytes(transport), encoding) { }
        public SendString(ArraySegment<byte> buffer, Func<Task<WebSocket>> transport, Encoding encoding = null):
            this(null, buffer, transport, encoding){}
        public SendString(SendBytes sendBytes, Encoding encoding = null):
            this(null, sendBytes, encoding){}

        public async Task AwaitFlowControl() {
            SendBytes sb = SendBytes;
            sb.Bytes = Encoding.GetBytes(ToSend);
            await sb.AwaitFlowControl();
        }
    }
}
