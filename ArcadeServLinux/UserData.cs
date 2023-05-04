using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace APM3Serv
{
    static class UserData
    {
        static Dictionary<string, JsonNode> userdata = new Dictionary<string, JsonNode>();
        static JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = false, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

        private static JsonNode Open(string userid, string gameid)
        {
            if (!userdata.ContainsKey(userid))
            {
                string dataFilename = @"apm3data/" + userid + ".json";
                if (!File.Exists(dataFilename))
                {
                    // create new user data from template
                    string templateFilename = @"apm3data/ABAASGS-" + gameid + "-user.json";
                    File.Copy(templateFilename, dataFilename, false);
                }
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

                userdata.Add(userid, JsonNode.Parse(dataString));
            }

            return userdata[userid];
        }

        public static void Close(string userid)
        {
            if (userdata.ContainsKey(userid))
            {
                string dataFilename = @"apm3data/" + userid + ".json";
                string dataString = userdata[userid].ToJsonString(options);

                if (File.Exists(dataFilename))
                {
                    string dataBackupFilename = @"apm3data/" + userid + ".json.backup";
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
            foreach (string key in userdata.Keys)
            {
                Close(key);
            }
        }

        public static string Load(string userid, string gameid, string loadString)
        {
            JsonNode dataJSON = Open(userid, gameid);
            JsonNode loadJSON = JsonNode.Parse(loadString);

            int reqUserId = (int)loadJSON["user"]["user_id"];
            dataJSON["user"]["user_id"] = reqUserId;

            JsonObject resultJSON = JsonNode.Parse(dataJSON.ToJsonString(options)).AsObject();
            JsonObject savedataJSON = new JsonObject { };

            // construct savedata element
            if (loadJSON["user"]["savedata"]?["keys"] is JsonNode requestNode)
            {
                JsonArray keys = loadJSON["user"]["savedata"]["keys"].AsArray();

                foreach (string key in keys)
                {
                    if (key.IndexOf("*") >= 0)
                    {
                        string search = key.Substring(0, key.IndexOf("*"));
                        foreach (var match in dataJSON["user"]["savedata"].AsObject().AsEnumerable())
                        {
                            if (match.Key.StartsWith(search))
                            {
                                savedataJSON[match.Key] = JsonNode.Parse(match.Value.ToJsonString(options));
                            }
                        }
                    }
                    else
                    {
                        if (dataJSON["user"]["savedata"]?[key] is JsonNode resultNode)
                        {
                            savedataJSON[key] = JsonNode.Parse(resultNode.ToJsonString(options));
                        }
                    }
                }
                resultJSON["user"]["savedata"] = savedataJSON;
            }
            else
            {
                resultJSON["user"]["savedata"] = null;
            }

            // construct control element
            int startgame = (int)loadJSON["control"]["start_game"];
            if (startgame == 0)
            {
                resultJSON["control"] = JsonNode.Parse("{\"seq_id\":null,\"online\":0,\"token\":\"484a1804e2650c145d5d25c315a5ae62e164728f\",\"start_game\":0}");
            }
            else if (startgame == 1)
            {
                resultJSON["control"] = JsonNode.Parse("{\"seq_id\":\"0008192939A63E01B958520210126210555\",\"online\":1,\"token\":\"484a1804e2650c145d5d25c315a5ae62e164728f\",\"start_game\":0}");
            }

            return resultJSON.ToJsonString(options);
        }

        public static string LoadRankings(string userid, string gameid, string loadString)
        {
            JsonNode dataJSON = Open(userid, gameid);
            JsonNode loadJSON = JsonNode.Parse(loadString);

            JsonObject resultJSON = new JsonObject();
            JsonArray resultArray = new JsonArray();

            JsonArray loadArray = loadJSON["rankings"].AsArray();
            foreach (var rankingNode in loadArray)
            {
                JsonObject ranking = rankingNode.AsObject();
                JsonArray recordArray = new JsonArray();
                JsonObject resultNode = new JsonObject();

                int ranking_id = ranking.ContainsKey("ranking_id") ? (int)ranking["ranking_id"] : 0;
                int type = ranking.ContainsKey("type") ? (int)ranking["type"] : 0;
                int filter1 = ranking.ContainsKey("filter1") ? (int)ranking["filter1"] : 0;
                int filter2 = ranking.ContainsKey("filter2") ? (int)ranking["filter2"] : 0;
                int filter3 = ranking.ContainsKey("filter3") ? (int)ranking["filter3"] : 0;
                int count = ranking.ContainsKey("count") ? (int)ranking["count"] : 0;

                resultNode["ranking_id"] = ranking_id;
                resultNode["type"] = type;

                if (dataJSON.AsObject().ContainsKey("rankings"))
                {
                    JsonArray rankingsArray = dataJSON["rankings"].AsArray();
                    int recordcount = 0;

                    foreach (var recordNode in rankingsArray)
                    {
                        if (recordcount < count)
                        {
                            JsonObject record = recordNode.AsObject();

                            int search_ranking_id = record.ContainsKey("ranking_id") ? (int)record["ranking_id"] : 0;
                            int search_filter1 = record.ContainsKey("filter1") ? (int)record["filter1"] : 0;
                            int search_filter2 = record.ContainsKey("filter2") ? (int)record["filter2"] : 0;
                            int search_filter3 = record.ContainsKey("filter3") ? (int)record["filter3"] : 0;
                            int search_type = record.ContainsKey("type") ? (int)record["type"] : 0;
                            if (search_ranking_id == ranking_id && search_type == type && search_filter1 == filter1 && search_filter2 == filter2 && search_filter3 == filter3)
                            {
                                if (!record.ContainsKey("name"))
                                {
                                    record["name"] = JsonNode.Parse(dataJSON["user"]["name"].ToJsonString());
                                }
                                recordArray.Add(JsonNode.Parse(record.ToJsonString()));
                                recordcount++;
                            }
                        }
                    }
                }
                resultNode["records"] = recordArray;
                resultArray.Add(resultNode);
            }
            resultJSON["rankings"] = resultArray;

            return resultJSON.ToJsonString(options);
        }

        public static void Save(string userid, string gameid, string saveString)
        {
            JsonObject dataJSON = Open(userid, gameid).AsObject();
            JsonNode saveJSON = JsonNode.Parse(saveString);

            int reqUserId = (int)saveJSON["user"]["user_id"];
            dataJSON["user"]["user_id"] = reqUserId;

            if ((bool)saveJSON["user"]?.AsObject().ContainsKey("savedata"))
            {
                JsonNode keys = saveJSON["user"]["savedata"];

                foreach (var property in keys.AsObject().AsEnumerable())
                {
                    dataJSON["user"]["savedata"][property.Key] = JsonNode.Parse(property.Value.ToJsonString(options));
                }
            }

            if ((bool)saveJSON["user"]?.AsObject().ContainsKey("rankings"))
            {
                dataJSON["user"]["rankings"] = JsonNode.Parse(saveJSON["user"]["rankings"].ToJsonString(options));
            }

            if (saveJSON.AsObject().ContainsKey("rankings"))
            {
                dataJSON["rankings"] = JsonNode.Parse(saveJSON["rankings"].ToJsonString(options));
            }

            userdata[userid] = dataJSON;
        }
    }
}
