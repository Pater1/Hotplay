using FubarDev.WebDavServer.FileSystem;
using FubarDev.WebDavServer.Locking;
using FubarDev.WebDavServer.Props.Store;
using Microsoft.Extensions.Options;
using System;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Security.Principal;
using System.Linq;

namespace Hotplay.Common.FileChain {
    public class BuildOnceChainedFileSystemFactory: ChainedFileSystemFactory {
        public BuildOnceChainedFileSystemFactory(IOptions<ChainedFileSystemOptions> options, IPathTraversalEngine pathTraversalEngine, IServiceProvider serviceProvider, IPropertyStoreFactory propertyStoreFactory = null, ILockManager lockManager = null) : base(options, pathTraversalEngine, serviceProvider, propertyStoreFactory, lockManager) {
        }

        IFileSystem cached = null;
        public override IFileSystem CreateFileSystem(ICollection mountPoint, IPrincipal principal) {
            if(cached == null) {
                cached = base.CreateFileSystem(mountPoint, principal);
            }
            return cached;
        }
    }
    public class ChainedFileSystemFactory: IFileSystemFactory {
        private ChainedFileSystemOptions Options { get; set; }
        private IServiceProvider ServiceProvider { get; set; }
        private ILockManager LockManager{ get; set; }
        private IPropertyStoreFactory PropertyStoreFactory { get; set; }
        public ChainedFileSystemFactory(IOptions<ChainedFileSystemOptions> options, 
                                        IPathTraversalEngine pathTraversalEngine,
                                        IServiceProvider serviceProvider,
                                        IPropertyStoreFactory propertyStoreFactory = null, 
                                        ILockManager lockManager = null)
        {
            Options = options.Value;
            ServiceProvider = serviceProvider;
            LockManager = lockManager;
            PropertyStoreFactory = propertyStoreFactory;
        }

        public virtual IFileSystem CreateFileSystem(ICollection mountPoint, IPrincipal principal) {
            ChainedFileSystem cfs = new ChainedFileSystem() {
                LockManager = this.LockManager
            };

            ChainLinkGeneratorOptions opt = new ChainLinkGeneratorOptions() {
                ServiceProvider = this.ServiceProvider,
                MountPoint = mountPoint,
                Principal = principal,
                FileSystem = cfs
            };
            IEnumerable<IFileChain> Chain = Options.ChainLinkGenerators.Select(x => x(opt));
            cfs.SetupFileChain(Chain);
            cfs.PropertyStore = PropertyStoreFactory?.Create(cfs);

            return cfs;
        }
    }
}
