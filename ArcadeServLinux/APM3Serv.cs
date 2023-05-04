namespace APM3Serv
{
    using System.Net.Sockets;
    using System.Net;
    using System.Text;
    using Util;
    using System.IO.Hashing;

    class APM3WebServer
    {
        // this should come from a config file
        public static string apm3Address = "apm3.teknoparrot.com";

        private TcpListener apm3Listener;
        private TcpListener matchListener;
        private TcpListener aimeListener;
        public static IPAddress localAddr = IPAddress.Parse("0.0.0.0");
        private static int APM3port = 8180; // APM3 port   
        private static int Matchport = 55000; // Match server port   
        private static int AIMEport = 22345; // AIME port 

        public APM3WebServer()
        {
            try
            {
                apm3Listener = new TcpListener(localAddr, APM3port);
                apm3Listener.Start();

                matchListener = new TcpListener(localAddr, Matchport);
                matchListener.Start();

                aimeListener = new TcpListener(localAddr, AIMEport);
                aimeListener.Start();

                Console.WriteLine("APM3 Server Running on {0}:{1}", localAddr.ToString(), APM3port);
                Console.WriteLine("Match Server Running on {0}:{1}", localAddr.ToString(), Matchport);
                Console.WriteLine("AIME Server Running on {0}:{1}", localAddr.ToString(), AIMEport);

                //start the APM3 thread
                Thread thAPM3 = new Thread(new ThreadStart(StartAPM3Listen));
                thAPM3.Start();

                //start the Matchserver thread
                Thread thMatch = new Thread(new ThreadStart(StartMatchListen));
                thMatch.Start();

                //start the AIME thread
                Thread thAIME = new Thread(new ThreadStart(StartAIMEListen));
                thAIME.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("An Exception Occurred while Listening :" + e.ToString());
            }
        }

        public void SendAPM3Header(int iTotBytes, ref Socket mySocket)
        {
            String timestamp = String.Format("{0:r}", DateTime.UtcNow);
            String sBuffer = "";
            sBuffer = sBuffer + "HTTP/1.1 200 OK\r\n";
            sBuffer = sBuffer + "Content-Length: " + iTotBytes + "\r\n";
            sBuffer = sBuffer + "Date: " + timestamp + "\r\n";
            sBuffer = sBuffer + "Context-Type: application/json\r\n";
            sBuffer = sBuffer + "Connection: close\r\n\r\n";

            Console.WriteLine("Sending headers: " + sBuffer);

            Byte[] bSendData = Encoding.ASCII.GetBytes(sBuffer);
            Util.SendToClient(bSendData, bSendData.Length, ref mySocket);
            Console.WriteLine("Header bytes sent: " + sBuffer.Length);
        }

        public void StartMatchListen()
        {
            while (true)
            {
                //Accept a new connection  
                Socket mySocket = matchListener.AcceptSocket();
                Console.WriteLine("\nSocket Type: " + mySocket.SocketType);
                if (mySocket.Connected)
                {
                    try
                    {
                        Console.WriteLine("Matchserver client Connected!!\n===========================\nCLient IP " + mySocket.RemoteEndPoint.ToString());
                        Byte[] bReceive = new Byte[1024];
                        int read = mySocket.Receive(bReceive, bReceive.Length, 0);
                        bReceive = bReceive.Take(read).ToArray();
                        Console.WriteLine("Matchserver input: " + Util.HexBinary(bReceive).ToString());

                        // do nothing . . . I guess . . . ?
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error Occurred : {0} ", e);
                    }
                }
                mySocket.Close();
            }
        }

        public void StartAIMEListen()
        {
            while (true)
            {
                //Accept a new connection  
                Socket mySocket = aimeListener.AcceptSocket();
                Console.WriteLine("\nSocket Type: " + mySocket.SocketType);
                if (mySocket.Connected)
                {
                    try
                    {
                        Console.WriteLine("AIME client Connected!!\n===========================\nCLient IP " + mySocket.RemoteEndPoint.ToString());
                        Byte[] bReceive = new Byte[1024];
                        int read = mySocket.Receive(bReceive, bReceive.Length, 0);
                        bReceive = bReceive.Take(read).ToArray();

                        byte[] requestBytes = Crypto.AES_128_ECB.Decrypt(bReceive, Crypto.StringToByteArray(APM3.aimeKey));
                        string requestString = Util.HexBinary(requestBytes).ToString();
                        Console.WriteLine("AIME input: " + requestString);

                        string gameid = APM3.GetAIMEGameID(requestBytes);
                        string requestType = APM3.GetAIMERequestType(requestBytes);

                        Console.WriteLine("AIME game ID: " + gameid);
                        Console.WriteLine("AIME request type: " + requestType);

                        byte[] responseBytes = Crypto.StringToByteArray(APM3.aimeResponses[gameid][requestType]);
                        string responseString = Util.HexBinary(responseBytes).ToString();
                        Console.WriteLine("AIME output: " + responseString);

                        if (responseBytes.Length > 0)
                        {
                            byte[] response = Crypto.AES_128_ECB.Encrypt(responseBytes, Crypto.StringToByteArray(APM3.aimeKey));
                            Util.SendToClient(response, response.Length, ref mySocket);
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

        public void StartAPM3Listen()
        {
            while (true)
            {
                //Accept a new connection  
                Socket mySocket = apm3Listener.AcceptSocket();
                Console.WriteLine("\nSocket Type " + mySocket.SocketType);
                if (mySocket.Connected)
                {
                    try
                    {
                        Console.WriteLine("APM3 client Connected!!\n=======================\nCLient IP " + mySocket.RemoteEndPoint.ToString());
                        //make a byte array and receive data from the client   
                        Byte[] bReceive = new Byte[4096];
                        mySocket.Receive(bReceive, bReceive.Length, 0);

                        // extract headers from request
                        string headers = Encoding.UTF8.GetString(bReceive);
                        int headerend = headers.IndexOf("\r\n\r\n");
                        headers = headers.Substring(0, headerend + 2);

                        // determine path
                        int pathStart = headers.IndexOf("POST ") + 5;
                        int pathEnd = headers.IndexOf(" HTTP/1.1");
                        string path = headers.Substring(pathStart, pathEnd - pathStart);
                        Console.WriteLine("Requested path " + path);
                        headers = headers.Substring(pathEnd + 11);

                        string user = "";
                        string gameid = "";
                        int len = 0;
                        bool docontinue = false;

                        string[] headerArray = headers.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string header in headerArray)
                        {
                            string[] headerVal = header.Split(new[] { ": " }, StringSplitOptions.RemoveEmptyEntries);
                            if (headerVal[0] == "User-Agent")
                            {
                                // user-agent header identifies title and user
                                user = headerVal[1].ToUpper();
                                user = user.Replace("/", "-");
                                gameid = user.Substring(user.IndexOf("-") + 1, 4);
                                Console.WriteLine("Requested user " + user);
                                Console.WriteLine("Requested gameid " + gameid);
                            }
                            else if (headerVal[0] == "Content-Length")
                            {
                                // get request content length
                                string contentLength = headerVal[1];
                                Console.WriteLine("Request content length " + contentLength);
                                len = Convert.ToInt32(contentLength) - 16;
                            }
                            else if (headerVal[0] == "Expect")
                            {
                                Console.WriteLine("Expect header value: " + headerVal[1]);

                                if (headerVal[1] == "100-continue")
                                {
                                    docontinue = true;
                                }
                            }
                        }

                        if (docontinue)
                        {
                            int read;
                            int pollcount = 0;
                            bReceive = new Byte[102400]; // should poll for chunks, but . . . meh
                            // send continue and get the actual request body
                            Util.SendContinue(ref mySocket);
                            Thread.Sleep(250);
                            while ((read = mySocket.Receive(bReceive, bReceive.Length, 0)) <= 0 && pollcount < 4)
                            {
                                Thread.Sleep(250);
                                pollcount++;
                            }
                            if (read <= 0)
                            {
                                throw new Exception("No body read after continue header sent!");
                            }
                            bReceive = bReceive.Take(read).ToArray();
                        }
                        else
                        {
                            // the remainder of the receive buffer is the request body
                            bReceive = bReceive.Skip(headerend + 4).ToArray();
                        }

                        // first 16 bytes of body are the IV
                        byte[] reqIV = bReceive.Take(16).ToArray();
                        Console.WriteLine("Request IV: " + Util.HexBinary(reqIV).ToString());

                        // remainder of body is the encrypted and compressed payload
                        byte[] encryptedbody = bReceive.Skip(16).Take(len).ToArray();

                        // decrypt and decompress request body
                        byte[] key = Crypto.StringToByteArray(APM3.GetKey(gameid, path));
                        Console.WriteLine("key: " + Util.HexBinary(key).ToString());
                        string body = Crypto.DecryptStringFromBytes_Aes(encryptedbody, key, reqIV);
                        Console.WriteLine("Plaintext body: " + body);

                        string sResponse = "";
                        if (path == "/api/data/save")
                        {
                            GameData.Save(gameid, body);
                            GameData.Close(gameid);
                            sResponse = APM3.GetCannedResponse(gameid, path);
                        }
                        else if (path == "/api/data/load")
                        {
                            sResponse = GameData.Load(gameid, body);
                        }
                        else if (path == "/api/user/save")
                        {
                            UserData.Save(user, gameid, body);
                            UserData.Close(user);
                            sResponse = APM3.GetCannedResponse(gameid, path);
                        }
                        else if (path == "/api/user/load")
                        {
                            sResponse = UserData.Load(user, gameid, body);
                        }
                        else if (path == "/api/ranking/list")
                        {
                            sResponse = UserData.LoadRankings(user, gameid, body);
                        }
                        else
                        {
                            sResponse = APM3.GetCannedResponse(gameid, path);
                        }

                        byte[] bytes = Encoding.UTF8.GetBytes(sResponse);
                        int reslen = bytes.Length;

                        // calculate plaintext CRC for use in response IV
                        byte[] crc32 = Crc32.Hash(bytes);
                        Array.Reverse(crc32);
                        Console.WriteLine("Response CRC32B: " + Util.HexBinary(crc32).ToString());

                        // calculate response IV
                        byte[] IV = APM3.GetIV(gameid, path, crc32, reslen);
                        Console.WriteLine("Response IV: " + Util.HexBinary(IV).ToString());

                        // compress and encrypt the response
                        key = Crypto.StringToByteArray(APM3.GetKey(gameid, path));
                        byte[] response = { };
                        if (APM3.EncryptResponse(path))
                        {
                            response = Crypto.EncryptBytesToBytes_Aes(bytes, key, IV);
                        }
                        int responseLen = IV.Length + response.Length;

                        // send response headers and body
                        SendAPM3Header(responseLen, ref mySocket);
                        Util.SendToClient(IV, IV.Length, ref mySocket);
                        if (reslen > 0)
                        {
                            Util.SendToClient(response, response.Length, ref mySocket);
                        }

                        Console.WriteLine("Body bytes sent: {0}", responseLen);
                        Console.WriteLine("Wrote response " + Encoding.UTF8.GetString(bytes) + "\n");
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
