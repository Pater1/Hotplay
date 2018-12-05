using FubarDev.WebDavServer;
using FubarDev.WebDavServer.AspNetCore;
using FubarDev.WebDavServer.AspNetCore.Filters;
using FubarDev.WebDavServer.Dispatchers;
using FubarDev.WebDavServer.Engines.Remote;
using FubarDev.WebDavServer.FileSystem;
using FubarDev.WebDavServer.Formatters;
using FubarDev.WebDavServer.Handlers;
using FubarDev.WebDavServer.Locking;
using FubarDev.WebDavServer.Props;
using FubarDev.WebDavServer.Props.Dead;
using FubarDev.WebDavServer.Props.Store;
using FubarDev.WebDavServer.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hotplay.Common.Helpers {
    public static class ServerSetupExtentions {
        public static IMvcCoreBuilder AddCustomWebDav(this IMvcCoreBuilder builder) {
            builder.Services.AddCustomWebDav();
            return builder;
        }
        public static IServiceCollection AddCustomWebDav(this IServiceCollection services) {
            IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes());
            Type _WebDavXmlSerializerMvcOptionsSetup = types.Where(x => x.Name.Contains("WebDavXmlSerializerMvcOptionsSetup")).FirstOrDefault();
            Type _WebDavExceptionFilterMvcOptionsSetup = types.Where(x => x.Name.Contains("WebDavExceptionFilterMvcOptionsSetup")).FirstOrDefault();
            Type generic = typeof(IConfigureOptions<MvcOptions>);

            ServiceDescriptor srv_WebDavExceptionFilterMvcOptionsSetup = new ServiceDescriptor(generic, _WebDavExceptionFilterMvcOptionsSetup, ServiceLifetime.Transient);
            ServiceDescriptor srv_WebDavXmlSerializerMvcOptionsSetup = new ServiceDescriptor(generic, _WebDavXmlSerializerMvcOptionsSetup, ServiceLifetime.Transient);

            services.TryAddEnumerable(srv_WebDavExceptionFilterMvcOptionsSetup);
            services.TryAddEnumerable(srv_WebDavXmlSerializerMvcOptionsSetup);

            services.TryAddTransient<IWebDavContext, WebDavContext>();
            
            services.TryAddSingleton<IDeadPropertyFactory, DeadPropertyFactory>();
            services.TryAddSingleton<IRemoteCopyTargetActionsFactory, DefaultRemoteTargetActionsFactory>();
            services.TryAddSingleton<IRemoteMoveTargetActionsFactory, DefaultRemoteTargetActionsFactory>();
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.TryAddSingleton<IHttpMessageHandlerFactory, DefaultHttpMessageHandlerFactory>();
            services.TryAddSingleton<ISystemClock, SystemClock>();
            services.TryAddSingleton<ITimeoutPolicy, DefaultTimeoutPolicy>();
            services.TryAddSingleton<ILockCleanupTask, LockCleanupTask>();
            services.TryAddSingleton<IPathTraversalEngine, PathTraversalEngine>();
            services.TryAddSingleton<IMimeTypeDetector, DefaultMimeTypeDetector>();
            services.TryAddSingleton<IEntryPropertyInitializer, DefaultEntryPropertyInitializer>();
            services
                .AddOptions()
                .AddTransient<IWebDavDispatcher, WebDavServer>()
                .AddSingleton<WebDavExceptionFilter>()
                .AddSingleton<IWebDavOutputFormatter, WebDavXmlOutputFormatter>()
                .AddSingleton<LockCleanupTask>();
            services.Scan(
                scan => scan
                    .FromAssemblyOf<IHandler>()
                    .AddClasses(classes => classes.AssignableToAny(typeof(IHandler), typeof(IWebDavClass)))
                    .AsImplementedInterfaces()
                    .WithTransientLifetime());
            services.AddSingleton(
                sp => {
                    var factory = sp.GetRequiredService<IFileSystemFactory>();
                    var context = sp.GetRequiredService<IWebDavContext>();
                    return factory.CreateFileSystem(null, context.User);
                });
            services.AddSingleton(
                sp => {
                    var factory = sp.GetRequiredService<IPropertyStoreFactory>();
                    var fileSystem = sp.GetRequiredService<IFileSystem>();
                    return factory.Create(fileSystem);
                });
            return services;
        }
    }
}
