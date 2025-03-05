using System;
using System.Collections.Generic;
using System.Text;

namespace Hotplay.Common {
    public class DisposalAssistant: IDisposable {
        private static DisposalAssistant _DisposalAssistent = null;
        public static DisposalAssistant StaticDisposalAssistent{
            get{
                if(_DisposalAssistent == null){
                    _DisposalAssistent = new DisposalAssistant();
                }
                return _DisposalAssistent;
            }
        }
        public static T _TrackForDisposal<T>(T toTrack) where T : IDisposable {
            return StaticDisposalAssistent.TrackForDisposal(toTrack);
        }

        private DisposalAssistant() { }

        private List<IDisposable> toDispose = new List<IDisposable>();
        public T TrackForDisposal<T>(T toTrack) where T : IDisposable {
            toDispose.Add(toTrack);
            return toTrack;
        }

        public void Dispose() {
            foreach(IDisposable disp in toDispose){
                try {
                    disp.Dispose();
                }catch{ }
            }
        }
    }
}
