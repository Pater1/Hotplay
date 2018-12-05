using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Hotplay.Common.Helpers {
    public static class StreamHelpers {
        public static async Task<(bool success, byte[] data)> TryKnownLengthCopy(this Stream stream, int length){
            byte[] ret = new byte[length];
            int l = await stream.ReadAsync(ret, 0, length);
            return ((l==length, ret));
        }
        public static Task<(bool success, byte[] data)> TryReadToEnd(this System.IO.Stream stream) {
            long originalPosition = 0;

            if(stream.CanSeek) {
                originalPosition = stream.Position;
                stream.Position = 0;
            }

            try {
                byte[] readBuffer = new byte[4096];

                int totalBytesRead = 0;
                int bytesRead;

                while((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0) {
                    totalBytesRead += bytesRead;

                    if(totalBytesRead == readBuffer.Length) {
                        int nextByte = stream.ReadByte();
                        if(nextByte != -1) {
                            byte[] temp = new byte[readBuffer.Length * 2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }

                    //detect overflow
                    if(totalBytesRead > int.MaxValue || totalBytesRead < 0){
                        return Task.FromResult((false, (byte[])null));
                    }
                }

                byte[] data = readBuffer;
                if(readBuffer.Length != totalBytesRead) {
                    data = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, data, 0, totalBytesRead);
                }
                return Task.FromResult((true, data));
            } finally {
                if(stream.CanSeek) {
                    stream.Position = originalPosition;
                }
            }
        }
    }
}
