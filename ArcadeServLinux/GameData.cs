using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace APM3Serv
{
    static class GameData
    {
        static Dictionary<string, JsonNode> gamedata = new Dictionary<string, JsonNode>();
        static JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = false, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

        private static JsonNode Open(string gameid)
        {
            if (!gamedata.ContainsKey(gameid))
            {
                string dataFilename = @"apm3data/ABAASGS-" + gameid + "-data.json";
                string dataString = "";
                FileStream fs = new FileStream(dataFilename, FileMode.Open, FileAccess.Read, FileShare.None);
                BinaryReader reader = new BinaryReader(fs);
                byte[] bytes = new byte[fs.Length];
                int read;
                if ((read = reader.Read(bytes, 0, bytes.Length)) != 0)
                {
                    dataString = Encoding.UTF8.GetString(bytes, 0, read);
                }
                reader.Close();
                fs.Close();

                gamedata.Add(gameid, JsonNode.Parse(dataString));
            }

            return gamedata[gameid];
        }

        public static void Close(string gameid)
        {
            if (gamedata.ContainsKey(gameid))
            {
                string dataFilename = @"apm3data/ABAASGS-" + gameid + "-data.json";
                string dataString = gamedata[gameid].ToJsonString(options);

                if (File.Exists(dataFilename))
                {
                    string dataBackupFilename = @"apm3data/ABAASGS-" + gameid + "-data.json.backup";
                    File.Copy(dataFilename, dataBackupFilename, true);
                }
                FileStream fs = new FileStream(dataFilename, FileMode.Truncate, FileAccess.Write, FileShare.None);
                BinaryWriter writer = new BinaryWriter(fs);
                byte[] data = Encoding.UTF8.GetBytes(dataString);
                writer.Write(data);
                writer.Close();
                fs.Close();
            }
        }

        public static void CloseAll()
        {
            foreach (string key in gamedata.Keys)
            {
                Close(key);
            }
        }

        public static string Load(string gameid, string loadString)
        {
            JsonNode dataJSON = Open(gameid);
            JsonNode loadJSON = JsonNode.Parse(loadString);
            JsonObject resultJSON = new JsonObject { ["free_buckets"] = new JsonObject { } };

            JsonArray keys = loadJSON["free_buckets"]["keys"].AsArray();

            foreach (string key in keys)
            {
                if (dataJSON["free_buckets"]?[key] is JsonNode resultNode)
                {
                    // re-parsing here will perform poorly, but . . . meh
                    resultJSON["free_buckets"][key] = JsonNode.Parse(resultNode.ToJsonString(options));
                }
            }

            return resultJSON.ToJsonString(options);
        }

        public static void Save(string gameid, string saveString)
        {
            JsonObject dataJSON = Open(gameid).AsObject();
            JsonNode saveJSON = JsonNode.Parse(saveString);

            // more re-parsing, blame System.Text.Json . . .
            if (saveJSON?["cabinet_properties"] is JsonNode cabNode)
            {
                dataJSON["cabinet_properties"] = JsonNode.Parse(cabNode.ToJsonString(options));
            }
            if (saveJSON?["collections"] is JsonNode collNode)
            {
                dataJSON["collections"] = JsonNode.Parse(collNode.ToJsonString(options));
            }
            dataJSON["client"] = JsonNode.Parse(saveJSON["client"].ToJsonString(options));

            gamedata[gameid] = dataJSON;
        }
    }
}
