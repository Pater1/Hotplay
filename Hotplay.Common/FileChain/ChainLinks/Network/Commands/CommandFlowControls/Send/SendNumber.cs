using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Hotplay.Common.FileChain.ChainLinks.Network.Commands.CommandFlowControls.CommonNumberTransport;

namespace Hotplay.Common.FileChain.ChainLinks.Network.Commands.CommandFlowControls.Send {
    public class SendNumber: IFlowControl {
        public SendNumber() { }
        public SendNumber(object number, SendBytes sendBytes):this() {
            Number = number;
            SendBytes = sendBytes;
        }
        public SendNumber(SendBytes sendBytes) 
            : this(null, sendBytes) { }

        private SendBytes SendBytes{ get; set; }
        private byte[] ToSend{ get; set; }
        private NumberType NumberType{ get; set; }
        public object Number{
            get{
                return NumberType.DecodeNumber(ToSend);
            }
            set{
                (NumberType numberType, byte[] encoded) = EncodeNumber(value);
                NumberType = numberType;
                ToSend = encoded;
            }
        }

        public async Task AwaitFlowControl() {
            SendBytes sb = SendBytes;
            sb.Bytes = ToSend.Prepend((byte)NumberType).ToArray();
            await sb.AwaitFlowControl();
        }
    }
}
