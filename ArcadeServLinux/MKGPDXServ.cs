namespace MKGPDXServ
{
    using System.Net.Sockets;
    using System.Net;
    using System.Text;
    using Util;

    class MKGPDXWebServer
    {
        private TcpListener mkgpdxListener;
        public static IPAddress localAddr = IPAddress.Parse("0.0.0.0");
        private static int MKGPDXport = 49200; // MKGPDX port   

        public MKGPDXWebServer()
        {
            try
            {
                mkgpdxListener = new TcpListener(localAddr, MKGPDXport);
                mkgpdxListener.Start();

                Console.WriteLine("MKGPDX Server Running on {0}:{1}", localAddr.ToString(), MKGPDXport);

                //start the APM3 thread
                Thread thMKGPDX = new Thread(new ThreadStart(StartMKGPDXListen));
                thMKGPDX.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("An Exception Occurred while Listening :" + e.ToString());
            }
        }

        public void SendMKGPDXHeader(int iTotBytes, ref Socket mySocket)
        {
            String timestamp = String.Format("{0:r}", DateTime.UtcNow);
            String sBuffer = "";
            sBuffer = sBuffer + "HTTP/1.1 200 OK\r\n";
            sBuffer = sBuffer + "Content-Type: application/json; charset=utf-8\r\n";
            sBuffer = sBuffer + "Date: " + timestamp + "\r\n";
            sBuffer = sBuffer + "Server: Bagels\r\n";
            sBuffer = sBuffer + "Content-Length: " + iTotBytes + "\r\n";
            sBuffer = sBuffer + "Connection: close\r\n\r\n";

            Console.WriteLine("Sending headers: " + sBuffer);

            Byte[] bSendData = Encoding.ASCII.GetBytes(sBuffer);
            Util.SendToClient(bSendData, bSendData.Length, ref mySocket);
            Console.WriteLine("Header bytes sent: " + sBuffer.Length);
        }

        public void StartMKGPDXListen()
        {
            while (true)
            {
                //Accept a new connection  
                Socket mySocket = mkgpdxListener.AcceptSocket();
                Console.WriteLine("\nSocket Type " + mySocket.SocketType);
                if (mySocket.Connected)
                {
                    try
                    {
                        Console.WriteLine("MKGPDX client Connected!!\n=======================\nCLient IP " + mySocket.RemoteEndPoint.ToString());
                        //make a byte array and receive data from the client   
                        Byte[] bReceive = new Byte[4096];
                        // add a delay before reading from socket to avoid incomplete post data, maybe?
                        Thread.Sleep(50);
                        mySocket.Receive(bReceive, bReceive.Length, 0);

                        // extract headers from request
                        string headers = Encoding.UTF8.GetString(bReceive);
                        int headerend = headers.IndexOf("\r\n\r\n");
                        headers = headers.Substring(0, headerend + 2);

                        // determine path
                        int pathStart = headers.IndexOf("POST ") + 5;
                        int pathEnd = headers.IndexOf(" HTTP/1.1");
                        string path = headers.Substring(pathStart, pathEnd - pathStart);
                        headers = headers.Substring(pathEnd + 11);

                        bool docontinue = false;
                        int len = 0;
                        string userid = path.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries)[0];
                        Console.WriteLine("Requested userid " + userid);
                        path = path.Substring(path.IndexOf(userid) + userid.Length);
                        Console.WriteLine("Requested path " + path);

                        string[] headerArray = headers.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string header in headerArray)
                        {
                            string[] headerVal = header.Split(new[] { ": " }, StringSplitOptions.RemoveEmptyEntries);
                            if (headerVal[0] == "Content-Length")
                            {
                                // get request content length
                                string contentLength = headerVal[1];
                                Console.WriteLine("Request content length " + contentLength);
                                len = Convert.ToInt32(contentLength);
                            }
                            else if (headerVal[0] == "Expect")
                            {
                                Console.WriteLine("Expect header value: " + headerVal[1]);

                                if (headerVal[1] == "100-continue")
                                {
                                    docontinue = true;
                                }
                            }
                            else
                            {
                                Console.WriteLine("Unknown header name: " + headerVal[0] + " with value: " + headerVal[1]);
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

                        string body = Encoding.UTF8.GetString(bReceive.Take(len).ToArray());
                        Console.WriteLine("Request body: " + body);

                        string sResponse = "";
                        if (path == "/player/saveData")
                        {
                            MKGPDX.Save(userid, body);
                            MKGPDX.Close(userid);
                            sResponse = MKGPDX.GetCannedResponse(userid, path);
                        }
                        else if (path == "/player/saveDataRe")
                        {
                            MKGPDX.Save(userid, body);
                            MKGPDX.Close(userid);
                            sResponse = MKGPDX.GetCannedResponse(userid, path);
                        }
                        else if (path == "/player/getData")
                        {
                            sResponse = MKGPDX.Load(userid);
                        }
                        else if (path == "/player/getDataRe")
                        {
                            sResponse = MKGPDX.Load(userid);
                        }
                        else
                        {
                            sResponse = MKGPDX.GetCannedResponse(userid, path);
                        }

                        byte[] bytes = Encoding.UTF8.GetBytes(sResponse);
                        int reslen = bytes.Length;

                        // send response headers and body
                        SendMKGPDXHeader(reslen, ref mySocket);
                        if (reslen > 0)
                        {
                            Util.SendToClient(bytes, bytes.Length, ref mySocket);
                        }

                        Console.WriteLine("Body bytes sent: {0}", reslen);
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
