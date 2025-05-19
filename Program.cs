using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketTest
{
    public class SynchronousSocketListener
    {
        public void StartListening()
        {
            const int BACKLOG = 10;
            const int DEFPORTNUM = 11000;

            // Use loopback IP (localhost)
            IPAddress ipAddress = new IPAddress(new byte[] { 127, 0, 0, 1 });
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, DEFPORTNUM);

            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(BACKLOG);

                while (true)
                {
                    Console.WriteLine($"Waiting for a connection at {localEndPoint.Port}...");

                    Socket handler = listener.Accept();
                    var remoteEndPoint = (IPEndPoint)handler.RemoteEndPoint!;
                    byte[] bytes = new byte[1024];
                    string data = string.Empty;

                    // Keep the connection open for multiple requests
                    while (true)
                    {
                        int bytesRec = handler.Receive(bytes);
                        if (bytesRec == 0)
                        {
                            // Client disconnected
                            Console.WriteLine($"Client {remoteEndPoint} disconnected.");
                            break;
                        }
                        data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                        if (data.IndexOf('\n') > -1)
                        {
                            Console.WriteLine($"Received text from {remoteEndPoint}: {data}");

                            byte[] msg = Encoding.ASCII.GetBytes($"[{data}]");
                            handler.Send(msg);

                            data = string.Empty; // Reset for next message
                        }
                    }

                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.ReadLine();
        }

        public static int Main(string[] args)
        {
            new SynchronousSocketListener().StartListening();
            return 0;
        }
    }
}
