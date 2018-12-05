using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Hotplay.Common.FileChain.ChainLinks.Network.Commands.CommandFlowControls.Send {
    public class SendEnum<T>: IFlowControl where T : Enum {
        public T ToSend { get; set; }
        private SendString SendString { get; set; }

        public SendEnum(T toSend, SendString sendString) {
            ToSend = toSend;
            SendString = sendString;
        }
        public SendEnum(T toSend, SendBytes sendBytes, Encoding encoding = null) :
            this(toSend, new SendString(sendBytes, encoding)) { }
        public SendEnum(T toSend, ArraySegment<byte> buffer, Func<Task<WebSocket>> transport, Encoding encoding = null) :
            this(toSend, new SendString(new SendBytes(transport), encoding)) { }

        public async Task AwaitFlowControl() {
            string send = ToSend.ToString();
            SendString ss = SendString;
            ss.ToSend = send;
            await ss.AwaitFlowControl();
        }
    }
}
