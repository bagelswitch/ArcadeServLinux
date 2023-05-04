namespace AuthServ
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using Util;

    class AuthWebServer
    {
        private TcpListener authListener;
        private IPAddress localAddr = IPAddress.Parse("0.0.0.0");
        private int Authport = 8080;

        public AuthWebServer()
        {
            try
            {
                //start listing on the given port  
                authListener = new TcpListener(localAddr, Authport);
                authListener.Start();
                Console.WriteLine("Auth Server Running on {0}:{1}", localAddr.ToString(), Authport);

                //start the thread which calls the method 'StartListen'  
                Thread th = new Thread(new ThreadStart(StartAuthListen));
                th.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("An Exception Occurred while Listening :" + e.ToString());
            }
        }

        public void SendAuthHeader(int iTotBytes, ref Socket mySocket)
        {
            String sBuffer = "";
            sBuffer = sBuffer + "HTTP/1.1 200 OK\r\n";
            sBuffer = sBuffer + "Content-Type: text/html; charset=UTF-8\r\n";
            sBuffer = sBuffer + "Content-Length: " + iTotBytes + "\r\n\r\n";
            Byte[] bSendData = Encoding.ASCII.GetBytes(sBuffer);
            Util.SendToClient(bSendData, bSendData.Length, ref mySocket);
            Console.WriteLine("Header bytes sent: " + bSendData.Length.ToString());
        }

        public void StartAuthListen()
        {
            while (true)
            {
                //Accept a new connection  
                Socket mySocket = authListener.AcceptSocket();
                Console.WriteLine("\nSocket Type " + mySocket.SocketType);
                if (mySocket.Connected)
                {
                    try
                    {
                        Console.WriteLine("Auth client Connected!!\n=======================\nCLient IP " + mySocket.RemoteEndPoint.ToString());
                        //make a byte array and receive data from the client   
                        Byte[] bReceive = new Byte[4096];
                        mySocket.Receive(bReceive, bReceive.Length, 0);

                        // extract headers from request
                        string headers = Encoding.UTF8.GetString(bReceive);
                        int headerend = headers.IndexOf("\r\n\r\n");
                        headers = headers.Substring(0, headerend + 2);

                        // determine path
                        int pathStart = headers.IndexOf("GET ") + 5;
                        int pathEnd = headers.IndexOf(" HTTP/1.1");
                        string path = headers.Substring(pathStart, pathEnd - pathStart);
                        Console.WriteLine("Requested path " + path);
                        headers = headers.Substring(pathEnd + 11);

                        string code = "";
                        string hwid = "";
                        string hash = "";

                        string reqparams = path.Substring(path.IndexOf("?") + 1);
                        string[] reqparamsArray = reqparams.Split(new[] { "&" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string reqparam in reqparamsArray)
                        {
                            string[] reqparamVal = reqparam.Split(new[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                            if (reqparamVal[0] == "code")
                            {
                                code = WebUtility.UrlDecode(reqparamVal[1]);
                                Console.WriteLine("Request code " + code);
                            }
                            else if (reqparamVal[0] == "hwid")
                            {
                                hwid = reqparamVal[1];
                                Console.WriteLine("Request hwid " + hwid);
                            }
                            else if (reqparamVal[0] == "hash")
                            {
                                hash = WebUtility.UrlDecode(reqparamVal[1]);
                                Console.WriteLine("Request hash " + hash);
                            }
                        }

                        int iTotBytes = 0;
                        string sResponse = "";
                        FileStream fs = new FileStream("authdata/" + hwid + ".bin", FileMode.Open, FileAccess.Read, FileShare.Read);
                        // Create a reader that can read bytes from the FileStream.  
                        BinaryReader reader = new BinaryReader(fs);
                        byte[] bytes = new byte[fs.Length];
                        int read;
                        while ((read = reader.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            // Read from the file and write the data to the network  
                            sResponse = sResponse + Encoding.UTF8.GetString(bytes, 0, read);
                            iTotBytes = iTotBytes + read;
                        }
                        reader.Close();
                        fs.Close();
                        SendAuthHeader(iTotBytes, ref mySocket);
                        Util.SendToClient(bytes, iTotBytes, ref mySocket);

                        Console.WriteLine("\nWrote response " + sResponse + "\n");

                        try
                        {
                            // response body is Base64 encoded . . .
                            sResponse = sResponse.Substring(sResponse.IndexOf("\n") + 1);
                            byte[] decoded = System.Convert.FromBase64String(sResponse);
                            string decodedString = Util.HexBinary(decoded).ToString();
                            Console.WriteLine("\nDecoded response " + decodedString + "\n");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Error Occurred : {0} ", e);
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
