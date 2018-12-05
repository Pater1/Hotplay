using FubarDev.WebDavServer.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hotplay.Common.FileChain.ChainLinks.Network.Commands.CommandFlowControls.Receive {
    public class ReceiveBytesStream: Stream, IFlowControl<ReceiveBytesStream> {
        private static readonly IEnumerable<Stream> EMPTY_PARALLEL_WRITE_STREAMS = new Stream[0];
        private static Dictionary<List<byte>, bool> BufferPool = new Dictionary<List<byte>, bool>();//Key: ByteBuffer, Value: IsBusy

        public IDocument Document { get; set; }
        public ReceiveBytes<Stream> InternalReceive { get; set; }

        public FileOpenReadNetworkCommand Command { get; set; }

        private Func<Task<IEnumerable<Stream>>> ParallelWriteFactory { get; set; }
        private IEnumerable<Stream> ParallelWriteStreams { get; set; }
        private List<byte> ByteBuffer { get; set; }
        private long _internalWritePointer = 0;
        private long InternalWritePointer {
            get {
                lock(ParallelWriteFactory) {
                    return _internalWritePointer;
                }
            }
            set {
                lock(ParallelWriteFactory) {
                    _internalWritePointer = value;
                }
            }
        }
        private Semaphore OverReadLock { get; set; } = new Semaphore(1, 1);
        private Semaphore ByteBufferLock { get; set; } = new Semaphore(1, 1);
        public ReceiveBytesStream(IDocument doc, ReceiveBytes<Stream> internalReceive, Func<Task<IEnumerable<Stream>>> parallelWriteFactory = null) {
            if(parallelWriteFactory == null) {
                parallelWriteFactory = () => Task.FromResult(EMPTY_PARALLEL_WRITE_STREAMS);
            }
            ParallelWriteFactory = parallelWriteFactory;
            Document = doc;
            lock(BufferPool) {
                ByteBuffer = BufferPool.Where(x => !x.Value).Select(x => x.Key).FirstOrDefault();
                if(ByteBuffer == null) {
                    ByteBuffer = new List<byte>();
                    BufferPool.Add(ByteBuffer, true);
                }
            }
            InternalReceive = internalReceive;
        }

        public ICollection<Action> OnDispose { get; private set; } = new List<Action>();
        protected override void Dispose(bool b) {
            lock(BufferPool) {
                ByteBuffer.Clear();
                BufferPool[ByteBuffer] = false;
            }
            OverReadLock.Dispose();
            ByteBufferLock.Dispose();
            foreach(Stream s in ParallelWriteStreams) {
                s.Dispose();
            }
            foreach(Action a in OnDispose) {
                a();
            }
        }

        #region IFlowControl<ReceiveBytesStream>
        public ReceiveBytesStream Result => this;
        bool started = false;
        public async Task AwaitFlowControl() {
            if(!started) {
                ParallelWriteStreams = await ParallelWriteFactory();
                InternalReceive.Result = this;
                started = true;
            }
            //ThreadPool.QueueUserWorkItem(async (x) => {
                while(true) {
                    try {
                        await InternalReceive.AwaitFlowControl();
                        break;
                    } catch(Exception e) {
                        Command.RestartPointer = InternalWritePointer;
                        await Command.Retry();
                        InternalReceive = Command.ReceiveBytes;
                        InternalReceive.Result = this;
                    }
                }
            //});
        }
        #endregion

        #region Stream
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => Document.Length;

        public override long Position {
            get {
                return InternalWritePointer;
            }

            set {
                throw new NotImplementedException();
            }
        }

        public override void Flush() { }

        //public override void CopyTo(Stream destination, int bufferSize) {
        //    while(InternalWritePointer < Document.Length) {
        //        if(ByteBuffer.Count <= 0) {
        //            OverReadLock.WaitOne(0);//ensure locked
        //            OverReadLock.WaitOne();
        //        }

        //        try {
        //            ByteBufferLock.WaitOne();
        //            destination.Write(ByteBuffer.InternalArray, 0, ByteBuffer.Count);
        //            ByteBuffer.Clear();
        //        }finally{
        //            ByteBufferLock.Release();
        //        }
        //    }
        //}
        //public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) {
        //    byte[] buffer = new byte[bufferSize];
        //    while(InternalWritePointer < Document.Length || ByteBuffer.Count > 0) {
        //        //if(ByteBuffer.Count <= 0 && InternalWritePointer < Document.Length) {
        //        //    OverReadLock.WaitOne(0);//ensure locked
        //        //    OverReadLock.WaitOne();
        //        //}

        //        try {
        //            ByteBufferLock.WaitOne();
        //            int copy = buffer.Length > ByteBuffer.Count ? ByteBuffer.Count : buffer.Length;
        //            for(int i = 0; i < copy; i++){
        //                buffer[i] = ByteBuffer[i];
        //            }
        //            ByteBuffer.RemoveRange(0, copy);
        //            await destination.WriteAsync(buffer, 0, copy);
        //            //await destination.WriteAsync(ByteBuffer.ToArray(), 0, ByteBuffer.Count);
        //            //await destination.WriteAsync(ByteBuffer.InternalArray, 0, ByteBuffer.Count);
        //            ByteBuffer.Clear();
        //        } finally {
        //            ByteBufferLock.Release();
        //        }
        //    }
        //}

        public override int Read(byte[] buffer, int offset, int count) {
            //if(ByteBuffer.Count <= 0 && InternalWritePointer < Document.Length) {
            //    OverReadLock.WaitOne(0);//ensure locked
            //    OverReadLock.WaitOne();
            //}
            try {
                ByteBufferLock.WaitOne();
                if(count > ByteBuffer.Count) count = ByteBuffer.Count;
                for(int i = 0; i < count; i++){
                    buffer[i + offset] = ByteBuffer[i];
                }
                ByteBuffer.RemoveRange(0, count);
                return count;
            } finally {
                ByteBufferLock.Release();
            }
        }

        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotImplementedException();
        }

        public override void SetLength(long value) {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count) {
            if(count <= 0 || offset < 0 || buffer.Length <= 1 || offset > buffer.Length) return;

            try {
                ByteBufferLock.WaitOne();
                int size = ByteBuffer.Count;
                byte[] subBuffer = buffer.Skip(offset).Take(count).ToArray();
                ByteBuffer.AddRange(subBuffer);
                byte[] cloneBuffer = ByteBuffer.Skip(size).ToArray();
                bool equal = subBuffer.Length == cloneBuffer.Length;
                if(equal) {
                    for(int i = 0; i < subBuffer.Length; i++) {
                        equal = subBuffer[i] == cloneBuffer[i];
                        if(!equal) break;
                    }
                }
                string sub = Encoding.ASCII.GetString(subBuffer);
                string clone = Encoding.ASCII.GetString(cloneBuffer);
                //ByteBuffer.AddRange(buffer, offset, count);
                InternalWritePointer += count;
                //try {
                //    OverReadLock.Release();
                //} catch { }
            } finally {
                ByteBufferLock.Release();
            }

            foreach(Stream x in ParallelWriteStreams) {
                x.Write(buffer, offset, count);
            }
        }
        #endregion
    }
    //public class ReceiveBytesStream: Stream, IFlowControl<ReceiveBytesStream> {
    //    public IDocument Document { get; set; }
    //    public Func<Task<Stream>> InternalStreamFactory { get; set; }
    //    public ReceiveBytes<Stream> InternalReceive { get; set; }

    //    public FileOpenReadNetworkCommand Command { get; set; }

    //    private Stream InternalStream { get; set; }
    //    private Semaphore ReadLock { get; set; } = new Semaphore(1, 1);
    //    private Semaphore StreamLock { get; set; } = new Semaphore(1, 1);

    //    private long _internalReadPointer = 0;
    //    private object _internalReadLock = new object();
    //    private long InternalReadPointer {
    //        get {
    //            lock(_internalReadLock) {
    //                bool locked = _internalReadPointer < Document.Length && _internalReadPointer <= _internalWritePointer;
    //                if(locked) {
    //                    ReadLock.WaitOne(0);//ensure locked
    //                    ReadLock.WaitOne();
    //                }
    //                return _internalReadPointer;
    //            }
    //        }
    //        set {
    //            lock(_internalReadLock) {
    //                _internalReadPointer = value;
    //            }
    //        }
    //    }
    //    private long _internalWritePointer = 0;
    //    private object _internalWriteLock = new object();
    //    private long InternalWritePointer {
    //        get {
    //            lock(_internalWriteLock) {
    //                return _internalWritePointer;
    //            }
    //        }
    //        set {
    //            lock(_internalWriteLock) {
    //                _internalWritePointer = value;

    //                bool unlocked = value > _internalReadPointer || value >= Document.Length;
    //                if(unlocked) {
    //                    try {
    //                        ReadLock.Release();
    //                    } catch(SemaphoreFullException _) { }
    //                }
    //            }
    //        }
    //    }
    //    public ReceiveBytesStream(IDocument doc, ReceiveBytes<Stream> internalReceive, Func<Task<Stream>> internalStreamFactory = null) {
    //        if(internalStreamFactory == null) {
    //            internalStreamFactory = () => Task.FromResult((Stream)new MemoryStream());
    //        }

    //        Document = doc;
    //        InternalStreamFactory = internalStreamFactory;
    //        InternalReceive = internalReceive;
    //    }

    //    public ICollection<Action> OnDispose { get; private set; } = new List<Action>();
    //    public override void Close() {
    //        base.Close();
    //        InternalStream.Dispose();
    //        ReadLock.Dispose();
    //        StreamLock.Dispose();
    //        foreach(Action a in OnDispose) {
    //            a();
    //        }
    //    }

    //    #region IFlowControl<ReceiveBytesStream>
    //    public ReceiveBytesStream Result => this;
    //    bool started = false;
    //    public async Task AwaitFlowControl() {
    //        if(!started) {
    //            InternalStream = await InternalStreamFactory();
    //            InternalReceive.Result = this;
    //            started = true;
    //        }
    //        ThreadPool.QueueUserWorkItem(async (x) => {
    //            while(true) {
    //                try {
    //                    await InternalReceive.AwaitFlowControl();
    //                    break;
    //                } catch(Exception e) {
    //                    Command.RestartPointer = InternalWritePointer;
    //                    await Command.Retry();
    //                    InternalReceive = Command.ReceiveBytes;
    //                    InternalReceive.Result = this;
    //                }
    //            }
    //        });
    //    }
    //    #endregion

    //    #region Stream
    //    public override bool CanRead => InternalStream.CanRead;

    //    public override bool CanSeek => InternalStream.CanSeek;

    //    public override bool CanWrite => false;

    //    public override long Length => Document.Length;

    //    public override long Position {
    //        get {
    //            return InternalReadPointer;
    //        }

    //        set {
    //            InternalReadPointer = value;
    //        }
    //    }

    //    public override void Flush() {
    //        try {
    //            StreamLock.WaitOne();
    //            InternalStream.Flush();
    //        } finally {
    //            StreamLock.Release();
    //        }
    //    }

    //    public override int Read(byte[] buffer, int offset, int count) {
    //        int ret;
    //        try {
    //            ReadLock.WaitOne();
    //            long internalRead = InternalReadPointer;
    //            StreamLock.WaitOne();

    //            InternalStream.Position = internalRead;
    //            long delta = InternalWritePointer - internalRead;
    //            if(delta < count) count = (int)delta;

    //            ret = InternalStream.Read(buffer, offset, count);
    //        } finally {
    //            StreamLock.Release();
    //            try {
    //                ReadLock.Release();
    //            } catch(SemaphoreFullException _) { }
    //        }
    //        InternalReadPointer += ret;
    //        return ret;
    //    }

    //    public override long Seek(long offset, SeekOrigin origin) {
    //        throw new NotImplementedException();
    //    }

    //    public override void SetLength(long value) {
    //        throw new NotImplementedException();
    //    }

    //    public override void Write(byte[] buffer, int offset, int count) {
    //        try {
    //            StreamLock.WaitOne();
    //            InternalStream.Position = InternalWritePointer;
    //            InternalStream.Write(buffer, offset, count);
    //            InternalWritePointer += count;
    //        } finally {
    //            StreamLock.Release();
    //        }
    //    }
    //    #endregion
    //}
}