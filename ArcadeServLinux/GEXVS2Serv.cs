namespace GEXVS2Serv
{
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using Util;

    class GEXVS2WebServer
    {
        private TcpListener exvs2Listener;
        private TcpListener mmListener;
        private static IPAddress localAddr = IPAddress.Parse("0.0.0.0");
        private int EXVS2port = 7820; // EXVS2 port   
        private int MMport = 7821; // EXVS2 MM port

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
                        Byte[] bReceive = new Byte[1024];
                        int i = mySocket.Receive(bReceive, bReceive.Length, 0);
                        //Convert Byte to String  
                        string sBuffer = Encoding.ASCII.GetString(bReceive);
                        // Determine target method
                        reqBody = sBuffer.Substring(sBuffer.IndexOf("MTHD"));
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