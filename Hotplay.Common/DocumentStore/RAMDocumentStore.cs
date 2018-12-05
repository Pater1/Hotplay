using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hotplay.Common.Helpers;

namespace Hotplay.Common.DocumentStore {
    public class RAMDocumentStore: IDocumentStore {
        private Dictionary<string, byte[]> Store { get; set; } = new Dictionary<string, byte[]>();
        public void Dispose() { }

        public Task Copy(string from, string to) {
            if(Store.ContainsKey(from)) {
                byte[] d = Store[from];
                Store.Add(to, d.ToArray()/*copy byte[]*/);
            }
            return Task.CompletedTask;
        }


        public Task Move(string from, string to) {
            if(Store.ContainsKey(from)) {
                byte[] d = Store[from];
                Store.Remove(from);
                Store.Add(to, d);
            }
            return Task.CompletedTask;
        }

        public Task Purge() {
            Store.Clear();
            return Task.CompletedTask;
        }

        public Task Remove(string key) {
            Store.Remove(key);
            return Task.CompletedTask;
        }

        public Task<(bool success, Stream data)> TryGet(string key, bool dryRun = false) {
            if(Store.ContainsKey(key)) {
                return Task.FromResult((true, (Stream)(dryRun ? null : new MemoryStream(Store[key]))));
            } else {
                return Task.FromResult((false, (Stream)null));
            }
        }

        public async Task<bool> TryPut(string key, Stream stream) {
            if(Store.ContainsKey(key)) {
                return true;
            }

            long length = 0;
            try {
                length = stream.Length;
            } catch(NotSupportedException) {
                length = -1;
            }

            //length known to be 0, therefore there's no data to store 
            //                      -OR-
            //file size > 2GB; Can't allocate byte[] that large
            if(length == 0 || length >= int.MaxValue) {
                return false;
            }

            byte[] data;
            if(stream is MemoryStream) {
                data = ((MemoryStream)stream).ToArray();
            } else {
                try {
                    using(MemoryStream mem = new MemoryStream()) {
                        await stream.CopyToAsync(mem);
                        data = mem.ToArray();
                    }
                } catch {
                    //a stream of unknown length just outran int.MaxValue
                    return false;
                }
            }
            Store.Add(key, data);

            return true;
        }
    }
}
