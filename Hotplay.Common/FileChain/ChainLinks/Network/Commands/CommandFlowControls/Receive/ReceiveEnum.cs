using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Hotplay.Common.FileChain.ChainLinks.Network.Commands.CommandFlowControls.Receive {
    public class ReceiveEnum<T>: IFlowControl<T> where T : Enum {
        public T Result { get; private set; }

        private ReceiveString ReceiveString{ get; set; }
        public ReceiveEnum(ReceiveString receiveString) {
            ReceiveString = receiveString;
            Result = default;
        }

        public async Task AwaitFlowControl() {
            string raw = await ReceiveString.AwaitFlowResult();
            Result = (T)Enum.Parse(typeof(T), raw);
        }
    }
}
