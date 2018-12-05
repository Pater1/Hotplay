using FubarDev.WebDavServer.FileSystem;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hotplay.Common.Helpers {
    public static class IEntryExtentions {

        public static string FullPath(this IEntry entry) {
            return Uri.UnescapeDataString(entry.Path.OriginalString);
        }
        public static string ParentFullPath(this IEntry entry) {
            string[] pathParts = entry.FullPath().Split('/');
            string fullPath = "";
            for(int i = 0; i < pathParts.Length - 1; i++) {
                fullPath += pathParts[i];
            }
            return fullPath;
        }
    }
}
