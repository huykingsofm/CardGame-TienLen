using System.IO;
using System.Json;
using System.Text;

namespace Server{
    class JsonReader : FileStream{
        public static JsonReader __default__ = new JsonReader("server.ini");
        public JsonReader(string path) : base(path, FileMode.Open, FileAccess.Read){
        }

        public JsonValue Read(){
            byte[] content = new byte[1000];
            int len = base.Read(content, 0, 1000);
            string s = Encoding.ASCII.GetString(content, 0, len);
            JsonValue json = JsonValue.Parse(s);
            return json;
        }
    }
}