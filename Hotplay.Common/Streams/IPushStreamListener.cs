using System;
using System.Collections.Generic;
using System.Text;

namespace Hotplay.Common.Streams {
    public interface IPushStreamListener: IDisposable {
        PushReadStream Stream { get; }
        void PushRead(byte[] buffer, long pushStartIndex, long pushEndIndex);
        void PushWrite(byte[] buffer, long pushStartIndex, long pushEndIndex);
    }
}
