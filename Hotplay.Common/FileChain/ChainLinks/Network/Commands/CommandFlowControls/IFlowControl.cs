using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Hotplay.Common.FileChain.ChainLinks.Network.Commands.CommandFlowControls {
    public interface IFlowControl {
        Task AwaitFlowControl();
    }
    public interface IFlowControl<T>: IFlowControl {
        T Result { get; }
    }
    public static class FlowControlExtentions {
        public static async Task<T> AwaitFlowResult<T>(this IFlowControl<T> control) {
            await control.AwaitFlowControl();
            return control.Result;
        }
    }
}
