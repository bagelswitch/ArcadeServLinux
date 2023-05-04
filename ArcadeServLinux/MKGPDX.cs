using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace MKGPDXServ
{
    static class MKGPDX
    {
        static Dictionary<string, MKGPDXUser> userdata = new Dictionary<string, MKGPDXUser>();

        private static MKGPDXUser Open(string userid)
        {
            if (!userdata.ContainsKey(userid))
            {
                string dataFilename = @"mkgpdata/" + userid + ".bin";
                if (!File.Exists(dataFilename))
                {
                    // create new user data from template
                    string templateFilename = @"mkgpdata/user.bin";
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

                userdata.Add(userid, new MKGPDXUser(dataString));
            }

            return userdata[userid];
        }

        public static void Close(string userid)
        {
            if (userdata.ContainsKey(userid))
            {
                string dataFilename = @"mkgpdata/" + userid + ".bin";
                string dataString = userdata[userid].ToString();

                if (File.Exists(dataFilename))
                {
                    string dataBackupFilename = @"mkgpdata/" + userid + ".bin.backup";
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

        public static string Load(string userid)
        {
            MKGPDXUser data = Open(userid);
            return data.ToString();
        }

        public static void Save(string userid, string saveString)
        {
            MKGPDXUser data = Open(userid);
            MKGPDXUserReq request = new MKGPDXUserReq(saveString);

            data.Merge(request);

            userdata[userid] = data;
        }

        // some canned responses for APM3 requests that don't have dynamic content
        public static string GetCannedResponse(string userid, string path)
        {
            switch (path)
            {
                case "/server/getStatus": return "[0,9]";
                case "/board/saveData": return "[0,[[60000,2],[60000,2],[60000,2],[60000,2],[60000,2],[60000,2],[60000,2],[60000,2],[60000,2],[60000,2],[60000,2],[60000,2],[60000,2],[60000,2],[60000,2],[60000,2],[60000,2],[60000,2]]]";
                case "/amid/getAmidInfo_bf": return "[0,0,1," + userid + ",1007,\"JPN\",315911285,\"JPN\",0,0,1,0,\"\",\"\",\"\",\"\",\"\"]";
                case "/judgement/getData": return "[0,[[[0,0,3]],[[0,2],[10,1]],[[18,3],[22,1],[39,1],[41,2],[56,2]],[[1,0,7,2],[2,0,7,2],[10,0,3,6],[11,0,5,5],[15,0,2,0],[16,0,1,0]],\"66666666666666666666666666666666666666666666666666\"]]";
                case "/incoming/saveData": return "[0]";
                case "/judgement/saveData": return "[0]";
                case "/bookkeep/saveData": return "[0,\"\"]";
                case "/player/saveData": return "[0]";
                case "/amid/unLock": return "[0]";
                default: return "[0]";
            }
        }
    }
}
