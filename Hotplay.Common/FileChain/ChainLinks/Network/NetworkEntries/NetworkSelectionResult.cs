using FubarDev.WebDavServer.FileSystem;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Hotplay.Common.FileChain.ChainLinks.Network.NetworkEntries {
    public class NetworkSelectionResult {
        [JsonProperty]
        public SelectionResultType SelectionResultType{ get; set; }
        [JsonProperty]
        public NetworkClientCollection Collection{ get; set; }
        [JsonProperty]
        public NetworkClientDocument Document{ get; set; }
        [JsonProperty]
        public string[] MissingNames { get; set; }

        [JsonIgnore]
        public IFileSystem FileSystem { get; set; }
        [JsonIgnore]
        public JsonSerializer Serializer { get; set; }

        [JsonIgnore]
        public Func<Task<WebSocket>> Transport { get; set; }

        public NetworkSelectionResult() { }
        public NetworkSelectionResult(SelectionResult selectionResult) {
            SelectionResultType = selectionResult.ResultType;
            Collection = new NetworkClientCollection( selectionResult.Collection );

            if(SelectionResultType == SelectionResultType.FoundDocument) {
                Document = new NetworkClientDocument( selectionResult.Document );
            } else{
                Document = null;
            }

            if(SelectionResultType == SelectionResultType.MissingCollection || SelectionResultType == SelectionResultType.MissingDocumentOrCollection) {
                MissingNames = selectionResult.MissingNames.ToArray();
            }else{
                MissingNames = new string[0];
            }
        }

        public SelectionResult AsSelectionResult() {
            Collection.FileSystem = FileSystem;
            Collection.Serializer = Serializer;
            Collection.Transport = Transport;
            switch(SelectionResultType) {
                case SelectionResultType.FoundCollection:
                    return SelectionResult.Create(Collection);
                case SelectionResultType.FoundDocument:
                    Document.FileSystem = FileSystem;
                    Document.Serializer = Serializer;
                    Document.Transport = Transport;
                    return SelectionResult.Create(Collection, Document);
                case SelectionResultType.MissingCollection:
                    return SelectionResult.CreateMissingCollection(Collection, MissingNames);
                case SelectionResultType.MissingDocumentOrCollection:
                    return SelectionResult.CreateMissingDocumentOrCollection(Collection, MissingNames);
                default:
                    return null;//can't be reached, but the compiler requires it...
            }
        }
    }
}
