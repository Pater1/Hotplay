using FubarDev.WebDavServer.FileSystem;
using FubarDev.WebDavServer.Locking;
using FubarDev.WebDavServer.Props.Dead;
using FubarDev.WebDavServer.Props.Store;
using System;
using System.Security.Principal;

namespace Hotplay.Common.FileChain {
    public class ChainLinkGeneratorOptions {
        public IServiceProvider ServiceProvider{ get; set; }
        public ICollection MountPoint { get; set; }
        public IPrincipal Principal { get; set; }

        public ChainedFileSystem FileSystem{ get; set; }
    }
}
