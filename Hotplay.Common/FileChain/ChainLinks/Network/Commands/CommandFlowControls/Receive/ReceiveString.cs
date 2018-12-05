using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Hotplay.Common.FileChain.ChainLinks.Network.Commands.CommandFlowControls.Receive {
    public class ReceiveString: IFlowControl<string> {
        public string Result { get; private set; }

        private ReceiveBytes ReceiveBytes { get; set; }
        private Encoding Encoding { get; set; }

        public ReceiveString() { }
        public ReceiveString(ArraySegment<byte> buffer, Func<Task<WebSocket>> transport, Encoding encoding = null) :
            this(new ReceiveBytes(transport), encoding) { }
        public ReceiveString(ReceiveBytes receiveBytes, Encoding encoding = null){
            ReceiveBytes = receiveBytes;
            Encoding = encoding != null ? encoding : Encoding.ASCII;
            Result = null;
        }

        public async Task AwaitFlowControl() {
            MemoryStream mem = await ReceiveBytes.AwaitFlowResult() as MemoryStream;
            byte[] raw = mem.ToArray();
            Result = Encoding.GetString(raw);
        }
    }
}
