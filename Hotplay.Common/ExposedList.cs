using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hotplay.Common {
    public class ExposedList<T>: IList<T> {
        public T[] _internalArray;
        public T[] InternalArray {
            get {
                return _internalArray;
            }
            private set {
                _internalArray = value;
            }
        }
        public int Capacity {
            get {
                return InternalArray.Length;
            }
        }

        public ExposedList(int startingCapacity = 16) {
            InternalArray = new T[startingCapacity];
        }

        public int Count { get; private set; } = 0;

        public bool IsReadOnly => false;

        public T this[int index] {
            get {
                return InternalArray[index];
            }

            set {
                InternalArray[index] = value;
            }
        }

        public int IndexOf(T item) {
            return Array.IndexOf(InternalArray, item);
        }

        public void Insert(int index, T item) {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index) {
            throw new NotImplementedException();
        }

        public void Add(T item) {
            throw new NotImplementedException();
        }

        public bool Remove(T item) {
            throw new NotImplementedException();
        }

        public void AddRange(T[] item, int offset = 0, int count = -1) {
            if(item.Length <= 0) return;

            if(count < 0) {
                count = item.Length;
            }
            if(offset < 0) {
                offset = 0;
            }
            if((count + offset) > item.Length){
                count -= offset;
            }

            if((Count + count) >= Capacity) {
                DoubleCapacity(Count + count);
            }
            for(int i = offset; i < count; i++) {
                InternalArray[i + Count - offset] = item[i];
            }
            Count += count;
        }

        public void RemoveRange(int start, int count) {
            for(int i = start; i < count; i++) {
                if((i + count) < Count) {
                    InternalArray[i] = InternalArray[i + count];
                }
            };
            Count -= count;
            if(Count < 0) Count = 0;
        }

        public void Clear() {
            Count = 0;
        }

        public bool Contains(T item) {
            return InternalArray.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex = 0) {
            Array.Copy(InternalArray, array, (array.Length < Count ? array.Length : Count));
        }
        private void DoubleCapacity(int min) {
            int cur = Capacity;
            do {
                cur <<= 1;
            } while(cur < min);

            Array.Resize(ref _internalArray, cur);
        }

        public IEnumerator<T> GetEnumerator() {
            for(int i = 0; i < Count; i++) {
                yield return InternalArray[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return InternalArray.GetEnumerator();
        }
    }
}
