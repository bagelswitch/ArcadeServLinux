using APM3Serv;
using AuthServ;
using GEXVS2Serv;
using NAOMIServ;
using MKGPDXServ;

namespace ArcadeServLinux
{
    static class Program
    {
        static void Main()
        {
            new AuthWebServer();
            new NAOMIWebServer();
            new GEXVS2WebServer();
            new APM3WebServer();
            new MKGPDXWebServer();
        }
    }
}
