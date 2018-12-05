using FubarDev.WebDavServer.FileSystem;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Hotplay.Common.FileChain.ChainLinks.Cache {
    public interface IDocumentCache: IFileChain, IDisposable {
        Task<float> FitnessToStoreAsync(string key, Func<Task<IDocument>> doc, Func<Task<Stream>> stream);
    }
}
