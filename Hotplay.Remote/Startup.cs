using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Hotplay.Common.FileChain.ChainLinks.Network;
using Hotplay.Common.Extentions;

namespace Hotplay.Remote {
    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services
                .AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                ;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
            if(env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            } else {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();
            app.UseWebSockets(new WebSocketOptions() {
                
            });
            app.Use(async (context, next) => {
                string[] path = context.Request.Path.Value.Split('/').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
                ulong key;
                if(path.Length >= 2 && path[0] == "files" && ulong.TryParse(path[1], out key)) {
                    if(context.WebSockets.IsWebSocketRequest) {
                        NetworkAccessServer networkAccessServer = NetworkAccessServer.NewTracked(key, () =>
                            context.WebSockets.AcceptWebSocketAsync());
                        if(networkAccessServer == null) {
                            context.Response.StatusCode = 423;
                        } else {
                            await networkAccessServer.StartAsync();
                        }
                    } else {
                        context.Response.StatusCode = 400;
                    }
                } else {
                    await next();
                }
            });
        }
    }
}
