using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading; 

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
                    Console.WriteLine($"Accepted connection from {remoteEndPoint}");

                    Thread clientThread = new Thread(() => HandleClient(handler, remoteEndPoint));
                    clientThread.Start();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.ReadLine();
        }

        private void HandleClient(Socket handler, IPEndPoint remoteEndPoint)
        {
            try
            {
                byte[] buffer = new byte[4096];
                StringBuilder requestBuilder = new StringBuilder();

                while (true)
                {
                    int bytesRec = handler.Receive(buffer);
                    if (bytesRec == 0)
                    {
                        Console.WriteLine($"Client {remoteEndPoint} disconnected.");
                        break;
                    }

                    requestBuilder.Append(Encoding.ASCII.GetString(buffer, 0, bytesRec));

                    // Check for end of HTTP headers
                    if (requestBuilder.ToString().Contains("\r\n\r\n"))
                    {
                        string request = requestBuilder.ToString();
                        Console.WriteLine($"Received HTTP request from {remoteEndPoint}:\n{request}");

                        // Parse the request line
                        string[] lines = request.Split("\r\n");
                        string[] requestLine = lines[0].Split(' ');

                        if (requestLine.Length >= 2 && requestLine[0] == "GET" && requestLine[1] == "/register")
                        {
                            // Generate a random username (for demo, use a GUID substring)
                            string username = Guid.NewGuid().ToString().Substring(0, 8);

                            string responseBody = $"{{\"username\":\"{username}\"}}";
                            string response =
                                "HTTP/1.1 200 OK\r\n" +
                                "Content-Type: application/json\r\n" +
                                $"Content-Length: {Encoding.UTF8.GetByteCount(responseBody)}\r\n" +
                                "Connection: keep-alive\r\n" +
                                "\r\n" +
                                responseBody;

                            handler.Send(Encoding.UTF8.GetBytes(response));
                        }
                        else
                        {
                            // 404 Not Found for other endpoints
                            string responseBody = "Not Found";
                            string response =
                                "HTTP/1.1 404 Not Found\r\n" +
                                "Content-Type: text/plain\r\n" +
                                $"Content-Length: {Encoding.UTF8.GetByteCount(responseBody)}\r\n" +
                                "Connection: close\r\n" +
                                "\r\n" +
                                responseBody;

                            handler.Send(Encoding.UTF8.GetBytes(response));
                            break; // Close connection for unknown endpoints
                        }

                        requestBuilder.Clear(); // Ready for next request
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error with client {remoteEndPoint}: {e}");
            }
            finally
            {
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
        }

        public static int Main(string[] args)
        {
            new SynchronousSocketListener().StartListening();
            return 0;
        }
    }
}
