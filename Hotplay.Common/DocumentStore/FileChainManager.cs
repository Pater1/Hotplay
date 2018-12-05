using Hotplay.Common.DocumentStore;
using Hotplay.Common.FileChain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hotplay.Common.DocumentStore {
    public static class FileChainManager {
        private static Dictionary<string, IFileChain> Store { get; set; } = new Dictionary<string, IFileChain>();

        public static T RequestTrackedChain<T>(string trackingKey) where T : IFileChain, new() {
            if(Store.ContainsKey(trackingKey)) {
                return (T)Store[trackingKey];
            }
            
            return RequestTrackedChain<T>(trackingKey, () => new T());
        }
        public static T RequestTrackedChain<T>(string trackingKey, Func<T> factory) where T : IFileChain {
            if(Store.ContainsKey(trackingKey)) {
                return (T)Store[trackingKey];
            }

            T t = factory();
            if(!string.IsNullOrWhiteSpace(trackingKey)) {
                try {
                    Store.Add(trackingKey, t);
                } catch { }
            }
            return t;
        }
    }
}
