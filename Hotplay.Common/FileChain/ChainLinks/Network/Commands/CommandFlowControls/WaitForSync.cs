using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hotplay.Common.FileChain.ChainLinks.Network.Commands.CommandFlowControls {
    public class WaitForSync: IFlowControl {
        private Func<Task<WebSocket>> transport;

        public WaitForSync(Func<Task<WebSocket>> transport) {
            this.transport = transport;
            this.syncRead = new ArraySegment<byte>(new byte[1]);
        }

        private static ArraySegment<byte> syncBuffer = new ArraySegment<byte>(new byte[] { 126 });
        private ArraySegment<byte> syncRead;
        public async Task AwaitFlowControl() {
            WebSocket ws = await transport();
            await ws.SendAsync(syncBuffer, WebSocketMessageType.Binary, true, CancellationToken.None);
            WebSocketReceiveResult result;
            do {
                ws = await transport();
                result = await ws.ReceiveAsync(syncRead, CancellationToken.None);
                if(result.MessageType == WebSocketMessageType.Close){
                    await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure,
                        "received, preplied",
                        CancellationToken.None);
                    throw new Exception("Socket close signal received unexpectedly; in WaitForSync");
                }
            } while(!result.EndOfMessage);
        }
    }
}
