using Hotplay.Common.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hotplay.Common.Streams {
    public class PushReadStream: Stream, IDelayedDisposable {
        private Stream BaseStream { get; set; }
        public PushReadStream(Stream baseStream) {
            BaseStream = baseStream;
        }

        public override bool CanRead => BaseStream.CanRead;

        public override bool CanSeek => BaseStream.CanSeek;

        public override bool CanWrite => BaseStream.CanWrite;

        public override long Length => BaseStream.Length;


        public override bool CanTimeout => BaseStream.CanTimeout;
        public override int ReadTimeout => BaseStream.ReadTimeout;
        public override int WriteTimeout => BaseStream.WriteTimeout;

        public override long Position {
            get {
                return BaseStream.Position;
            }

            set {
                BaseStream.Position = value;
            }
        }

        public override void Flush() {
            if(BaseStream != null) BaseStream.Flush();
        }

        private List<IPushStreamListener> multiReadActions = new List<IPushStreamListener>();
        public void AddListener(IPushStreamListener pushOnRead) {
            multiReadActions.Add(pushOnRead);
        }
        public override int Read(byte[] buffer, int offset, int count) {
            long start = Position + offset;
            int lengthRead = BaseStream.Read(buffer, offset, count);
            foreach(var pushOnRead in multiReadActions) {
                pushOnRead.PushRead(buffer, start, start + lengthRead);
            }
            return lengthRead;
        }

        public override long Seek(long offset, SeekOrigin origin) => BaseStream.Seek(offset, origin);

        public override void SetLength(long value) => BaseStream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) {
            long start = Position + offset;
            BaseStream.Write(buffer, offset, count);
            foreach(var pushOnWrite in multiReadActions) {
                pushOnWrite.PushWrite(buffer, start, start + count);
            }
        }

        public override bool Equals(object obj) {
            return BaseStream.Equals(obj);
        }

        public override int GetHashCode() {
            return BaseStream.GetHashCode();
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) {
            return BaseStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) {
            return BaseStream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void CopyTo(Stream destination, int bufferSize) {
            BaseStream.CopyTo(destination, bufferSize);
        }

        //READS HERE & WRITES HERE...?
        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) {
            Position = 0;
            byte[] buffer = new byte[bufferSize];
            int read = 0;
            while((read = this.Read(buffer, 0, buffer.Length)) > 0) {
                await destination.WriteAsync(buffer, 0, read);
            }
        }

        public override int EndRead(IAsyncResult asyncResult) {
            return BaseStream.EndRead(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult) {
            BaseStream.EndWrite(asyncResult);
        }

        public override Task FlushAsync(CancellationToken cancellationToken) {
            return BaseStream.FlushAsync(cancellationToken);
        }

        public override int Read(Span<byte> buffer) {
            return BaseStream.Read(buffer);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            return BaseStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken)) {
            return BaseStream.ReadAsync(buffer, cancellationToken);
        }

        public override int ReadByte() {
            return BaseStream.ReadByte();
        }

        public override void Write(ReadOnlySpan<byte> buffer) {
            BaseStream.Write(buffer);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            return BaseStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken)) {
            return BaseStream.WriteAsync(buffer, cancellationToken);
        }

        public override void WriteByte(byte value) {
            BaseStream.WriteByte(value);
        }


        public ICollection<Action> OnDispose { get; } = new List<Action>();

        public ICollection<Func<Task>> OnDisposeAsync { get; } = new List<Func<Task>>();
        public void DisposeInternal() {
            foreach(var pushOnWrite in multiReadActions) {
                pushOnWrite.Dispose();
            }
            Flush();
            BaseStream.Close();
        }

        protected override void Dispose(bool disposing) => this.DefaultDelayedDispose();
        public override void Close() => this.DefaultDelayedDispose();

        //protected override void Dispose(bool disposing) {
        //    BaseStream.Dispose();
        //}

        //public override void Close() {
        //    foreach(var pushOnWrite in multiReadActions) {
        //        pushOnWrite.Dispose();
        //    }
        //    Flush();
        //    BaseStream.Close();
        //}
    }
}
