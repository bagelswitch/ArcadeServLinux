using System.Net.Sockets;
using System.Text;

namespace Util
{
    static class Util
    {
        public static void EatTo(ref string input, string food)
        {
            input = input.Substring(input.IndexOf(food) + food.Length);
        }

        public static string HexBinary(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        public static void SendToClient(Byte[] bSendData, int length, ref Socket mySocket)
        {
            int numBytes = 0;
            try
            {
                if (mySocket.Connected)
                {
                    if ((numBytes = mySocket.Send(bSendData, length, 0)) == -1)
                    {
                        Console.WriteLine("Socket Error cannot Send Packet");
                    }
                }
                else Console.WriteLine("Connection Dropped....");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error Occurred : {0} ", e);
            }
        }

        public static void SendToClient(String sData, ref Socket mySocket)
        {
            SendToClient(Encoding.UTF8.GetBytes(sData), sData.Length, ref mySocket);
        }
        public static void SendContinue(ref Socket mySocket)
        {
            String sBuffer = "";
            sBuffer = sBuffer + "HTTP/1.1 100 Continue\r\n\r\n";

            Console.WriteLine("Sending continue: " + sBuffer);

            Byte[] bSendData = Encoding.ASCII.GetBytes(sBuffer);
            SendToClient(bSendData, bSendData.Length, ref mySocket);
            Console.WriteLine("Continue bytes sent: " + sBuffer.Length);
        }
    }
}
