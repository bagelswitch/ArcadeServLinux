namespace MKGPDXServ
{
    using Util;
    using static MKGPDXServ.MKGPDXUserReq.MKGPDXUserreq;

    internal class MKGPDXUser
    {
        private MKGPDXUserdata user;

        public MKGPDXUser(string serialmkgpdxuserdata)
        {
            this.user = new MKGPDXUserdata(serialmkgpdxuserdata);
        }

        public void Merge(MKGPDXUserReq request)
        {
            this.user.UpdateTimestamp(request.GetTimestamp());
            this.user.UpdateUserdata(request.GetUserdata());
            
            if (request.GetCostumedatas().Length > 0)
            {
                foreach (MKGPDXUserReq.MKGPDXUserreq.Costumedata costumedata in request.GetCostumedatas())
                {
                    this.user.AddOrUpdateCostume(costumedata.GetCostumeid());
                }
            }

            if (request.GetKartdatas().Length > 0)
            {
                foreach (MKGPDXUserReq.MKGPDXUserreq.Kartdata kartdata in request.GetKartdatas())
                {
                    this.user.AddOrUpdateKart(kartdata.GetKartid());
                }
            }

            if (request.GetItemdatas().Length > 0)
            {
                foreach (MKGPDXUserReq.MKGPDXUserreq.Itemdata itemdata in request.GetItemdatas())
                {
                    this.user.AddOrUpdateItem(itemdata.GetItemid());
                }
            }

            string trackid = request.GetRacedata().GetCompositeid();
            int finishpos = request.GetResultdata().GetFinishpos();
            this.user.AddOrUpdateTrack(trackid, finishpos, 0);
            trackid = request.GetResultdata().GetCompositeid();
            this.user.AddOrUpdateTrack(trackid, 0, 1);
        }

        public override string ToString()
        {
            return this.user.ToString();
        }

        private struct MKGPDXUserdata
        {
            private Userdata userdata;
            private List<Costumedata> costumedatas = new List<Costumedata>();
            private List<Kartdata> kartdatas = new List<Kartdata>();
            private List<Itemdata> itemdatas = new List<Itemdata>();
            private List<Trackdata> trackdatas = new List<Trackdata>();

            public void UpdateUserdata(string userdata)
            {
                this.userdata.SetUserdata(userdata);
            }

            public void UpdateTimestamp(string timestamp)
            {
                this.userdata.SetTimestamp(timestamp);
            }

            public void AddOrUpdateCostume(int costumeid)
            {
                Console.WriteLine("Updating Costume " + costumeid);

                bool foundcostume = false;
                foreach (Costumedata costumedata in this.costumedatas)
                {
                    if (costumeid == costumedata.GetId())
                    {
                        foundcostume = true;
                        // do nothing, for now
                        break;
                    }
                }

                if (!foundcostume)
                {
                    Costumedata newcostume = new Costumedata(costumeid + ",1,0,0,0");
                    this.costumedatas.Add(newcostume);
                }
            }

            public void AddOrUpdateKart(int kartid)
            {
                Console.WriteLine("Updating Kart " + kartid);

                bool foundkart = false;
                foreach (Kartdata kartdata in this.kartdatas)
                {
                    if (kartid == kartdata.GetId())
                    {
                        foundkart = true;
                        // do nothing, for now
                        break;
                    }
                }

                if (!foundkart)
                {
                    Kartdata newkart = new Kartdata(kartid + ",1,0,0,0");
                    this.kartdatas.Add(newkart);
                }
            }

            public void AddOrUpdateItem(int itemid)
            {
                Console.WriteLine("Updating Item " + itemid);

                bool founditem = false;
                foreach (Itemdata itemdata in this.itemdatas)
                {
                    if (itemid == itemdata.GetId())
                    {
                        founditem = true;
                        // do nothing, for now
                        break;
                    }
                }

                if (!founditem)
                {
                    Itemdata newitem = new Itemdata(itemid + ",1,0");
                    this.itemdatas.Add(newitem);
                }
            }

            public void AddOrUpdateTrack(string trackid, int setbestfinish, int setnewlabel)
            {
                Console.WriteLine("Updating Track " + trackid + " with best finish " + setbestfinish + " and new label " + setnewlabel);

                int i = 0;
                bool foundtrack = false;
                foreach (Trackdata trackdata in this.trackdatas)
                {
                    if (trackid == trackdata.GetCompositeId())
                    {
                        foundtrack = true;
                        Console.WriteLine("Found Track " + trackid + ": " + trackdata.ToString());
                        if ((setbestfinish != 0) && ((setbestfinish < trackdata.GetBestfinish()) || (trackdata.GetBestfinish() == 0)))
                        {
                            trackdata.SetBestfinish(setbestfinish);
                        }
                        trackdata.setNewlabel(0); // newlabel);
                        trackdatas[i] = trackdata;
                        Console.WriteLine("Updated Track " + trackid + ": " + trackdata.ToString());
                        break;
                    }
                    i++;
                }

                if (!foundtrack)
                {
                    Trackdata newtrack = new Trackdata("0," + trackid + ",0," + setbestfinish + ",0," + setnewlabel);
                    this.trackdatas.Add(newtrack);
                    Console.WriteLine("Added Track " + trackid + ": " + newtrack.ToString());
                }

                Console.WriteLine("Track list length now: " + this.trackdatas.Count);
            }

            public MKGPDXUserdata(string serialmkgpdxuserdata)
            {
                Console.WriteLine("Creating MKGPDXUserdata from string: " + serialmkgpdxuserdata);

                Util.EatTo(ref serialmkgpdxuserdata, "[0,[");
                this.userdata = new Userdata(serialmkgpdxuserdata.Substring(0, serialmkgpdxuserdata.IndexOf("]")));

                Util.EatTo(ref serialmkgpdxuserdata, "],[[");
                String[] costumedatastrings = serialmkgpdxuserdata.Substring(0, serialmkgpdxuserdata.IndexOf("],[[")).Split("],[");
                int i = 0;
                foreach (string costumedatastring in costumedatastrings)
                {
                    costumedatas.Add(new Costumedata(costumedatastring.TrimStart('[').TrimEnd(']')));
                    i++;
                }

                Util.EatTo(ref serialmkgpdxuserdata, "]],[");
                String[] kartdatastrings = serialmkgpdxuserdata.Substring(0, serialmkgpdxuserdata.IndexOf("],[[")).Split("],[");
                int j = 0;
                foreach (string kartdatastring in kartdatastrings)
                {
                    kartdatas.Add(new Kartdata(kartdatastring.TrimStart('[').TrimEnd(']')));
                    j++;
                }

                Util.EatTo(ref serialmkgpdxuserdata, "]],[");
                String[] itemdatastrings = serialmkgpdxuserdata.Substring(0, serialmkgpdxuserdata.IndexOf("]]],")).Split("],[");
                int k = 0;
                foreach (string itemdatastring in itemdatastrings)
                {
                    itemdatas.Add(new Itemdata(itemdatastring.TrimStart('[').TrimEnd(']')));
                    k++;
                }

                Util.EatTo(ref serialmkgpdxuserdata, "],[],[");
                String[] trackdatastrings = serialmkgpdxuserdata.Substring(0, serialmkgpdxuserdata.IndexOf("]],")).Split("],[");
                int m = 0;
                foreach (string trackdatastring in trackdatastrings)
                {
                    trackdatas.Add(new Trackdata(trackdatastring.TrimStart('[').TrimEnd(']')));
                    m++;
                }
            }
            public override string ToString()
            {
                string buffer = "[0,[";
                buffer += this.userdata;
                buffer += "],[[";
                foreach (Costumedata costumedata in this.costumedatas)
                {
                    buffer += "[" + costumedata.ToString() + "],";
                }
                buffer = buffer.TrimEnd(',');
                buffer += "],[";
                foreach (Kartdata kartdata in this.kartdatas)
                {
                    buffer += "[" + kartdata.ToString() + "],";
                }
                buffer = buffer.TrimEnd(',');
                buffer += "],[";
                foreach (Itemdata itemdata in this.itemdatas)
                {
                    buffer += "[" + itemdata.ToString() + "],";
                }
                buffer = buffer.TrimEnd(',');
                buffer += "]],[],[";
                foreach (Trackdata trackdata in this.trackdatas)
                {
                    buffer += "[" + trackdata.ToString() + "],";
                }
                buffer = buffer.TrimEnd(',');
                buffer += "],\"0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000\"]";

                return buffer;
            }

            private struct Userdata
            {
                private string prefix = "\"\",0,0,\"\",0";
                private string timestamp;
                private string unknown1 = "\"10.22\"";
                private string username = "";
                private string userdata;

                public void SetTimestamp(string timestamp)
                {
                    this.timestamp = timestamp;
                }

                public void SetUserdata(string userdata)
                {
                    this.userdata = userdata;
                }

                public Userdata(string serialuserdata)
                {
                    Console.WriteLine("Creating Userdata from string: " + serialuserdata);

                    string[] serialuserdataArray = serialuserdata.Split(",");
                    this.timestamp = serialuserdataArray[5];
                    this.username = serialuserdataArray[7];
                    this.userdata = serialuserdata.Substring(serialuserdata.IndexOf(this.username) + this.username.Length + 1);
                    this.username = this.username.Replace("\"", "");
                }

                public override string ToString()
                {
                    return this.prefix + "," + this.timestamp + "," + this.unknown1 + ",\"" + this.username + "\"," + this.userdata;
                }
            }

            private struct Costumedata
            {
                private int costumeid;
                private int unknown1;
                private int unknown2;
                private int unknown3;
                private int unknown4;

                public Costumedata(string serialcostumedata)
                {
                    Console.WriteLine("Creating Costumedata from string: " + serialcostumedata);

                    string[] serialcostumedataArray = serialcostumedata.Split(",");
                    this.costumeid = int.Parse(serialcostumedataArray[0]);
                    this.unknown1 = int.Parse(serialcostumedataArray[1]);
                    this.unknown2 = int.Parse(serialcostumedataArray[2]);
                    this.unknown3 = int.Parse(serialcostumedataArray[3]);
                    this.unknown4 = int.Parse(serialcostumedataArray[4]);
                }

                public int GetId() { return this.costumeid; }

                public override string ToString()
                {
                    return this.costumeid + "," + this.unknown1 + "," + this.unknown2 + "," + this.unknown3 + "," + this.unknown4;
                }
            }

            private struct Kartdata
            {
                private int kartid;
                private int unknown1;
                private int unknown2;
                private int unknown3;
                private int unknown4;

                public Kartdata(string serialkartdata)
                {
                    Console.WriteLine("Creating Kartdata from string: " + serialkartdata);

                    string[] serialkartdataArray = serialkartdata.Split(",");
                    this.kartid = int.Parse(serialkartdataArray[0]);
                    this.unknown1 = int.Parse(serialkartdataArray[1]);
                    this.unknown2 = int.Parse(serialkartdataArray[2]);
                    this.unknown3 = int.Parse(serialkartdataArray[3]);
                    this.unknown4 = int.Parse(serialkartdataArray[4]);
                }

                public int GetId() { return this.kartid; }

                public override string ToString()
                {
                    return this.kartid + "," + this.unknown1 + "," + this.unknown2 + "," + this.unknown3 + "," + this.unknown4;
                }
            }

            private struct Itemdata
            {
                private int itemid;
                private int unknown1;
                private int unknown2;

                public Itemdata(string serialitemdata)
                {
                    Console.WriteLine("Creating Itemdata from string: " + serialitemdata);

                    string[] serialitemdataArray = serialitemdata.Split(",");
                    this.itemid = int.Parse(serialitemdataArray[0]);
                    this.unknown1 = int.Parse(serialitemdataArray[1]);
                    this.unknown2 = int.Parse(serialitemdataArray[2]);
                }

                public int GetId() { return this.itemid; }

                public override string ToString()
                {
                    return this.itemid + "," + this.unknown1 + "," + this.unknown2;
                }
            }

            private struct Trackdata
            {
                private int unknown1 = 0;
                private int kartclass;
                private int world;
                private int track;
                private int unknown2 = 0;
                private int bestfinish;
                private int unknown3 = 0;
                private int newlabel;

                public Trackdata(string serialtrackdata)
                {
                    Console.WriteLine("Creating Trackdata from string: " + serialtrackdata);

                    string[] serialtrackdataArray = serialtrackdata.Split(",");
                    this.unknown1 = int.Parse(serialtrackdataArray[0]);
                    this.kartclass = int.Parse(serialtrackdataArray[1]);
                    this.world = int.Parse(serialtrackdataArray[2]);
                    this.track = int.Parse(serialtrackdataArray[3]);
                    this.unknown2 = int.Parse(serialtrackdataArray[4]);
                    this.bestfinish = int.Parse(serialtrackdataArray[5]);
                    this.unknown3 = int.Parse(serialtrackdataArray[6]);
                    this.newlabel = int.Parse(serialtrackdataArray[7]);
                }

                public void SetBestfinish(int setbestfinish)
                {
                    Console.WriteLine("Setting track bestfinish: " + setbestfinish);
                    this.bestfinish = setbestfinish;
                }

                public int GetBestfinish()
                {
                    return this.bestfinish;
                }

                public void setNewlabel(int setnewlabel)
                {
                    Console.WriteLine("Setting track newlabel: " + setnewlabel);
                    this.newlabel = setnewlabel;
                }

                public string GetCompositeId() { return this.kartclass + "," + this.world + "," + this.track; }

                public override string ToString()
                {
                    return this.unknown1 + "," + this.kartclass + "," + this.world + "," + this.track + "," + this.unknown2 + "," + this.bestfinish + "," + this.unknown3 + "," + this.newlabel;
                }
            }
        }
    }
    internal class MKGPDXUserReq
    {
        private MKGPDXUserreq request;

        public string GetUserid() { return this.request.GetUserid(); }
        public string GetTimestamp() { return this.request.GetTimestamp(); }
        public string GetUsername() { return this.request.GetUsername(); }
        public string GetUserdata() { return this.request.GetUserdata(); }

        public MKGPDXUserreq.Costumedata[] GetCostumedatas() { return this.request.GetCostumedatas(); }
        public MKGPDXUserreq.Kartdata[] GetKartdatas() { return this.request.GetKartdatas(); }
        public MKGPDXUserreq.Itemdata[] GetItemdatas() { return this.request.GetItemdatas(); }

        public MKGPDXUserreq.Racedata GetRacedata() { return this.request.GetRacedata(); }
        public MKGPDXUserreq.Resultdata GetResultdata() { return this.request.GetResultdata(); }

        public MKGPDXUserReq(string serialmkgpdxuserdata)
        {
            this.request = new MKGPDXUserreq(serialmkgpdxuserdata);
        }

        public struct MKGPDXUserreq
        {
            private string userid;
            private string unknown1;
            private string timestamp;
            private string unknown2;
            private Userdata userdata;
            private Costumedata[] costumedatas;
            private Kartdata[] kartdatas;
            private Itemdata[] itemdatas;
            private Racedata racedata;
            private Resultdata resultdata;

            public string GetUserid() { return this.userid; }
            public string GetTimestamp() { return this.timestamp; }
            public string GetUsername() { return this.userdata.GetUsername(); }
            public string GetUserdata() { return this.userdata.GetUserdata(); }

            public Costumedata[] GetCostumedatas() { return this.costumedatas; }
            public Kartdata[] GetKartdatas() { return this.kartdatas; }
            public Itemdata[] GetItemdatas() { return this.itemdatas; }

            public Racedata GetRacedata() { return this.racedata; }
            public Resultdata GetResultdata() { return this.resultdata; }


            public MKGPDXUserreq(string serialmkgpdxuserdata)
            {
                Console.WriteLine("Creating MKGPDXUserreq from string: " + serialmkgpdxuserdata);

                Util.EatTo(ref serialmkgpdxuserdata, "[");
                this.userid = serialmkgpdxuserdata.Substring(0, serialmkgpdxuserdata.IndexOf(","));
                Util.EatTo(ref serialmkgpdxuserdata, ",");
                this.unknown1 = serialmkgpdxuserdata.Substring(0, serialmkgpdxuserdata.IndexOf(","));
                Util.EatTo(ref serialmkgpdxuserdata, ",");
                this.timestamp = serialmkgpdxuserdata.Substring(0, serialmkgpdxuserdata.IndexOf(","));
                Util.EatTo(ref serialmkgpdxuserdata, ",");
                this.unknown2 = serialmkgpdxuserdata.Substring(0, serialmkgpdxuserdata.IndexOf(","));
                Util.EatTo(ref serialmkgpdxuserdata, ",[");
                this.userdata = new Userdata(serialmkgpdxuserdata.Substring(0, serialmkgpdxuserdata.IndexOf(",\"JPN\"]")));

                Util.EatTo(ref serialmkgpdxuserdata, "],[[");
                if (serialmkgpdxuserdata.StartsWith("["))
                {
                    String[] costumedatastrings = serialmkgpdxuserdata.Substring(0, serialmkgpdxuserdata.IndexOf("]],[")).Split("],[");
                    this.costumedatas = new Costumedata[costumedatastrings.Length];
                    int i = 0;
                    foreach (string costumedatastring in costumedatastrings)
                    {
                        costumedatas[i] = new Costumedata(costumedatastring.TrimStart('[').TrimEnd(']'));
                        i++;
                    }
                    Util.EatTo(ref serialmkgpdxuserdata, "]],[");
                } else
                {
                    this.costumedatas = new Costumedata[0];
                    Util.EatTo(ref serialmkgpdxuserdata, "],[");
                }

                if (serialmkgpdxuserdata.StartsWith("["))
                {
                    String[] kartdatastrings = serialmkgpdxuserdata.Substring(0, serialmkgpdxuserdata.IndexOf("]],[")).Split("],[");
                    this.kartdatas = new Kartdata[kartdatastrings.Length];
                    int j = 0;
                    foreach (string kartdatastring in kartdatastrings)
                    {
                        kartdatas[j] = new Kartdata(kartdatastring.TrimStart('[').TrimEnd(']'));
                        j++;
                    }
                    Util.EatTo(ref serialmkgpdxuserdata, "]],[");
                }
                else
                {
                    this.kartdatas = new Kartdata[0];
                    Util.EatTo(ref serialmkgpdxuserdata, "],[");
                }

                if (serialmkgpdxuserdata.StartsWith("["))
                {
                    String[] itemdatastrings = serialmkgpdxuserdata.Substring(0, serialmkgpdxuserdata.IndexOf("]],")).Split("],[");
                    this.itemdatas = new Itemdata[itemdatastrings.Length];
                    int k = 0;
                    foreach (string itemdatastring in itemdatastrings)
                    {
                        itemdatas[k] = new Itemdata(itemdatastring.TrimStart('[').TrimEnd(']'));
                        k++;
                    }
                }
                else
                {
                    this.itemdatas = new Itemdata[0];
                }

                Util.EatTo(ref serialmkgpdxuserdata, "],1,1,1,0],");
                String racedatastring = serialmkgpdxuserdata.Substring(0, serialmkgpdxuserdata.IndexOf("],["));
                this.racedata = new Racedata(racedatastring.TrimStart('[').TrimEnd(']'));

                Util.EatTo(ref serialmkgpdxuserdata, "],");
                String resultdatastring = serialmkgpdxuserdata.Substring(0, serialmkgpdxuserdata.IndexOf("],["));
                this.resultdata = new Resultdata(resultdatastring.TrimStart('[').TrimEnd(']'));
            }

            public struct Userdata
            {
                private string prefix = "\"\",0,0,\"\",0,";
                private string username = "";
                private string userdata;

                public string GetUsername() { return this.username; }

                public string GetUserdata() { return this.userdata; }

                public Userdata(string serialuserdata)
                {
                    Console.WriteLine("Creating request Userdata from string: " + serialuserdata);

                    string[] serialuserdataArray = serialuserdata.Split(",");
                    this.username = serialuserdataArray[5];
                    this.userdata = serialuserdata.Substring(serialuserdata.IndexOf(this.username) + this.username.Length + 1);
                    this.username = this.username.Replace("\"", "");
                }
            }

            public struct Costumedata
            {
                private int costumeid;
                private int unknown1;
                private int unknown2;
                private int unknown3;
                private int unknown4;

                public int GetCostumeid() { return this.costumeid; }

                public Costumedata(string serialcostumedata)
                {
                    Console.WriteLine("Creating request Costumedata from string: " + serialcostumedata);

                    string[] serialcostumedataArray = serialcostumedata.Split(",");
                    this.costumeid = int.Parse(serialcostumedataArray[0]);
                    this.unknown1 = int.Parse(serialcostumedataArray[1]);
                    this.unknown2 = int.Parse(serialcostumedataArray[2]);
                    this.unknown3 = int.Parse(serialcostumedataArray[3]);
                    this.unknown4 = int.Parse(serialcostumedataArray[4]);
                }
            }

            public struct Kartdata
            {
                private int kartid;
                private int unknown1;
                private int unknown2;

                public int GetKartid() { return this.kartid; }

                public Kartdata(string serialkartdata)
                {
                    Console.WriteLine("Creating request Kartdata from string: " + serialkartdata);

                    string[] serialkartdataArray = serialkartdata.Split(",");
                    this.kartid = int.Parse(serialkartdataArray[0]);
                    this.unknown1 = int.Parse(serialkartdataArray[1]);
                    this.unknown2 = int.Parse(serialkartdataArray[2]);
                }
            }

            public struct Itemdata
            {
                private int itemid;
                private int unknown1;

                public int GetItemid() { return this.itemid; }

                public Itemdata(string serialitemdata)
                {
                    Console.WriteLine("Creating request Itemdata from string: " + serialitemdata);

                    string[] serialitemdataArray = serialitemdata.Split(",");
                    this.itemid = int.Parse(serialitemdataArray[0]);
                    this.unknown1 = int.Parse(serialitemdataArray[1]);
                }
            }

            public struct Racedata
            {
                private int unknown1 = 0;
                private int unknown2 = 0;
                private int kartclass;
                private int world;
                private int track;
                private int unknown3 = 0;

                public string GetCompositeid() { return this.kartclass + "," + this.world + "," + this.track; }

                public Racedata(string serialtrackdata)
                {
                    Console.WriteLine("Creating request Racedata from string: " + serialtrackdata);

                    string[] serialtrackdataArray = serialtrackdata.Split(",");
                    this.unknown1 = int.Parse(serialtrackdataArray[0]);
                    this.unknown2 = int.Parse(serialtrackdataArray[1]);
                    this.kartclass = int.Parse(serialtrackdataArray[2]);
                    this.world = int.Parse(serialtrackdataArray[3]);
                    this.track = int.Parse(serialtrackdataArray[4]);
                    this.unknown3 = int.Parse(serialtrackdataArray[5]);
                }
            }

            public struct Resultdata
            {
                private int finishpos = 0;
                private int unknown1 = 0;
                private int unknown2 = 0;
                private int kartclass;
                private int world;
                private int track;
                private int unknown3 = 0;

                public int GetFinishpos() { return finishpos; }

                public string GetCompositeid() { return this.kartclass + "," + this.world + "," + this.track; }

                public Resultdata(string serialtrackdata)
                {
                    Console.WriteLine("Creating request Resultdata from string: " + serialtrackdata);

                    string[] serialtrackdataArray = serialtrackdata.Split(",");
                    this.finishpos = int.Parse(serialtrackdataArray[0]);
                    this.unknown1 = int.Parse(serialtrackdataArray[1]);
                    this.unknown2 = int.Parse(serialtrackdataArray[2]);
                    this.kartclass = int.Parse(serialtrackdataArray[3]);
                    this.world = int.Parse(serialtrackdataArray[4]);
                    this.track = int.Parse(serialtrackdataArray[5]);
                    this.unknown3 = int.Parse(serialtrackdataArray[6]);
                }
            }
        }
    }
}
