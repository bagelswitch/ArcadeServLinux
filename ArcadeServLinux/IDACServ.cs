namespace IDACServ
{
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Net.Http.Headers;
    using System.Text;
    using Util;
    using System.Net.Http.Json;
    using System.Net.Http;
    using System;
    using APM3Serv;

    class IDACWebServer
    {
        private TcpListener idacListener;
        private TcpListener mmListener;
        private static IPAddress localAddr = IPAddress.Parse("0.0.0.0");
        private int IDACport = 10124; // IDAC port   
        private int MMport = 10125; // IDAC MM port

        // TP proxy client
        private static HttpClient tpProxyClient;

        public IDACWebServer()
        {
            try
            {
                idacListener = new TcpListener(localAddr, IDACport);
                idacListener.Start();

                mmListener = new TcpListener(localAddr, MMport);
                mmListener.Start();

                Console.WriteLine("IDAC API Server Running on {0}:{1}", localAddr.ToString(), IDACport);
                Console.WriteLine("IDAC MM Server Running on {0}:{1}", localAddr.ToString(), MMport);

                //start the IDAC thread
                Thread thIDAC = new Thread(new ThreadStart(StartIDACListen));
                thIDAC.Start();

                //start the MM thread
                Thread thMM = new Thread(new ThreadStart(StartMMListen));
                thMM.Start();

                var handler = new HttpClientHandler()
                {
                    AutomaticDecompression = DecompressionMethods.GZip
                };
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.ServerCertificateCustomValidationCallback =
                    (httpRequestMessage, cert, cetChain, policyErrors) =>
                    {
                        return true;
                    };

                tpProxyClient = new(handler)
                {
                    BaseAddress = new Uri("https://kelvin-idac.local:10124"),
                    Timeout = TimeSpan.FromSeconds(10),
                };

                // set request headers for proxied call to TP server
                tpProxyClient.DefaultRequestHeaders.Add("Host", "kelvin-idac.local:10124");
                tpProxyClient.DefaultRequestHeaders.Add("Accept", "*/*");
                tpProxyClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
                tpProxyClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");
                tpProxyClient.DefaultRequestHeaders.Add("User-Agent", "GameProject/++UE4+Release-4.24-CL-0 Windows/10.0.14393.1.768.64bit");
            }
            catch (Exception e)
            {
                Console.WriteLine("An Exception Occurred while Listening :" + e.ToString());
            }
            Thread.Sleep(1000);
        }

        public void SendIDACHeader(int iTotBytes, ref Socket mySocket)
        {
            String sBuffer = "";
            sBuffer = sBuffer + "HTTP/1.1 200 OK\r\n";
            sBuffer = sBuffer + "Content-Type: application/json; charset=utf-8\r\n";
            sBuffer = sBuffer + "Content-Length: " + iTotBytes + "\r\n";
            sBuffer = sBuffer + "Connection: close\r\n\r\n";
            Byte[] bSendData = Encoding.ASCII.GetBytes(sBuffer);
            Util.SendToClient(bSendData, bSendData.Length, ref mySocket);
            Console.WriteLine("Header bytes sent: " + bSendData.Length);
        }

        public void StartIDACListen()
        {
            String sMethod;
            string appHeader = "timestamp=1720663897, a_serail=\"A64E01A0622\", a_store=\"224\", a_country=\"19\", a_region=\"80\", device_version=\"2.31.00\", asset_version=\"0\", a_als=\"1\"";

            while (true)
            {
                //Accept a new connection  
                Socket mySocket = idacListener.AcceptSocket();
                Console.WriteLine("Socket Type " + mySocket.SocketType);
                if (mySocket.Connected)
                {
                    try
                    {
                        Console.WriteLine("\nIDAC client Connected!!\n==================\nCLient IP " + mySocket.RemoteEndPoint.ToString() + "\n");
                        //make a byte array and receive data from the client   
                        Byte[] bReceive = new Byte[32768];
                        // add a delay before reading from socket to avoid incomplete post data, maybe?
                        Thread.Sleep(50);
                        int i = mySocket.Receive(bReceive, bReceive.Length, 0);

                        //string sRequest = Encoding.ASCII.GetString(bReceive);
                        //Console.WriteLine("\nComplete Request: " + sRequest + "\n");

                        // extract headers from request
                        string headers = Encoding.UTF8.GetString(bReceive);
                        int headerend = headers.IndexOf("\r\n\r\n");
                        headers = headers.Substring(0, headerend + 2);

                        // determine path
                        int pathStart = 0;
                        if (headers.Contains("GET "))
                        {
                            pathStart = headers.IndexOf("GET ") + 4;
                        }
                        else if (headers.Contains("POST "))
                        {
                            pathStart = headers.IndexOf("POST ") + 5;
                        }
                        int pathEnd = headers.IndexOf(" HTTP/1.1");
                        string path = headers.Substring(pathStart, pathEnd - pathStart);
                        Console.WriteLine("Requested path " + path);
                        headers = headers.Substring(pathEnd + 11);

                        Console.WriteLine("Request headers: " + headers);

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
                            } else if (headerVal[0] == "Application")
                            {
                                appHeader = headerVal[1];
                            }
                        }


                        byte[] requestBytes = bReceive.Skip(headerend + 4).Take(requestLen).ToArray();

                        sMethod = path.Replace("/", "-");

                        Console.WriteLine("\nRequested method " + sMethod + "\n");

                        Console.WriteLine("\nRequest Body: " + Encoding.UTF8.GetString(requestBytes) + "\n");

                        if (requestBytes.Length >= requestLen)
                        {
                            string reqFilename = @"idacdata/proxy" + sMethod + "-request.bin";
                            FileStream fs = new FileStream(reqFilename, FileMode.Create, FileAccess.Write, FileShare.None);
                            BinaryWriter writer = new BinaryWriter(fs);
                            writer.Write(requestBytes);
                            writer.Close();
                            fs.Close();
                            Console.WriteLine("\nWrote proxy request file: " + reqFilename + "\n");

                            string canned = GetCannedResponse(path);
                            if (!canned.Equals(""))
                            {
                                byte[] responseBytes = Encoding.ASCII.GetBytes(canned);
                                SendIDACHeader(responseBytes.Length, ref mySocket);
                                Util.SendToClient(responseBytes, responseBytes.Length, ref mySocket);
                                Console.WriteLine("Body bytes sent: {0}", responseBytes.Length);
                            }
                            else
                            {
                                byte[] responseBytes = proxyRequest(requestBytes, path, appHeader).Result;

                                string sResponse = Encoding.UTF8.GetString(responseBytes);
                                sResponse = sResponse.Replace("kelvin-idac.local:10124", "idac.teknoparrot.com:443");
                                responseBytes= Encoding.UTF8.GetBytes(sResponse);

                                string resFilename = @"idacdata/proxy" + sMethod + "-response.bin";
                                fs = new FileStream(resFilename, FileMode.Create, FileAccess.Write, FileShare.None);
                                writer = new BinaryWriter(fs);
                                writer.Write(responseBytes);
                                writer.Close();
                                fs.Close();
                                Console.WriteLine("\nWrote proxy response file: " + resFilename + "\n");

                                SendIDACHeader(responseBytes.Length, ref mySocket);
                                Util.SendToClient(responseBytes, responseBytes.Length, ref mySocket);
                                Console.WriteLine("Body bytes sent: {0}", responseBytes.Length);
                            }
                            //Console.WriteLine("\nWrote response " + sResponse + "\n");
                        } else
                        {
                            throw new Exception("Request contains only " + requestBytes.Length + " bytes, expected " + requestLen + "\n");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("\nError Occurred : {0} ", e);
                    }
                }
                mySocket.Close();
            }
        }

        public static string GetCannedResponse(string path)
        {
            switch (path)
            {
                default: return "";
            }
        }

        private async Task<byte[]> proxyRequest(byte[] request, string path, string appHeader)
        {
            using ByteArrayContent byteContent = new ByteArrayContent(request);

            byteContent.Headers.Add("Application", appHeader);
            byteContent.Headers.Add("Content-type", "application/json; charset=utf-8");

            using HttpResponseMessage response = await tpProxyClient.PostAsync(path, byteContent);

            response.EnsureSuccessStatusCode();

            HttpStatusCode responseCode = response.StatusCode;
            Console.WriteLine("Upstream response code: " + responseCode);

            Console.WriteLine("Upstream response headers: " + allHeadersAsString(response));

            byte[] responseBytes = response.Content.ReadAsByteArrayAsync().Result;
            
            return responseBytes;
        }

        private String allHeadersAsString(HttpResponseMessage resp)
        {
            String allHeaders = Enumerable
            .Empty<(String name, String value)>()
            // Add the main Response headers as a flat list of value-tuples with potentially duplicate `name` values:
            .Concat(
                resp.Headers
                    .SelectMany(kvp => kvp.Value
                        .Select(v => (name: kvp.Key, value: v))
                    )
            )
            // Concat with the content-specific headers as a flat list of value-tuples with potentially duplicate `name` values:
            .Concat(
                resp.Content.Headers
                    .SelectMany(kvp => kvp.Value
                        .Select(v => (name: kvp.Key, value: v))
                    )
            )
            // Render to a string:
            .Aggregate(
                seed: new StringBuilder(),
                func: (sb, pair) => sb.Append(pair.name).Append(": ").Append(pair.value).AppendLine(),
                resultSelector: sb => sb.ToString()
            );

            return allHeaders;
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
                        Console.WriteLine("\nIDAC MM client Connected!!\n==================\nCLient IP " + mySocket.RemoteEndPoint.ToString() + "\n");
                        //make a byte array and receive data from the client   
                        Byte[] bReceive = new Byte[1024];
                        int i = mySocket.Receive(bReceive, bReceive.Length, 0);
                        //Convert Byte to String  
                        string sBuffer = Encoding.ASCII.GetString(bReceive);

                        int iTotBytes = 0;
                        byte[] bytes;

                        if (sBuffer.IndexOf("ISSUE") >= 0)
                        {
                            // Determine target method
                            reqBody = sBuffer.Substring(sBuffer.IndexOf("ISSUE"));
                            sMethod = reqBody.Substring(0, reqBody.IndexOf("-"));

                            Console.WriteLine("\nRequested method " + sMethod + "\n");

                            sResponse = "";
                            FileStream fs;

                            fs = new FileStream("idacdata/" + sMethod + ".bin", FileMode.Open, FileAccess.Read, FileShare.Read);

                            // Create a reader that can read bytes from the FileStream.  
                            BinaryReader reader = new BinaryReader(fs);
                            bytes = new byte[fs.Length];
                            int read;
                            while ((read = reader.Read(bytes, 0, bytes.Length)) != 0)
                            {
                                // Read from the file and write the data to the network  
                                sResponse = sResponse + Encoding.UTF8.GetString(bytes, 0, read);
                                iTotBytes = iTotBytes + read;
                            }
                            reader.Close();
                            fs.Close();
                        } else
                        {
                            Console.WriteLine("\nUnknown method, request body: " + sBuffer + "\n");
                            bytes = new byte[0];
                        }
                        SendIDACHeader(iTotBytes, ref mySocket);
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