using System.Text;
using System.IO;
using System.Text.Json.Nodes;

namespace APM3Serv
{
    static class APM3
    {
        private static JsonNode keyDataJSON;
        public static string aimeKey;

        static APM3()
        {
            string keyDataString = "";
            string keyDataFileName = @"apm3data/keydata.json";
            FileStream fs = new FileStream(keyDataFileName, FileMode.Open, FileAccess.Read, FileShare.None);
            BinaryReader reader = new BinaryReader(fs);
            byte[] bytes = new byte[fs.Length];
            int read;
            if ((read = reader.Read(bytes, 0, bytes.Length)) != 0)
            {
                keyDataString = Encoding.UTF8.GetString(bytes, 0, read);
            }
            reader.Close();
            fs.Close();

            keyDataJSON = JsonNode.Parse(keyDataString);
            aimeKey = (string)keyDataJSON["aime"];
        }

        private static byte[] GetVoodoo(string gameid, string path)
        {
            switch (gameid)
            {
                default: return new byte[] { 0x01, 0x01, 0x00, 0x00 }; ;
            }
        }

        public static bool EncryptResponse(string path)
        {
            switch (path)
            {
                case "/api/alive": return false;
                case "/api/server/alive": return false;
                case "/api/user/heartbeat": return false;
                case "/api/user/save": return false;
                default: return true;
            }
        }

        private static byte[] GetPassword(string path)
        {
            switch (path)
            {
                case "/api/alive": return new byte[] { 0xcf, 0xec, 0x2b, 0x93 };
                case "/api/server/alive": return new byte[] { 0xcf, 0xec, 0x2b, 0x93 };
                case "/api/user/heartbeat": return new byte[] { 0xcf, 0xec, 0x2b, 0x93 };
                case "/api/user/save": return new byte[] { 0xcf, 0xec, 0x2b, 0x93 };
                default: return new byte[] { 0x01, 0x03, 0x03, 0x07 };
            }
        }

        // this data needs to be externalized, it cannot be distributed
        public static string GetKey(string gameid, string path)
        {
            switch (gameid)
            {
                case "SDFM":
                    switch (path)
                    {
                        case "/api/turninfo": return (string) keyDataJSON["keys"][gameid][path];
                        case "/api/config": return (string)keyDataJSON["keys"][gameid][path];
                        case "/api/alive": return (string)keyDataJSON["keys"][gameid][path];
                        default: return (string)keyDataJSON["keys"][gameid]["default"];
                    }
                case "SDFF":
                    switch (path)
                    {
                        case "/api/turninfo": return (string)keyDataJSON["keys"][gameid][path];
                        case "/api/config": return (string)keyDataJSON["keys"][gameid][path];
                        case "/api/alive": return (string)keyDataJSON["keys"][gameid][path];
                        default: return (string)keyDataJSON["keys"][gameid]["default"];
                    }
                case "SDFD":
                    switch (path)
                    {
                        case "/api/turninfo": return (string)keyDataJSON["keys"][gameid][path];
                        case "/api/config": return (string)keyDataJSON["keys"][gameid][path];
                        case "/api/alive": return (string)keyDataJSON["keys"][gameid][path];
                        default: return (string)keyDataJSON["keys"][gameid]["default"];
                    }
                case "SDHF":
                    switch (path)
                    {
                        case "/api/turninfo": return (string)keyDataJSON["keys"][gameid][path];
                        case "/api/config": return (string)keyDataJSON["keys"][gameid][path];
                        case "/api/alive": return (string)keyDataJSON["keys"][gameid][path];
                        default: return (string)keyDataJSON["keys"][gameid]["default"];
                    }
                case "SDHV":
                    switch (path)
                    {
                        case "/api/turninfo": return (string)keyDataJSON["keys"][gameid][path];
                        case "/api/config": return (string)keyDataJSON["keys"][gameid][path];
                        case "/api/alive": return (string)keyDataJSON["keys"][gameid][path];
                        default: return (string)keyDataJSON["keys"][gameid]["default"];
                    }
                case "SDDS": return (string)keyDataJSON["keys"][gameid]["default"];
                case "SDHB": return (string)keyDataJSON["keys"][gameid]["default"];
                case "SDGW": return (string)keyDataJSON["keys"][gameid]["default"];
                case "SDGU": return (string)keyDataJSON["keys"][gameid]["default"];
                case "SDHW": return (string)keyDataJSON["keys"][gameid]["default"];
                case "SDHP": return (string)keyDataJSON["keys"][gameid]["default"];
                case "SDJG": return (string)keyDataJSON["keys"][gameid]["default"];
                default: return "0000000000000000000000000000000000000000000000000000000000000000";
            }
        }

        public static byte[] GetIV(string gameid, string path, byte[] crc, int responseLen)
        {
            // unknown voodoo
            byte[] voodoo = GetVoodoo(gameid, path);
            // length of un-padded cleartext
            byte[] len = BitConverter.GetBytes(responseLen);
            Array.Reverse(len);
            // password from turninfo request
            byte[] password = GetPassword(path);

            byte[] IV = voodoo.Concat(crc).Concat(len).Concat(password).ToArray();

            return IV;
        }

        // some canned responses for APM3 requests that don't have dynamic content
        public static string GetCannedResponse(string gameid, string path)
        {
            switch (path)
            {
                case "/api/user/save": return "";
                case "/api/user/heartbeat": return "";
                case "/api/server/alive": return "";
                case "/api/data/save": return "null"; // weird
                case "/api/alive": return "";
                case "/api/data/load": return "{\"free_buckets\":{}}";
                case "/api/turninfo": return "{\"host\":\"" + APM3WebServer.apm3Address + "\",\"port\":80,\"id\":\"" + gameid + "\",\"pw\":\"1337\"}";
                case "/api/config": return "{\"polling\":500,\"log_server_host\":\"" + APM3WebServer.apm3Address + "\",\"log_server_port\":80}";
                default: return "";
            }
        }

        // canned AIME responses by message type, until decoding/encoding work is done
        public static Dictionary<string, Dictionary<string, string>> aimeResponses = new Dictionary<string, Dictionary<string, string>>()
        {
            { "SDDS", new Dictionary<string, string>()
                {
                    { "campaign", "3EA187300C000002010000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" },
                    { "goodbye", "" },
                    { "unknown", "" },
                    { "felica", "3EA187300300300001000000000000000000000000000000000000000000000000000000000000000000000252850000" },
                    { "lookup2", "3EA1873010003001010000000000000000000000000000000000000000000000C562000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" }
                }
            },
            { "SDEC", new Dictionary<string, string>()
                {
                    { "campaign", "3EA187300C000002010000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" },
                    { "goodbye", "" },
                    { "unknown", "" },
                    { "felica", "3EA187300300300001000000000000000000000000000000000000000000000000000000000000000000000252850000" },
                    { "lookup2", "3EA1873010003001010000000000000000000000000000000000000000000000C562000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" }
                }
            }
        };

        public static string GetAIMERequestType(byte[] request)
        {
            string type = "";
            byte code = request[4];
            switch (code)
            {
                case 0x01: type = "felica"; break;
                case 0x04: type = "lookup"; break;
                case 0x05: type = "register"; break;
                case 0x09: type = "log"; break;
                case 0x0b: type = "campaign"; break;
                case 0x0d: type = "register2"; break;
                case 0x0f: type = "lookup2"; break;
                case 0x64: type = "hello"; break;
                case 0x66: type = "goodbye"; break;
                default: type = "unknown"; break;
            }
            return type;
        }

        public static string GetAIMEGameID(byte[] request)
        {
            return Encoding.ASCII.GetString(request.Skip(10).Take(4).ToArray());
        }
    }
}
