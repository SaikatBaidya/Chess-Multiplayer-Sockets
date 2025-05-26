using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading; 
using System.Collections.Generic;

namespace SocketTest
{
    public class SynchronousSocketListener
    {
        private static object _lock = new object();
        private static List<GameRecord> waitingGames = new List<GameRecord>();
        private static List<GameRecord> activeGames = new List<GameRecord>();

        private class GameRecord
        {
            public string GameId { get; set; }
            public string State { get; set; } // "wait" or "progress"
            public string Player1 { get; set; }
            public string Player2 { get; set; }
            public string LastMove1 { get; set; }
            public string LastMove2 { get; set; }
        }

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
                        Console.WriteLine($"Received HTTP request from {remoteEndPoint} on thread {Thread.CurrentThread.ManagedThreadId}:\n{request}");

                        // Parse the request line
                        string[] lines = request.Split("\r\n");
                        string[] requestLine = lines[0].Split(' ');

                        if (requestLine.Length >= 2 && requestLine[0] == "GET")
                        {
                            if (requestLine[1].StartsWith("/register"))
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
                            else if (requestLine[1].StartsWith("/pairme"))
                            {
                                // Manual query string parsing for player parameter
                                string player = null;
                                string url = requestLine[1];
                                int qIndex = url.IndexOf('?');
                                if (qIndex != -1 && url.Length > qIndex + 1)
                                {
                                    string query = url.Substring(qIndex + 1);
                                    var pairs = query.Split('&');
                                    foreach (var pair in pairs)
                                    {
                                        var kv = pair.Split('=');
                                        if (kv.Length == 2 && kv[0] == "player")
                                        {
                                            player = Uri.UnescapeDataString(kv[1]);
                                            break;
                                        }
                                    }
                                }

                                if (string.IsNullOrEmpty(player))
                                {
                                    string responseBody = "Missing player parameter";
                                    string response =
                                        "HTTP/1.1 400 Bad Request\r\n" +
                                        "Content-Type: text/plain\r\n" +
                                        $"Content-Length: {Encoding.UTF8.GetByteCount(responseBody)}\r\n" +
                                        "Connection: keep-alive\r\n" +
                                        "\r\n" +
                                        responseBody;
                                    handler.Send(Encoding.UTF8.GetBytes(response));
                                }
                                else
                                {
                                    GameRecord gameRecord = null;
                                    lock (_lock)
                                    {
                                        // Try to find a waiting game
                                        if (waitingGames.Count > 0)
                                        {
                                            gameRecord = waitingGames[0];
                                            waitingGames.RemoveAt(0);
                                            gameRecord.Player2 = player;
                                            gameRecord.State = "progress";
                                            activeGames.Add(gameRecord);
                                        }
                                        else
                                        {
                                            // Create a new waiting game
                                            gameRecord = new GameRecord
                                            {
                                                GameId = Guid.NewGuid().ToString(),
                                                State = "wait",
                                                Player1 = player,
                                                Player2 = null,
                                                LastMove1 = null,
                                                LastMove2 = null
                                            };
                                            waitingGames.Add(gameRecord);
                                        }
                                    }

                                    // Build response JSON
                                    string responseBody = $"{{\"gameId\":\"{gameRecord.GameId}\",\"state\":\"{gameRecord.State}\",\"player1\":\"{gameRecord.Player1}\",\"player2\":\"{gameRecord.Player2}\",\"lastMove1\":{(gameRecord.LastMove1 == null ? "null" : $"\"{gameRecord.LastMove1}\"")},\"lastMove2\":{(gameRecord.LastMove2 == null ? "null" : $"\"{gameRecord.LastMove2}\"")}}}";
                                    string response =
                                        "HTTP/1.1 200 OK\r\n" +
                                        "Content-Type: application/json\r\n" +
                                        $"Content-Length: {Encoding.UTF8.GetByteCount(responseBody)}\r\n" +
                                        "Connection: keep-alive\r\n" +
                                        "\r\n" +
                                        responseBody;
                                    handler.Send(Encoding.UTF8.GetBytes(response));
                                }
                            }
                            else if (requestLine[1].StartsWith("/mymove"))
                            {
                                // Manual query string parsing for player, id, and move
                                string player = null, gameId = null, move = null;
                                string url = requestLine[1];
                                int qIndex = url.IndexOf('?');
                                if (qIndex != -1 && url.Length > qIndex + 1)
                                {
                                    string query = url.Substring(qIndex + 1);
                                    var pairs = query.Split('&');
                                    foreach (var pair in pairs)
                                    {
                                        var kv = pair.Split('=');
                                        if (kv.Length == 2)
                                        {
                                            if (kv[0] == "player") player = Uri.UnescapeDataString(kv[1]);
                                            else if (kv[0] == "id") gameId = Uri.UnescapeDataString(kv[1]);
                                            else if (kv[0] == "move") move = Uri.UnescapeDataString(kv[1]);
                                        }
                                    }
                                }

                                if (string.IsNullOrEmpty(player) || string.IsNullOrEmpty(gameId) || string.IsNullOrEmpty(move))
                                {
                                    string responseBody = "Missing parameter";
                                    string response =
                                        "HTTP/1.1 400 Bad Request\r\n" +
                                        "Content-Type: text/plain\r\n" +
                                        $"Content-Length: {Encoding.UTF8.GetByteCount(responseBody)}\r\n" +
                                        "Connection: keep-alive\r\n" +
                                        "\r\n" +
                                        responseBody;
                                    handler.Send(Encoding.UTF8.GetBytes(response));
                                }
                                else
                                {
                                    bool found = false;
                                    lock (_lock)
                                    {
                                        foreach (var game in activeGames)
                                        {
                                            if (game.GameId == gameId)
                                            {
                                                if (game.Player1 == player)
                                                    game.LastMove1 = move;
                                                else if (game.Player2 == player)
                                                    game.LastMove2 = move;
                                                else
                                                    break; // player not in this game

                                                found = true;
                                                break;
                                            }
                                        }
                                    }

                                    if (found)
                                    {
                                        string responseBody = "{\"status\":\"ok\"}";
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
                                        string responseBody = "Game or player not found";
                                        string response =
                                            "HTTP/1.1 404 Not Found\r\n" +
                                            "Content-Type: text/plain\r\n" +
                                            $"Content-Length: {Encoding.UTF8.GetByteCount(responseBody)}\r\n" +
                                            "Connection: keep-alive\r\n" +
                                            "\r\n" +
                                            responseBody;
                                        handler.Send(Encoding.UTF8.GetBytes(response));
                                    }
                                }
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
