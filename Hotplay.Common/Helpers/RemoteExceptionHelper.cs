using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Hotplay.Common.Helpers {
    public static class RemoteExceptionHelper {
        private static JsonSerializer Serializer { get; set; } = new JsonSerializer() {
            TypeNameHandling = TypeNameHandling.All
        };
        public static string StandardSerializeException<T>(T t)where T: Exception{
            string ret;
            using(StringWriter sw = new StringWriter()){
                using(JsonWriter jw = new JsonTextWriter(sw)){
                    Serializer.Serialize(jw, t);
                    ret = sw.ToString();
                }
            }
            return ret;
        }
        public static Exception StandardDeserializeException(string ser){
            using(StringReader sr = new StringReader(ser)){
                using(JsonReader jr = new JsonTextReader(sr)){
                    return Serializer.Deserialize(jr) as Exception;
                }
            }
        }
    }
}
