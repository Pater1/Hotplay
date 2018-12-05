using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Hotplay.Remote.Controllers {
    [Route("files")]
    //[ApiController]
    public class FilesController: ControllerBase {
        // GET files/{random long}
        public HttpResponse OpenSocket(int id) {
            if(this.HttpContext.WebSockets.IsWebSocketRequest) {
                var webSocket = this.HttpContext.WebSockets.AcceptWebSocketAsync().Result;
                if(webSocket != null && webSocket.State == WebSocketState.Open) {
                    //while(true) {
                        var response = string.Format("Hello! Time {0}", System.DateTime.Now.ToString());
                        var bytes = System.Text.Encoding.UTF8.GetBytes(response);

                        webSocket.SendAsync(new System.ArraySegment<byte>(bytes),
                            WebSocketMessageType.Text, true, CancellationToken.None)
                            .RunSynchronously();

                        //await Task.Delay(2000);
                    //}
                }
            }

            this.Response.StatusCode = StatusCodes.Status101SwitchingProtocols;
            return this.Response;
        }

        /* TODO: RESTful API access
        #region Documents
        Task<(bool success, IDocument result)> TryCopyToAsync(IDocument entry, ICollection collection, string name, CancellationToken ct);
        Task<(bool success, Stream result)> TryCreateAsync(IDocument entry, CancellationToken ct);
        Task<(bool success, DeleteResult result)> TryDeleteAsync(IDocument entry, CancellationToken ct);
        Task<(bool success, IDocument result)> TryMoveToAsync(IDocument entry, ICollection collection, string name, CancellationToken ct);
        Task<(bool success, Stream result)> TryOpenReadAsync(IDocument entry, CancellationToken ct);
        #endregion

        #region Collections
        Task<(bool success, ICollection result)> TryCreateCollectionAsync(ICollection entry, string name, CancellationToken ct);
        Task<(bool success, IDocument result)> TryCreateDocumentAsync(ICollection entry, string name, CancellationToken ct);
        Task<(bool success, DeleteResult result)> TryDeleteAsync(ICollection entry, CancellationToken ct);
        #endregion
        */
    }
}
