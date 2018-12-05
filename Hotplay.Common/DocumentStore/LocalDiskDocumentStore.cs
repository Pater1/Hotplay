using Hotplay.Common.Extentions;
using Hotplay.Common.FileChain;
using Hotplay.Common.Streams;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hotplay.Common.DocumentStore {
    public class LocalDiskDocumentStore: IDocumentStore {
        private bool PurgeOnDispose { get; set; }
        private DirectoryInfo CacheDirectory { get; set; }


        private List<(string path, string key)> CachedFiles { get; set; } = new List<(string path, string key)>();
        private FileInfo CacheHeader { get; set; }
        private string CacheHeaderFilename { get; set; }
        private JsonSerializer Serializer { get; set; } = new JsonSerializer() {
            Formatting = Formatting.Indented,
        };

        //private Timer SaveTimer = null;
        public LocalDiskDocumentStore(DirectoryInfo cacheDirectory, string headerFilename = null, bool purgeOnDispose = false) {
            if(!cacheDirectory.Exists) {
                cacheDirectory.Create();
            }

            PurgeOnDispose = purgeOnDispose;
            CacheDirectory = cacheDirectory;

            if(!PurgeOnDispose) {
                CacheHeaderFilename = headerFilename == null ? $"{CACHE_HEADER_EXTENTION}" : $"{headerFilename}{CACHE_HEADER_EXTENTION}";

                CacheHeader = cacheDirectory.EnumerateFiles().Where(x => x.Name == CacheHeaderFilename).FirstOrDefault();
                if(CacheHeader == null) {
                    CacheHeader = new FileInfo(Path.Combine(cacheDirectory.FullName, CacheHeaderFilename));
                }

                if(CacheHeader.Exists) {
                    using(FileStream fs = CacheHeader.OpenRead()) {
                        using(StreamReader sr = new StreamReader(fs)) {
                            using(JsonReader jr = new JsonTextReader(sr)) {
                                CachedFiles = Serializer.Deserialize<List<(string path, string key)>>(jr);
                            }
                        }
                    }
                }
            }


            //SaveTimer = new Timer(
            //    e => {
            //        try {
            //            SaveHeader();
            //        } catch(Exception ex) {
            //            //TODO: Log Exception
            //        }
            //    },
            //    null,
            //    TimeSpan.FromMilliseconds(50),
            //    TimeSpan.FromMinutes(1));
            //DisposalAssistant._TrackForDisposal(SaveTimer);
            DisposalAssistant._TrackForDisposal(this);
        }

        const string CACHE_FILE_EXTENTION = ".cache", CACHE_HEADER_EXTENTION = ".cacheHeader";
        private string PathToFileName(string path) {
            byte[] encoded = Encoding.UTF32.GetBytes(path);
            List<int> divided = new List<int>();
            byte[] toEncode = new byte[4];
            for(int i = 0; i < encoded.Length; i++) {
                toEncode[i % 4] = encoded[i];
                if(i % 4 == 3) {
                    if(!BitConverter.IsLittleEndian) {
                        Array.Reverse(toEncode);
                    }
                    int tmp = BitConverter.ToInt32(toEncode);
                    divided.Add(tmp);
                    for(int j = 0; j < toEncode.Length; j++) {
                        toEncode[j] = 0;
                    }
                }
            }
            return $"{path.Length}_{path.Where(x => x == '/' || x == '\\').Count()}_{encoded.Where(x => x != 0).Count()}_{divided.Aggregate((x, y) => x ^ y)}_{divided.Sum()}{CACHE_FILE_EXTENTION}";
        }

        private IEnumerable<FileInfo> CacheFiles => CacheDirectory.EnumerateFiles().Where(x => x.Extension == CACHE_FILE_EXTENTION);

        private FileInfo FileForPath(string path, out string key) {
            key = PathToFileName(path);
            return FileForKey(key);
        }
        private FileInfo FileForPath(string path) {
            string s;
            return FileForPath(path, out s);
        }
        private FileInfo FileForKey(string key) {
            IEnumerable<FileInfo> infos = CacheDirectory.EnumerateFiles().ToArray();
            FileInfo has = infos.Where(x => x.Name == key).FirstOrDefault();
            if(has == null) {
                has = new FileInfo(Path.Combine(CacheDirectory.FullName, key));
            }
            return has;
        }

        public void Dispose() {
            if(PurgeOnDispose) {
                Purge().RunSync();
            } else {
                //SaveTimer.Dispose();
                SaveHeader();
            }
        }
        private int saveKey;
        private Random rand = new Random();
        private void SaveHeader() {
            int localSaveKey = rand.Next();
            saveKey = localSaveKey;
            ThreadPool.QueueUserWorkItem((x) => {
                lock(CachedFiles) {
                    if(saveKey != localSaveKey) return;
                    using(FileStream fs = CacheHeader.CreateOrTruncate()) {
                        using(StreamWriter sr = new StreamWriter(fs)) {
                            using(JsonWriter jr = new JsonTextWriter(sr)) {
                                Serializer.Serialize(jr, CachedFiles);
                            }
                        }
                    }
                }
            });
        }

        public Task<(bool success, Stream data)> TryGet(string name, bool dryRun = false) {
            FileInfo data = FileForPath(name, out string key);
            PushReadStream prs = null;
            if(data.Exists && !dryRun) {
                RequestLockManager.LockAndWait(key);
                prs = new PushReadStream(data.OpenRead());
                prs.OnDispose.Add(() => RequestLockManager.Unlock(key));
            }
            return Task.FromResult((data.Exists, (Stream)prs));
        }

        public async Task<bool> TryPut(string name, Stream data) {
            FileInfo info = FileForPath(name, out string key);
            RequestLockManager.LockAndWait(key);
            try {
                using(Stream writeTo = info.CreateOrTruncate()) {
                    await data.CopyToAsync(writeTo);
                }
            } finally {
                RequestLockManager.Unlock(key);
            }

            lock(CachedFiles) {
                if(!CachedFiles.Where(x => x.path == name).Any()) {
                    CachedFiles.Add((name, key));
                    SaveHeader();
                }
            }
            return true;
        }

        public Task Purge() {
            foreach(FileInfo file in CacheFiles) {
                file.Delete();
            }
            lock(CachedFiles) {
                if(CachedFiles.Any()) {
                    CachedFiles.Clear();
                    SaveHeader();
                }
            }
            return Task.CompletedTask;
        }

        public async Task Move(string from, string to) {
            (bool success, FileInfo fileFrom, FileInfo fileTo) = await CopyInternal(from, to);

            if(success) {
                await Remove(from);
            }
        }
        public async Task Copy(string from, string to) {
            await CopyInternal(from, to);
        }
        private async Task<(bool success, FileInfo fileFrom, FileInfo fileTo)> CopyInternal(string from, string to) {
            FileInfo fileFrom = FileForPath(from, out string fileFromName);
            FileInfo fileTo = FileForPath(to, out string fileToName);

            if(!fileFrom.Exists) return (false, fileFrom, fileTo);

            RequestLockManager.LockAndWait(fileToName);
            RequestLockManager.LockAndWait(fileFromName);
            try {
                using(Stream streamFrom = fileFrom.OpenRead()) {
                    using(Stream streamTo = fileTo.CreateOrTruncate()) {
                        await streamFrom.CopyToAsync(streamTo);
                    }
                }
            } finally {
                RequestLockManager.Unlock(fileFromName);
                RequestLockManager.Unlock(fileToName);
            }

            lock(CachedFiles) {
                if(!CachedFiles.Where(x => x.key == fileToName).Any()) {
                    CachedFiles.Add((to, fileToName));
                    SaveHeader();
                }
            }

            return (true, fileFrom, fileTo);
        }

        public Task Remove(string name) {
            FileInfo fileFrom = FileForPath(name, out string key);
            if(fileFrom.Exists) {
                RequestLockManager.LockAndWait(key);
                try {
                    fileFrom.Delete();
                } finally {
                    RequestLockManager.Unlock(key);
                }
            }

            lock(CachedFiles) {
                var v = CachedFiles.Where(x => x.path == name).Cast<(string path, string key)?>().FirstOrDefault();
                if(v.HasValue) {
                    CachedFiles.Remove(v.Value);
                    SaveHeader();
                }
            }

            return Task.CompletedTask;
        }
    }
}
