using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Hotplay.Common.FileChain.ChainLinks.Network.Commands.CommandFlowControls.CommonNumberTransport;

namespace Hotplay.Common.FileChain.ChainLinks.Network.Commands.CommandFlowControls.Receive {
    public class ReceiveNumber: IFlowControl<object> {
        public object Result { get; private set; }

        private ReceiveBytes ReceiveBytes { get; set; }
        public ReceiveNumber(ReceiveBytes receiveBytes) {
            ReceiveBytes = receiveBytes;
        }

        public async Task AwaitFlowControl() {
            byte[] raw = (await ReceiveBytes.AwaitFlowResult() as MemoryStream).ToArray();
            NumberType numberType = (NumberType)raw[0];
            byte[] encoded = raw.Skip(1).ToArray();
            Result = numberType.DecodeNumber(encoded);
        }
    }
}
