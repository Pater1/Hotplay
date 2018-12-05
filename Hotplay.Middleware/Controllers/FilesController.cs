using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FubarDev.WebDavServer;
using FubarDev.WebDavServer.AspNetCore;
using FubarDev.WebDavServer.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.Extensions.Logging;

namespace Hotplay.Middleware.Controllers
{
    [Route("{*path}")]
    public class FilesController: WebDavControllerBase
    {
        public FilesController(IWebDavContext context, IWebDavDispatcher dispatcher, ILogger<WebDavIndirectResult> responseLogger = null)
             : base(context, dispatcher, responseLogger) {
        }
        internal async Task InvokeManualControllerCall(HttpContext context, string filePath, IServiceProvider serviceProvider, CancellationToken ct = default(CancellationToken)) {
            IActionResult action = await QuearyManualControllerCall(context, filePath, serviceProvider, ct);
            await action.ExecuteResultAsync(this.ControllerContext);
        }
        internal Task<IActionResult> QuearyManualControllerCall(HttpContext context, string filePath, IServiceProvider serviceProvider, CancellationToken ct = default(CancellationToken)) {
            switch(context.Request.Method.ToUpperInvariant()){
                case "OPTIONS":
                    return QueryOptionsAsync(filePath, ct);
                case "MKCOL":
                    return MkColAsync(filePath, ct);
                case "GET":
                    return GetAsync(filePath, ct);
                case "PUT":
                    return PutAsync(filePath, ct);
                case "DELETE":
                    return DeleteAsync(filePath, ct);
                case "PROPFIND":
                    propfind request = null;
                    return PropFindAsync(filePath, request, ct);
                case "PROPPATCH":
                    propertyupdate propUp = null;
                    return PropPatchAsync(filePath, propUp, ct);
                case "HEAD":
                    return HeadAsync(filePath, ct);
                case "COPY":
                    string copyDestination = context.Request.Headers["Destination"];
                    return CopyAsync(filePath, copyDestination, ct);
                case "MOVE":
                    string moveDestination = context.Request.Headers["Destination"];
                    return MoveAsync(filePath, moveDestination, ct);
                case "LOCK":
                    Stream body = context.Request.Body;
                    MemoryStream mem = new MemoryStream();
                    body.CopyTo(mem);
                    body.Dispose();
                    byte[] raw = mem.ToArray();
                    string json = Encoding.UTF8.GetString(raw);//?
                    return LockAsync(filePath, null, ct);
                case "UNLOCK":
                    string lockToken = context.Request.Headers["Lock-Token"];
                    return this.UnlockAsync(filePath, lockToken);
                default:
                    return null;
            }
        }
    }
}
