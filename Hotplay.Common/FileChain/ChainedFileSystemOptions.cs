using System;
using System.Collections.Generic;
using System.Text;

namespace Hotplay.Common.FileChain {
    public class ChainedFileSystemOptions {
        public IEnumerable<Func<ChainLinkGeneratorOptions, IFileChain>> ChainLinkGenerators{ get; set; }
    }
}
