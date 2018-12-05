using FubarDev.WebDavServer;
using FubarDev.WebDavServer.AspNetCore;
using FubarDev.WebDavServer.AspNetCore.Filters;
using FubarDev.WebDavServer.Dispatchers;
using FubarDev.WebDavServer.Engines.Remote;
using FubarDev.WebDavServer.FileSystem;
using FubarDev.WebDavServer.FileSystem.DotNet;
using FubarDev.WebDavServer.Formatters;
using FubarDev.WebDavServer.Handlers;
using FubarDev.WebDavServer.Locking;
using FubarDev.WebDavServer.Locking.InMemory;
using FubarDev.WebDavServer.Props;
using FubarDev.WebDavServer.Props.Dead;
using FubarDev.WebDavServer.Props.Store;
using FubarDev.WebDavServer.Props.Store.TextFile;
using FubarDev.WebDavServer.Utils;
using Hotplay.Common;
using Hotplay.Common.Caches.Allocator;
using Hotplay.Common.Caches.Invalidator;
using Hotplay.Common.DocumentStore;
using Hotplay.Common.FileChain;
using Hotplay.Common.FileChain.ChainLinks;
using Hotplay.Common.FileChain.ChainLinks.Cache;
using Hotplay.Common.FileChain.ChainLinks.LocalDiskAccess;
using Hotplay.Common.FileChain.ChainLinks.Network;
using Hotplay.Common.FileSystems;
using Hotplay.Common.Helpers;
using Hotplay.Common.Predictors;
using Hotplay.Middleware.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Hotplay.Middleware {
    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services
            .Configure<DotNetFileSystemOptions>(
                opt => {
                    //opt.RootPath = Path.Combine(Path.GetTempPath(), "webdav");
                    //opt.AnonymousUserName = "anonymous";

                    opt.RootPath = @"E:\\Testing";
                    opt.AnonymousUserName = null;
                })
            .Configure<ChainedFileSystemOptions>(
                opt => {
                    opt.ChainLinkGenerators = new Func<ChainLinkGeneratorOptions, IFileChain>[]{
                        (x =>   FileChainManager.RequestTrackedChain("RAMCache", () =>
                                    new ManagedCache(
                                        new DocumentCache(
                                            new RAMDocumentStore(),
                                            new FileCountAllocator(50, new LRUInvalidator()),
                                            true
                                        )
                                    )
                                )
                        ),

                        (x => FileChainManager.RequestTrackedChain("MarkovChain-1", () =>
                                new DocumentPredictor(
                                    new LocalDiskDocumentStore(new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Hotplay", "MarkovChain-1"))),
                                    new MarkovChainDocumentPredictor(1, "MarkovChain-1-RAM.precache"),
                                    new FileCountAllocator(100, new LRUInvalidator()),
                                    x.FileSystem)
                                )
                        ),
                        (x => FileChainManager.RequestTrackedChain("MarkovChain-3", () =>
                                new DocumentPredictor(
                                    new LocalDiskDocumentStore(new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Hotplay", "MarkovChain-3"))),
                                    new MarkovChainDocumentPredictor(3, "MarkovChain-3-RAM.precache"),
                                    new FileCountAllocator(100, new LRUInvalidator()),
                                    x.FileSystem)
                                )
                        ),
                        (x => FileChainManager.RequestTrackedChain("MarkovChain-5", () =>
                                new DocumentPredictor(
                                    new LocalDiskDocumentStore(new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Hotplay", "MarkovChain-5"))),
                                    new MarkovChainDocumentPredictor(5, "MarkovChain-5-RAM.precache"),
                                    new FileCountAllocator(100, new LRUInvalidator()),
                                    x.FileSystem)
                                )
                        ),

                        //DocumentCache
                        (x =>   FileChainManager.RequestTrackedChain("DiskCache", () =>
                                    new ManagedCache(
                                        new LocalDiskDocumentStore(new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Hotplay", "cache"))),
                                        new FileCountAllocator(500, new LRUInvalidator())
                                    )
                                )
                        ),


                        //TODO: generateRandomKey
                        (x => FileChainManager.RequestTrackedChain("NetworkAccess", () =>
                                new NetworkAccessFileChain(new NetworkAccessFileChainOptions(){
                                    BaseConnectionUri = new Uri("wss://localhost:44396/files/"),
                                    ConcurrentRequestLimit = 8,
                                    KeyGenerator = null
                                })
                            )
                        ),

                        //(x => new LocalDiskAccessFileChain(new LocalDiskAccessFileChain.LocalDiskAccessFileChain_Options(){
                        //    RootPath = @"E:\\Testing"
                        //}, x.FileSystem)),

                        //(x => {
                        //        DirectDotNetFileSystemFactory factory = new DirectDotNetFileSystemFactory(
                        //            x.ServiceProvider.GetService<IOptions<DotNetFileSystemOptions>>(),
                        //            x.ServiceProvider.GetService<IPathTraversalEngine>(),
                        //            x.ServiceProvider.GetService<IPropertyStoreFactory>(),
                        //            x.ServiceProvider.GetService<ILockManager>()
                        //        );
                        //        return new FileSystemChainWrapper(factory.CreateFileSystem(x.MountPoint, x.Principal));
                        //    }
                        //)
                    };
                })
            .AddSingleton<IFileSystemFactory, ChainedFileSystemFactory>()
            //.AddSingleton<IFileSystemFactory, BuildOnceChainedFileSystemFactory>()
            .AddSingleton<IPropertyStoreFactory, TextFilePropertyStoreFactory>()
            .AddSingleton<ILockManager, InMemoryLockManager>()
            .TrackServiceCollection()
            .AddMvcCore()
            .AddAuthorization()
            //            .AddWebDav();
            .AddCustomWebDav();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
            if(env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            } else {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            //app.Use(async (context, next) => {
            //    string[] path = Regex.Split(context.Request.Path.Value, @"(?<=[/])");

            //    string rerouter = path[1];

            //    IEnumerable<string> fileRelevant = path.Skip(2).Where(x => !string.IsNullOrEmpty(x));
            //    string filePath = "/" + (fileRelevant.Any() ? fileRelevant.Aggregate((x, y) => $"{x}{y}") : "");

            //    context.Request.Path = filePath;
            //    await ManualReroute(rerouter, filePath, context, next);
            //});
            app.Use(async (context, next) => {
                await next();
            });
            app.UseMvc();
        }

        private Task ManualReroute(string routing, string path, HttpContext context, Func<Task> next) {
            switch(routing.ToLowerInvariant()) {
                case "files/":
                case "file/":
                case "files":
                case "file":
                    return next();
                default:
                    return Task.CompletedTask;
            }
        }
    }
}
