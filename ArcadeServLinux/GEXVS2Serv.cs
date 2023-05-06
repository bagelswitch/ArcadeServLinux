namespace GEXVS2Serv
{
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Net.Http.Headers;
    using System.Text;
    using Util;
    using System.Net.Http.Json;
    using System.Net.Http;

    class GEXVS2WebServer
    {
        private TcpListener exvs2Listener;
        private TcpListener mmListener;
        private static IPAddress localAddr = IPAddress.Parse("0.0.0.0");
        private int EXVS2port = 7820; // EXVS2 port   
        private int MMport = 7821; // EXVS2 MM port

        // TP proxy client
        private static HttpClient tpProxyClient;

        public GEXVS2WebServer()
        {
            try
            {
                exvs2Listener = new TcpListener(localAddr, EXVS2port);
                exvs2Listener.Start();

                mmListener = new TcpListener(localAddr, MMport);
                mmListener.Start();

                Console.WriteLine("GEXVS2 API Server Running on {0}:{1}", localAddr.ToString(), EXVS2port);
                Console.WriteLine("GEXVS2 MM Server Running on {0}:{1}", localAddr.ToString(), MMport);

                //start the EXVS2 thread
                Thread thEXVS2 = new Thread(new ThreadStart(StartEXVS2Listen));
                thEXVS2.Start();

                //start the MM thread
                Thread thMM = new Thread(new ThreadStart(StartMMListen));
                thMM.Start();

                var handler = new HttpClientHandler();
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.ServerCertificateCustomValidationCallback =
                    (httpRequestMessage, cert, cetChain, policyErrors) =>
                    {
                        return true;
                    };

                tpProxyClient = new(handler)
                {
                    BaseAddress = new Uri("https://74.234.107.148:7820"),
                    Timeout = TimeSpan.FromSeconds(10),
                };

                // set request headers for proxied call to TP server
                tpProxyClient.DefaultRequestHeaders.Add("Host", "tpserv.northeurope.cloudapp.azure.com");
                tpProxyClient.DefaultRequestHeaders.Add("Accept", "*/*");
                tpProxyClient.DefaultRequestHeaders.Add("X-Nue-Protobuf-Revision", "33");
            }
            catch (Exception e)
            {
                Console.WriteLine("An Exception Occurred while Listening :" + e.ToString());
            }
            Thread.Sleep(1000);
        }

        public void SendEXVS2Header(int iTotBytes, ref Socket mySocket)
        {
            String sBuffer = "";
            sBuffer = sBuffer + "HTTP/1.1 200 OK\r\n";
            sBuffer = sBuffer + "Content-Type: application/octet-stream\r\n";
            sBuffer = sBuffer + "Content-Length: " + iTotBytes + "\r\n";
            sBuffer = sBuffer + "Connection: close\r\n\r\n";
            Byte[] bSendData = Encoding.ASCII.GetBytes(sBuffer);
            Util.SendToClient(bSendData, bSendData.Length, ref mySocket);
            Console.WriteLine("Header bytes sent: " + bSendData.Length);
        }

        public void StartEXVS2Listen()
        {
            String reqBody;
            String sMethod;

            String sResponse = "";
            while (true)
            {
                //Accept a new connection  
                Socket mySocket = exvs2Listener.AcceptSocket();
                Console.WriteLine("Socket Type " + mySocket.SocketType);
                if (mySocket.Connected)
                {
                    try
                    {
                        Console.WriteLine("\nGEXVS2 client Connected!!\n==================\nCLient IP " + mySocket.RemoteEndPoint.ToString() + "\n");
                        //make a byte array and receive data from the client   
                        Byte[] bReceive = new Byte[4096];
                        int i = mySocket.Receive(bReceive, bReceive.Length, 0);

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

                        //Convert Byte to String  
                        string sBuffer = Encoding.ASCII.GetString(bReceive);
                        // Determine target method
                        reqBody = sBuffer.Substring(headerend + 4);
                        sMethod = reqBody.Substring(reqBody.IndexOf("MTHD"), reqBody.IndexOf("-") - reqBody.IndexOf("MTHD"));

                        Console.WriteLine("\nRequested method " + sMethod + "\n");

                        string reqFilename = @"gexvs2data/proxy-" + sMethod + "-request.bin";
                        FileStream fs = new FileStream(reqFilename, FileMode.Create, FileAccess.Write, FileShare.None);
                        BinaryWriter writer = new BinaryWriter(fs);
                        writer.Write(requestBytes);
                        writer.Close();
                        fs.Close();
                        Console.WriteLine("\nWrote proxy request file: " + reqFilename + "\n");

                        byte[] responseBytes = proxyRequest(requestBytes).Result;

                        string resFilename = @"gexvs2data/proxy-" + sMethod + "-response.bin";
                        fs = new FileStream(resFilename, FileMode.Create, FileAccess.Write, FileShare.None);
                        writer = new BinaryWriter(fs);
                        writer.Write(responseBytes);
                        writer.Close();
                        fs.Close();
                        Console.WriteLine("\nWrote proxy response file: " + resFilename + "\n");

                        /*
                        FileStream fs;

                        fs = new FileStream("gexvs2data/" + sMethod + ".bin", FileMode.Open, FileAccess.Read, FileShare.Read);

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
                        */


                        SendEXVS2Header(responseBytes.Length, ref mySocket);
                        Util.SendToClient(responseBytes, responseBytes.Length, ref mySocket);
                        Console.WriteLine("Body bytes sent: {0}", responseBytes.Length);

                        // Console.WriteLine("\nWrote response " + sResponse + "\n");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error Occurred : {0} ", e);
                    }
                }
                mySocket.Close();
            }
        }

        private async Task<byte[]> proxyRequest(byte[] request)
        {
            using ByteArrayContent byteContent = new ByteArrayContent(request);

            byteContent.Headers.Add("Content-type", "application/x-protobuf");

            using HttpResponseMessage response = await tpProxyClient.PostAsync("exvs2", byteContent);

            response.EnsureSuccessStatusCode();

            byte[] responseBytes = response.Content.ReadAsByteArrayAsync().Result;
            
            return responseBytes;
        }

        public void StartMMListen()
        {
            String reqBody;
            String sMethod;

            String sResponse = "";
            while (true)
            {
                //Accept a new connection  
                Socket mySocket = mmListener.AcceptSocket();
                Console.WriteLine("Socket Type " + mySocket.SocketType);
                if (mySocket.Connected)
                {
                    try
                    {
                        Console.WriteLine("\nMM client Connected!!\n==================\nCLient IP " + mySocket.RemoteEndPoint.ToString() + "\n");
                        //make a byte array and receive data from the client   
                        Byte[] bReceive = new Byte[1024];
                        int i = mySocket.Receive(bReceive, bReceive.Length, 0);
                        //Convert Byte to String  
                        string sBuffer = Encoding.ASCII.GetString(bReceive);
                        // Determine target method
                        reqBody = sBuffer.Substring(sBuffer.IndexOf("ISSUE"));
                        sMethod = reqBody.Substring(0, reqBody.IndexOf("-"));

                        Console.WriteLine("\nRequested method " + sMethod + "\n");

                        int iTotBytes = 0;
                        sResponse = "";
                        FileStream fs;

                        fs = new FileStream("gexvs2data/" + sMethod + ".bin", FileMode.Open, FileAccess.Read, FileShare.Read);

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
                        SendEXVS2Header(iTotBytes, ref mySocket);
                        Util.SendToClient(bytes, iTotBytes, ref mySocket);
                        Console.WriteLine("Body bytes sent: {0}", iTotBytes);

                        // Console.WriteLine("\nWrote response " + sResponse + "\n");
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