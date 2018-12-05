using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FubarDev.WebDavServer.FileSystem;
using Hotplay.Common.FileChain.Structure;
using Hotplay.Common.Helpers;

namespace Hotplay.Common.FileChain.ChainLinks.Cache {
    public static class CacheManager {
        private static List<(Func<Task<float>>, Func<Task>)> GetManagedActions(string key, Dictionary<string, List<(Func<Task<float>>, Func<Task>)>> inDict, bool remove = false) {
            List<(Func<Task<float>>, Func<Task>)> ret;
            if(inDict.ContainsKey(key)){
                ret = inDict[key];
            }else{
                ret = new List<(Func<Task<float>>, Func<Task>)>();
                inDict.Add(key, ret);
            }

            if(remove){
                inDict.Remove(key);
            }

            return ret;
        }

        private static string GenerateRequestKey(IEntry onEntry) =>
            $"{(onEntry is ICollection? nameof(ICollection): nameof(IDocument))} {onEntry.FullPath()}";
            
        private static Dictionary<string, List<(Func<Task<float>>, Func<Task>)>> CreateDocumentAsyncManagedActions = new Dictionary<string, List<(Func<Task<float>>, Func<Task>)>>();
        public static Task CreateDocumentAsync(ICollection entry, Func<Task<float>> fitness, Func<Task> managedCallback) {
            string key = GenerateRequestKey(entry);
            GetManagedActions(key, CreateDocumentAsyncManagedActions)?.Add((fitness, managedCallback));
            return Task.CompletedTask;
        }
        public static async Task Flush_CreateDocumentAsync(IEntry entry) {
            string key = GenerateRequestKey(entry);
            List<(Func<Task<float>>, Func<Task>)> toDo = GetManagedActions(key, CreateDocumentAsyncManagedActions, true);

            if(toDo == null) return;

            await Task.WhenAll(toDo.Select(x => x.Item2()));
        }
        public static Task Ignore_CreateDocumentAsync(IEntry entry) {
            string key = GenerateRequestKey(entry);
            if(CreateDocumentAsyncManagedActions.ContainsKey(key)){
                CreateDocumentAsyncManagedActions[key] = null;
            } else{
                CreateDocumentAsyncManagedActions.Add(key, null);
            }
            return Task.CompletedTask;
        }

        private static Dictionary<string, List<(Func<Task<float>>, Func<Task>)>> CreateCollectionAsyncManagedActions = new Dictionary<string, List<(Func<Task<float>>, Func<Task>)>>();
        public static Task CreateCollectionAsync(ICollection entry, Func<Task<float>> fitness, Func<Task> managedCallback) {
            string key = GenerateRequestKey(entry);
            GetManagedActions(key, CreateCollectionAsyncManagedActions)?.Add((fitness, managedCallback));
            return Task.CompletedTask;
        }
        public static async Task Flush_CreateCollectionAsync(IEntry entry) {
            string key = GenerateRequestKey(entry);
            List<(Func<Task<float>>, Func<Task>)> toDo = GetManagedActions(key, CreateCollectionAsyncManagedActions, true);

            if(toDo == null) return;

            await Task.WhenAll(toDo.Select(x => x.Item2()));
        }
        public static Task Ignore_CreateCollectionAsync(IEntry entry) {
            string key = GenerateRequestKey(entry);
            if(CreateCollectionAsyncManagedActions.ContainsKey(key)) {
                CreateCollectionAsyncManagedActions[key] = null;
            } else {
                CreateCollectionAsyncManagedActions.Add(key, null);
            }
            return Task.CompletedTask;
        }

        private static Dictionary<string, List<(Func<Task<float>>, Func<Task>)>> OpenReadAsyncManagedActions = new Dictionary<string, List<(Func<Task<float>>, Func<Task>)>>();
        public static Task OpenReadAsync(IDocument entry, Func<Task<float>> fitness, Func<Task> managedCallback) {
            string key = GenerateRequestKey(entry);
            GetManagedActions(key, OpenReadAsyncManagedActions)?.Add((fitness, managedCallback));
            return Task.CompletedTask;
        }
        public static async Task Flush_OpenReadAsync(IEntry entry) {
            string key = GenerateRequestKey(entry);
            List<(Func<Task<float>>, Func<Task>)> toDo = GetManagedActions(key, OpenReadAsyncManagedActions, true);

            if(toDo == null) return;

            if(toDo.Any()) {
                await toDo  .OrderByDescending(x => x.Item1().Result)
                            .ThenByDescending(x => toDo.IndexOf(x))
                            .First()
                            .Item2
                            ();
            }
        }
        public static Task Ignore_OpenReadAsync(IEntry entry) {
            string key = GenerateRequestKey(entry);
            if(OpenReadAsyncManagedActions.ContainsKey(key)) {
                OpenReadAsyncManagedActions[key] = null;
            } else {
                OpenReadAsyncManagedActions.Add(key, null);
            }
            return Task.CompletedTask;
        }

        private static Dictionary<string, List<(Func<Task<float>>, Func<Task>)>> MoveToAsyncManagedActions = new Dictionary<string, List<(Func<Task<float>>, Func<Task>)>>();
        public static Task MoveToAsync(IDocument entry, Func<Task<float>> fitness, Func<Task> managedCallback) {
            string key = GenerateRequestKey(entry);
            GetManagedActions(key, MoveToAsyncManagedActions)?.Add((fitness, managedCallback));
            return Task.CompletedTask;
        }
        public static async Task Flush_MoveToAsync(IEntry entry) {
            string key = GenerateRequestKey(entry);
            List<(Func<Task<float>>, Func<Task>)> toDo = GetManagedActions(key, MoveToAsyncManagedActions, true);

            if(toDo == null) return;

            await Task.WhenAll(toDo.Select(x => x.Item2()));
        }
        public static Task Ignore_MoveToAsync(IEntry entry) {
            string key = GenerateRequestKey(entry);
            if(MoveToAsyncManagedActions.ContainsKey(key)) {
                MoveToAsyncManagedActions[key] = null;
            } else {
                MoveToAsyncManagedActions.Add(key, null);
            }
            return Task.CompletedTask;
        }

        private static Dictionary<string, List<(Func<Task<float>>, Func<Task>)>> DeleteAsyncManagedActions = new Dictionary<string, List<(Func<Task<float>>, Func<Task>)>>();
        public static Task DeleteAsync(IDocument entry, Func<Task<float>> fitness, Func<Task> managedCallback) {
            string key = GenerateRequestKey(entry);
            GetManagedActions(key, DeleteAsyncManagedActions)?.Add((fitness, managedCallback));
            return Task.CompletedTask;
        }
        public static Task DeleteAsync(ICollection entry, Func<Task<float>> fitness, Func<Task> managedCallback) {
            string key = GenerateRequestKey(entry);
            GetManagedActions(key, DeleteAsyncManagedActions)?.Add((fitness, managedCallback));
            return Task.CompletedTask;
        }
        public static async Task Flush_DeleteAsync(IEntry entry) {
            string key = GenerateRequestKey(entry);
            List<(Func<Task<float>>, Func<Task>)> toDo = GetManagedActions(key, DeleteAsyncManagedActions, true);

            if(toDo == null) return;

            await Task.WhenAll(toDo.Select(x => x.Item2()));
        }
        public static Task Ignore_DeleteAsync(IEntry entry) {
            string key = GenerateRequestKey(entry);
            if(DeleteAsyncManagedActions.ContainsKey(key)) {
                DeleteAsyncManagedActions[key] = null;
            } else {
                DeleteAsyncManagedActions.Add(key, null);
            }
            return Task.CompletedTask;
        }

        private static Dictionary<string, List<(Func<Task<float>>, Func<Task>)>> CreateAsyncManagedActions = new Dictionary<string, List<(Func<Task<float>>, Func<Task>)>>();
        public static Task CreateAsync(IDocument entry, Func<Task<float>> fitness, Func<Task> managedCallback) {
            string key = GenerateRequestKey(entry);
            GetManagedActions(key, CreateAsyncManagedActions)?.Add((fitness, managedCallback));
            return Task.CompletedTask;
        }
        public static async Task Flush_CreateAsync(IEntry entry) {
            string key = GenerateRequestKey(entry);
            List<(Func<Task<float>>, Func<Task>)> toDo = GetManagedActions(key, CreateAsyncManagedActions, true);

            if(toDo == null) return;

            await Task.WhenAll(toDo.Select(x => x.Item2()));
        }
        public static Task Ignore_CreateAsync(IEntry entry) {
            string key = GenerateRequestKey(entry);
            if(CreateAsyncManagedActions.ContainsKey(key)) {
                CreateAsyncManagedActions[key] = null;
            } else {
                CreateAsyncManagedActions.Add(key, null);
            }
            return Task.CompletedTask;
        }

        private static Dictionary<string, List<(Func<Task<float>>, Func<Task>)>> CopyToAsyncManagedActions = new Dictionary<string, List<(Func<Task<float>>, Func<Task>)>>();
        public static Task CopyToAsync(IDocument entry, Func<Task<float>> fitness, Func<Task> managedCallback) {
            string key = GenerateRequestKey(entry);
            GetManagedActions(key, CopyToAsyncManagedActions)?.Add((fitness, managedCallback));
            return Task.CompletedTask;
        }
        public static async Task Flush_CopyToAsync(IEntry entry) {
            string key = GenerateRequestKey(entry);
            List<(Func<Task<float>>, Func<Task>)> toDo = GetManagedActions(key, CopyToAsyncManagedActions, true);

            if(toDo == null) return;

            await Task.WhenAll(toDo.Select(x => x.Item2()));
        }
        public static Task Ignore_CopyToAsync(IEntry entry) {
            string key = GenerateRequestKey(entry);
            if(CopyToAsyncManagedActions.ContainsKey(key)) {
                CopyToAsyncManagedActions[key] = null;
            } else {
                CopyToAsyncManagedActions.Add(key, null);
            }
            return Task.CompletedTask;
        }
    }
}
