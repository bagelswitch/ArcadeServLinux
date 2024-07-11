namespace NAOMIServ
{
    using System.Diagnostics.Metrics;
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Xml.Linq;
    using Ionic.Zlib;
    using Util;
    using static System.Net.WebRequestMethods;

    class NAOMIWebServer
    {
        // this should come from a config file
        public static string apm3Address = "apm3.teknoparrot.com";
        public static string apm3Protocol = "http";

        public static string gundamAddress = "gundam.teknoparrot.com"; 
        public static string gundamProtocol = "http";

        public static string chronoAddress = "chrono.teknoparrot.com"; // "213.186.231.196"; // "74.234.107.148";
        public static string chronoProtocol = "http";
        public static string chronoPort = "9004";

        public static string idacAddress = "idac.teknoparrot.com";
        public static string idacProtocol = "https";
        public static string idacPort = "443";

        private TcpListener naomiListener;
        public static IPAddress localAddr = IPAddress.Parse("0.0.0.0"); // loopback may not work, might need i/f address
        private static int NAOMINETport = 9876; // naominet.jp port 

        public NAOMIWebServer()
        {
            try
            {
                naomiListener = new TcpListener(localAddr, NAOMINETport);
                naomiListener.Start();

                Console.WriteLine("NAOMINET Server Running on {0}:{1}", localAddr.ToString(), NAOMINETport);

                //start the naominet.jp thread
                Thread thNAOMI = new Thread(new ThreadStart(StartNAOMIListen));
                thNAOMI.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("An Exception Occurred while Listening :" + e.ToString());
            }
        }

        public void SendPowerOnHeader(int iTotBytes, ref Socket mySocket)
        {
            String timestamp = String.Format("{0:r}", DateTime.UtcNow);
            String sBuffer = "";
            sBuffer = sBuffer + "HTTP/1.1 200 OK\r\n";
            sBuffer = sBuffer + "X-Powered-By: Bagels\r\n"; // probably not necessary
            sBuffer = sBuffer + "Content-Type: text/plain; charset=utf-8\r\n";
            sBuffer = sBuffer + "Content-Length: " + iTotBytes + "\r\n";
            sBuffer = sBuffer + "Date: " + timestamp + "\r\n";
            sBuffer = sBuffer + "Connection: close\r\n\r\n";

            Console.WriteLine("Sending headers: " + sBuffer);

            Byte[] bSendData = Encoding.ASCII.GetBytes(sBuffer);
            Util.SendToClient(bSendData, bSendData.Length, ref mySocket);
            Console.WriteLine("Header bytes sent: " + sBuffer.Length);
        }

        public void StartNAOMIListen()
        {
            while (true)
            {
                //Accept a new connection  
                Socket mySocket = naomiListener.AcceptSocket();
                Console.WriteLine("\nSocket Type: " + mySocket.SocketType);
                if (mySocket.Connected)
                {
                    try
                    {
                        Console.WriteLine("Naominet client Connected!!\n===========================\nCLient IP " + mySocket.RemoteEndPoint.ToString());
                        Byte[] bReceive = new Byte[4096];
                        mySocket.Receive(bReceive, bReceive.Length, 0);

                        // extract headers from request
                        string headers = Encoding.UTF8.GetString(bReceive);
                        int headerend = headers.IndexOf("\r\n\r\n");
                        headers = headers.Substring(0, headerend + 2);

                        int requestLen = 0;
                        string[] headerArray = headers.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string header in headerArray)
                        {
                            string[] headerVal = header.Split(new[] { ": " }, StringSplitOptions.RemoveEmptyEntries);
                            if (headerVal[0] == "Content-Length")
                            {
                                // get request content length
                                string contentLength = headerVal[1];
                                Console.WriteLine("Request content length " + contentLength);
                                requestLen = Convert.ToInt32(contentLength);
                            }
                        }
                        byte[] requestBytes = bReceive.Skip(headerend + 4).Take(requestLen).ToArray();
                        string bodyEncoded = Encoding.ASCII.GetString(requestBytes);

                        Console.WriteLine("Request body encoded: " + bodyEncoded + "<end>");

                        if (headers.IndexOf("PowerOn") >= 0)
                        {
                            // request body is Base64 encoded . . .
                            byte[] bodyZipped = System.Convert.FromBase64String(bodyEncoded);
                            // . . . and zlib compressed
                            string body;
                            using (MemoryStream ms = new MemoryStream(bodyZipped))
                            {
                                using (ZlibStream zs = new ZlibStream(ms, Ionic.Zlib.CompressionMode.Decompress))
                                {
                                    using (StreamReader sr = new StreamReader(zs))
                                    {
                                        body = sr.ReadToEnd().Trim();
                                    }
                                }
                            }

                            Console.WriteLine("Request body: " + body);

                            string gameid = "";
                            string token = "";
                            string[] reqparamsArray = body.Split(new[] { "&" }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (string reqparam in reqparamsArray)
                            {
                                string[] reqparamVal = reqparam.Split(new[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                                if (reqparamVal[0] == "game_id")
                                {
                                    gameid = WebUtility.UrlDecode(reqparamVal[1]);
                                    Console.WriteLine("Request gameid " + gameid);
                                }
                                else if (reqparamVal[0] == "token")
                                {
                                    token = reqparamVal[1];
                                    Console.WriteLine("Request token " + token);
                                }
                            }

                            string responseString = "";
                            if (gameid == "SBUZ")
                            {
                                // Gundam EX VS 2
                                //responseString = "stat=1&host=&name=Bagels&place_id=1234&nickname=Bagels&region0=1&setting=1&country=JPN&timezone=+09:00&res_class=PowerOnResponseVer2&uri=http://" + gundamAddress + ":7820/exvs2&region_name0=W&region_name1=X&region_name2=Y&region_name3=Z&year=2023&month=3&day=28&hour=0&minute=41&second=36";
                                //responseString = "stat=1&host=" + gundamAddress + "&name=Bagels&place_id=1234&nickname=Bagels&region0=1&setting=1&country=JPN&timezone=+09:00&res_class=PowerOnResponseVer2&uri=" + gundamProtocol + "://" + gundamAddress + ":7820/exvs2&region_name0=W&region_name1=X&region_name2=Y&region_name3=Z&year=2023&month=5&day=5&hour=11&minute=26&second=2";
                                responseString = "stat=1&host=" + gundamAddress + "&name=Bagels&place_id=1234&nickname=Bagels&region0=1&setting=1&country=JPN&timezone=+09:00&res_class=PowerOnResponseVer2&uri=" + gundamProtocol + "://" + gundamAddress + ":7820/exvs2xb&region_name0=W&region_name1=X&region_name2=Y&region_name3=Z&year=2023&month=7&day=28&hour=1&minute=43&second=52";
                            }
                            else if (gameid == "SDEC")
                            {
                                // Chrono Regalia
                                string pre = "stat=1&uri=" + chronoProtocol + "://" + chronoAddress + ":" + chronoPort + "/chrono&host=" + chronoAddress + ":" + chronoPort + "&place_id=123&name=TeknoParrot&nickname=TeknoParrot&region0=1&region_name0=Neo Tokyo&region_name1=X&region_name2=Y&region_name3=Z&country=JPN&allnet_id=456&client_timezone=+0900&utc_time=";
                                string post = "Z&setting=&res_ver=3&token=";
                                responseString = pre + String.Format("{0:s}", DateTime.UtcNow) + post + token + "\n";
                            }
                            else if (gameid == "SDGT")
                            {
                                // Initial D the Arcade
                                string pre = "stat=1&uri=" + idacProtocol + "://" + idacAddress + ":" + idacPort + "/idac&host=" + idacAddress + ":" + idacPort + "&place_id=123&name=TeknoParrot&nickname=TeknoParrot&region0=1&region_name0=Neo Tokyo&region_name1=X&region_name2=Y&region_name3=Z&country=JPN&allnet_id=456&client_timezone=+0900&utc_time=";
                                string post = "Z&setting=&res_ver=3&token=";
                                responseString = pre + String.Format("{0:s}", DateTime.UtcNow) + post + token + "\n";
                            }
                            else
                            {
                                // APM3/SWDC
                                String pre = "stat=1&uri=" + apm3Protocol + "://" + apm3Address + "&host=" + apm3Address + "&place_id=123&name=Bagels&nickname=Bagels&region0=1&region_name0=W&region_name1=X&region_name2=Y&region_name3=Z&country=JPN&allnet_id=456&client_timezone=+0900&utc_time=";
                                String post = "Z&setting=&res_ver=3&token=";
                                responseString = pre + String.Format("{0:s}", DateTime.UtcNow) + post + token + "\n";
                            }
                            int iTotBytes = responseString.Length;
                            SendPowerOnHeader(iTotBytes, ref mySocket);
                            Console.WriteLine("Sending response: {0}", responseString);
                            Util.SendToClient(responseString, ref mySocket);
                            Console.WriteLine("Response bytes sent: {0}", iTotBytes);
                        }
                        else
                        {
                            Console.WriteLine("Unknown request");
                            mySocket.Close();
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error Occurred : {0} ", e);
                    }
                }
                mySocket.Close();
            }
        } 
    }
}
