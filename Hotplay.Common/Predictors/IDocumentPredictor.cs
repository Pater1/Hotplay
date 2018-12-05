using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Hotplay.Common.Predictors {
    public interface IDocumentPredictor {
        Task Request(string docPath);
        Task Request_Bubbleup(string name);
        Task<IEnumerable<string>> GetLikelyhoodSortedDocuments();
    }
}
