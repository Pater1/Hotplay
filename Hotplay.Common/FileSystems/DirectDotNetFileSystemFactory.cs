using FubarDev.WebDavServer.FileSystem;
using FubarDev.WebDavServer.FileSystem.DotNet;
using FubarDev.WebDavServer.Locking;
using FubarDev.WebDavServer.Props.Store;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace Hotplay.Common.FileSystems
{
    public class DirectDotNetFileSystemFactory: IFileSystemFactory {
        private readonly IPathTraversalEngine _pathTraversalEngine;
        
        private readonly IPropertyStoreFactory _propertyStoreFactory;
        
        private readonly ILockManager _lockManager;
        
        private readonly DotNetFileSystemOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="DotNetFileSystemFactory"/> class.
        /// </summary>
        /// <param name="options">The options for this file system</param>
        /// <param name="pathTraversalEngine">The engine to traverse paths</param>
        /// <param name="propertyStoreFactory">The store for dead properties</param>
        /// <param name="lockManager">The global lock manager</param>
        public DirectDotNetFileSystemFactory(
            IOptions<DotNetFileSystemOptions> options,
            IPathTraversalEngine pathTraversalEngine,
            IPropertyStoreFactory propertyStoreFactory = null,
            ILockManager lockManager = null) {
            _pathTraversalEngine = pathTraversalEngine;
            _propertyStoreFactory = propertyStoreFactory;
            _lockManager = lockManager;
            _options = options.Value;
        }

        public IPathTraversalEngine PathTraversalEngine => _pathTraversalEngine;

        public IPathTraversalEngine PathTraversalEngine1 => _pathTraversalEngine;

        /// <inheritdoc />
        public virtual IFileSystem CreateFileSystem(ICollection mountPoint, IPrincipal principal) {
            return new DotNetFileSystem(_options, mountPoint, _options.RootPath, PathTraversalEngine, _lockManager, _propertyStoreFactory);
        }
    }
}
