using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hotplay.Common {
    [System.Serializable]
    public sealed class PushRegister<T>: IEnumerable<T> {
        private int capacity;
        [Newtonsoft.Json.JsonProperty("Capacity")]
        public int Capacity {
            get {
                return capacity;
            }
            private set {
                if(capacity > value) {
                    for(int i = value; i < capacity; i++) {
                        List.RemoveAt(0);
                    }
                }
                capacity = value;
            }
        }
        [Newtonsoft.Json.JsonProperty("Collection")]
        private IList<T> List { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        private object ListLock = new object();

        [Newtonsoft.Json.JsonIgnore]
        public int Count {
            get {
                lock(ListLock) {
                    return List.Count();
                }
            }
        }

        public int IndexOf(T what) {
            lock(ListLock) {
                return List.IndexOf(what);
            }
        }
        
        public PushRegister(int capacity) {
            Capacity = capacity;
            List = new List<T>();
        }
        public PushRegister() {}

        public void Push(T push) {
            lock(ListLock) {
                List.Add(push);
                if(List.Count > Capacity) {
                    List.RemoveAt(0);
                }
            }
        }

        public IEnumerator<T> GetEnumerator() => List.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => List.GetEnumerator();

        public float Similarity(PushRegister<T> ts) {
            float aggregator = 0;

            int aLength = this.Count, bLength = ts.Count, maxLength = aLength > bLength? aLength: bLength;
        
            IEnumerator<T> a = this.GetEnumerator(), b = ts.GetEnumerator();
            while(a.MoveNext() && b.MoveNext()){
                if(a.Current.Equals(b.Current)){
                    aggregator += 1;
                }else if(ts.Contains(a.Current)){
                    int aIndex = this.IndexOf(a.Current), bIndex = ts.IndexOf(b.Current);
                    int delta = Math.Abs(aIndex - bIndex);
                    float proximity = 1 - (delta / (float)maxLength);
                    aggregator += proximity;
                }
            }

            aggregator /= maxLength;

            return aggregator;
        }
        public PushRegister<T> Clone {
            get{
                PushRegister<T> ret = new PushRegister<T>(Capacity);
                lock(ListLock) {
                    foreach(T t in this) {
                        ret.Push(t);
                    }
                }
                return ret;
            }
        }

        public override bool Equals(object obj) {
            PushRegister<T> prt = obj as PushRegister<T>;
            if(prt == null) return false;

            if(Count != prt.Count) return false;

            IEnumerator<T> thisEnum = GetEnumerator(), otherEnum = prt.GetEnumerator();
            while(thisEnum.MoveNext() && otherEnum.MoveNext()){
                if(!thisEnum.Current.Equals(otherEnum.Current)) return false;
            }

            return true;
        }

        public override int GetHashCode() {
            if(!List.Any()) {
                return 0;
            }
            return List.Select(x => x.GetHashCode()).Aggregate((x, y) => x ^ y);
        }

        public override string ToString() {
            if(!List.Any()){
                return "";
            }
            return "[" + List.Select(x => x.ToString()).Aggregate((x, y) => x + ", " + y) + "]";
        }
    }

    public static class PushRegisterExtentions{
        //public static float FullSimilarity(this PushRegister<string> a, PushRegister<string> b){
            
        //}
        public static PushRegister<T> ToPushRegister<T>(this IEnumerable<T> source, int capacity = -1){
            if(capacity < 0){
                capacity = source.Count();
            }
            PushRegister<T> ret = new PushRegister<T>(capacity);
            foreach(T t in source){
                ret.Push(t);
            }
            return ret;
        }
    }
}
