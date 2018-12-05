using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Hotplay.Common.Predictors {
    public class MarkovChainDocumentPredictor: IDocumentPredictor {
        private PushRegister<string> Register { get; set; }
        private MarkovLink _currentLink;
        private MarkovLink CurrentLink {
            get {
                lock(linkLock) {
                    return _currentLink;
                }
            }
            set {
                lock(linkLock) {
                    _currentLink = value;
                }
            }
        }
        private MarkovLink StartingLink { get; set; }
        //private Timer SaveTimer { get; set; }
        private JsonSerializer Serializer { get; set; }
        private JsonSerializerSettings _serializerSettings;
        public JsonSerializerSettings SerializerSettings {
            get {
                return _serializerSettings;
            }
            set {
                _serializerSettings = value;
                Serializer = JsonSerializer.Create(_serializerSettings);
            }
        }
        private FileInfo SavedPredictor { get; set; }
        public MarkovChainDocumentPredictor(int registerLength, string saveName = null, string savePath = null) {
            if(savePath == null) {
                savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Hotplay");
            }
            if(saveName == null) {
                saveName = $"{nameof(MarkovChainDocumentPredictor)}.json";
            }
            DirectoryInfo sf = new DirectoryInfo(savePath);
            if(!sf.Exists) {
                sf.Create();
            }
            savePath = Path.Combine(savePath, saveName);

            Register = new PushRegister<string>(registerLength);

            SerializerSettings = new JsonSerializerSettings() {
                TypeNameHandling = TypeNameHandling.All,
                Formatting = Formatting.Indented
            };

            SavedPredictor = new FileInfo(savePath);
            if(SavedPredictor.Exists) {
                try {
                    using(JsonReader reader = new JsonTextReader(new StreamReader(SavedPredictor.OpenRead()))) {
                        var v = Serializer.Deserialize<(string[] startLink, string[] curLink, List<(string[] Key, (string[] pr, (string mapKey, (string[] link, ulong count) mapValue)[] map, string[] tagAlong) Link)> links)>(reader);
                        MarkovLink._Links = v.links.ToDictionary(x => x.Key.ToPushRegister(), x => MarkovLink.Deserialize(x.Link));
                        StartingLink = MarkovLink._Links[v.startLink.ToPushRegister()];
                        CurrentLink = MarkovLink._Links[v.curLink.ToPushRegister()];
                    }
                } catch(Exception ex) {
                    StartingLink = new MarkovLink(Register);
                }
            } else {
                StartingLink = new MarkovLink(Register);
            }
            CurrentLink = StartingLink;
            //SaveTimer = new Timer(
            //    e => {
            //        try {
            //            using(StreamWriter reader = new StreamWriter(savedPredictor.CreateOrTruncate())) {
            //                Serializer.Serialize(reader, (StartingLink.Key.ToArray(), CurrentLink.Key.ToArray(), MarkovLink._Links.Select(x => (x.Key.ToArray(), x.Value.Serialize())).ToList()));
            //            }
            //        } catch(Exception ex) {
            //            //TODO: Log Exception
            //        }
            //    },
            //    null,
            //    TimeSpan.FromMilliseconds(50),
            //    TimeSpan.FromMinutes(1));
            //DisposalAssistant._TrackForDisposal(SaveTimer);
        }

        private int saveKey;
        private Random rand = new Random();
        private void SaveMarkovChain() {
            //int localSaveKey = rand.Next();
            //saveKey = localSaveKey;
            //ThreadPool.QueueUserWorkItem((q) => {
            //    lock(MarkovLink._Links) {
            //        lock(linkLock) {
            //            if(saveKey != localSaveKey) return;
            using(StreamWriter reader = new StreamWriter(SavedPredictor.CreateOrTruncate())) {
                Serializer.Serialize(reader, (StartingLink.Key.ToArray(), CurrentLink.Key.ToArray(), MarkovLink._Links.Select(x => (x.Key.ToArray(), x.Value.Serialize())).ToList()));
            }
            //        }
            //    }
            //});
        }


        private MarkovLink nextLink = null;
        private TimeSpan tagalongTimeout = TimeSpan.FromSeconds(5), longestInOutDelay;
        private DateTime tagalongDateout, lastRequestIn, lastRequestOut;
        private object linkLock = new object();
        public Task Request(string docPath) {
            lastRequestIn = DateTime.Now;

            lock(linkLock) {
                if(DateTime.Now > tagalongDateout && tagalongDateout != default && nextLink != null) {
                    _currentLink = nextLink;
                    nextLink = null;
                    SaveMarkovChain();
                }

                if(nextLink == null) {
                    Register.Push(docPath);
                    nextLink = CurrentLink.Next(Register, docPath);
                } else {
                    _currentLink.TagAlongs.Add(docPath);
                }
            }

            TimeSpan delta = lastRequestOut - lastRequestIn;
            if(delta > TimeSpan.FromTicks(1)) {
                if(delta > longestInOutDelay || longestInOutDelay == default) {
                    longestInOutDelay = delta;
                }
                tagalongDateout = DateTime.Now + longestInOutDelay;
            } else {
                tagalongDateout = DateTime.Now + tagalongTimeout;
            }

            return Task.CompletedTask;
        }
        public Task Request_Bubbleup(string docPath) {
            tagalongDateout = DateTime.Now + tagalongTimeout;
            lastRequestOut = DateTime.Now;
            return Task.CompletedTask;
        }
        //private async Task TimeoutTagalongsAdd() {
        //    try {
        //        DateTime cur;
        //        while((cur = DateTime.Now) < tagalongDateout) {
        //            decrementLock.WaitOne();
        //            await Task.Delay(tagalongDateout - cur);
        //        }
        //        lock(linkLock) {
        //            _currentLink = nextLink;
        //            nextLink = null;
        //        }
        //        SaveMarkovChain();
        //    }finally{
        //        decrementLock.Release();
        //    }
        //}

        public Task<IEnumerable<string>> GetLikelyhoodSortedDocuments() {
            lock(MarkovLink._Links) {
                return Task.FromResult(
                    (IEnumerable<string>)
                    CurrentLink.TagAlongs.Select(y => (y, float.MaxValue))
                    .Concat(
                        MarkovLink._Links.SelectMany(x => {
                            lock(x.Value.Map) {
                                float similarity = x.Key.Similarity(CurrentLink.Key);
                                return x.Value.Map.Select(y => (y.Key, y.Value.count * similarity)).ToArray();
                            }
                        })
                    )
                    .GroupBy(x => x.Item1)
                    .Select(x => {
                        return (x.Key, x.Select(y => y.Item2).Max());
                    })
                    .OrderByDescending(y => y.Item2)
                    .Select(x => x.Item1)
                    .Distinct()
                    .ToArray()
                );
            }
        }

        [System.Serializable]
        private class MarkovLink {
            public static Dictionary<PushRegister<string>, MarkovLink> _Links { get; set; } = new Dictionary<PushRegister<string>, MarkovLink>();

            public static MarkovLink Deserialize((string[] pr, (string mapKey, (string[] link, ulong count) mapValue)[] map, string[] tagAlong) serialized) {
                MarkovLink ret = new MarkovLink(serialized.pr.ToPushRegister()) {
                    Map = serialized.map.ToDictionary(x => x.mapKey, x => (x.mapValue.link.ToPushRegister(), x.mapValue.count)),
                    TagAlongs = serialized.tagAlong.ToList()
                };
                return ret;
            }
            public (string[] pr, (string mapKey, (string[] link, ulong count) mapValue)[] map, string[] tagAlong) Serialize() {
                return (Key.ToArray(), Map.Select(x => (x.Key, (x.Value.link.ToArray(), x.Value.count))).ToArray(), TagAlongs.Distinct().ToArray());
            }

            public List<string> TagAlongs { get; set; } = new List<string>();

            private PushRegister<string> key;
            public PushRegister<string> Key {
                get {
                    return key;
                }
                set {
                    lock(_Links) {
                        if(key != null && _Links.ContainsKey(key)) {
                            _Links.Remove(key);
                        }
                        if(_Links.ContainsKey(value)) {
                            _Links.Remove(value);
                        }
                        key = value;
                        _Links.Add(key, this);
                    }
                }
            }
            public Dictionary<string, (PushRegister<string> link, ulong count)> Map { get; set; }
            public MarkovLink() { }
            public MarkovLink(PushRegister<string> register) {
                Key = register.Clone;
                Map = new Dictionary<string, (PushRegister<string> link, ulong count)>();
            }

            public MarkovLink Next(PushRegister<string> reg, string next) {
                lock(Map) {
                    if(Map.ContainsKey(next)) {
                        var v = Map[next];
                        v.count++;
                        Map[next] = v;
                        return _Links[v.link];
                    } else {
                        MarkovLink ret = new MarkovLink(reg);
                        var v = (ret.Key, (ulong)1);
                        Map.Add(next, v);
                        return ret;
                    }
                }
            }
        }
    }
}
