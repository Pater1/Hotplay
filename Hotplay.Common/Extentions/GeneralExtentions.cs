using FubarDev.WebDavServer.FileSystem;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hotplay.Common {
    public static class GeneralExtentions {
        public static FileStream CreateOrTruncate(this FileInfo file) {
            return file.Exists ? file.Open(FileMode.Truncate) : file.Create();
        }

        public static IServiceCollection TackedCollection { get; set; } = null;
        public static IServiceCollection TrackServiceCollection(this IServiceCollection collection) {
            TackedCollection = collection;
            return TackedCollection;
        }

        public static string ScrubPath(this string path) => path.Replace(System.IO.Path.DirectorySeparatorChar, '/').Replace("//", "/");
    }
}
