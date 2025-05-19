using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using ConsoleAppCultuEspai.Models;
using System.Collections.Generic;

namespace ChatServer
{
    public class ChatManager
    {
        private const int Port = 5000;
        private TcpListener _server;

        private static readonly ConcurrentDictionary<int, TcpClient> ConnectedClients = new ConcurrentDictionary<int, TcpClient>();
        private static readonly ConcurrentDictionary<int, int> UserEvents = new ConcurrentDictionary<int, int>();

        public static void Main(string[] args)
        {
            var server = new ChatManager();
            server.StartServer();
        }

        public void StartServer()
        {
            _server = new TcpListener(IPAddress.Any, Port);
            _server.Start();
            Console.WriteLine($"[SERVER] Listening on port {Port}...");

            while (true)
            {
                TcpClient client = _server.AcceptTcpClient();
                Console.WriteLine("[SERVER] Client connected.");
                new Thread(HandleClient).Start(client);
            }
        }

        private void HandleClient(object obj)
        {
            var client = (TcpClient)obj;
            var stream = client.GetStream();
            var buffer = new byte[2048];
            int bytesRead;
            int usuariId = -1;
            int eventId = -1;

            try
            {
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                var jsonString = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var auth = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);

                if (!auth.ContainsKey("UsuariID") || !auth.ContainsKey("IDEsdeveniment"))
                {
                    SendMessage(stream, "{\"error\":\"Auth inválido\"}");
                    return;
                }

                usuariId = int.Parse(auth["UsuariID"]);
                eventId = int.Parse(auth["IDEsdeveniment"]);

                using (var db = new espaiCulturalEntities())
                {
                    if (!db.Usuaris.Any(u => u.UsuariID == usuariId) ||
                        !db.Esdeveniments.Any(e => e.EsdevenimentID == eventId))
                    {
                        SendMessage(stream, "{\"error\":\"Usuari o esdeveniment no existeix\"}");
                        return;
                    }
                }

                ConnectedClients[usuariId] = client;
                UserEvents[usuariId] = eventId;

                Console.WriteLine($"[INFO] Usuari {usuariId} connectat al esdeveniment {eventId}");

                SendChatHistory(stream, eventId);

                var reader = new StreamReader(stream, Encoding.UTF8);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var msg = JsonConvert.DeserializeObject<Dictionary<string, string>>(line);
                    if (!msg.ContainsKey("Text") || string.IsNullOrWhiteSpace(msg["Text"]))
                        continue;

                    using (var db = new espaiCulturalEntities())
                    {
                        var nouMissatge = db.Xats.Create();
                        nouMissatge.IDUser = usuariId;
                        nouMissatge.Text = msg["Text"];
                        nouMissatge.IDEsdeveniment = eventId;
                        nouMissatge.Data = DateTime.Now;
                        nouMissatge.Enviat = true;

                        db.Xats.Add(nouMissatge);
                        db.SaveChanges();
                    }

                    foreach (var kvp in UserEvents.Where(e => e.Value == eventId))
                    {
                        if (ConnectedClients.TryGetValue(kvp.Key, out var targetClient))
                        {
                            try
                            {
                                var targetStream = targetClient.GetStream();
                                SendChatHistory(targetStream, eventId);
                            }
                            catch
                            {
                                ConnectedClients.TryRemove(kvp.Key, out _);
                                UserEvents.TryRemove(kvp.Key, out _);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
            }
            finally
            {
                if (usuariId > 0)
                {
                    ConnectedClients.TryRemove(usuariId, out _);
                    UserEvents.TryRemove(usuariId, out _);
                    Console.WriteLine($"[INFO] Usuari {usuariId} desconnectat.");
                }

                client.Close();
            }
        }

        private void SendChatHistory(NetworkStream stream, int eventId)
        {
            List<Xats> missatges;
            using (var db = new espaiCulturalEntities())
            {
                missatges = db.Xats
                    .Where(m => m.IDEsdeveniment == eventId)
                    .OrderBy(m => m.Data)
                    .ToList();
            }

            var json = JsonConvert.SerializeObject(missatges);
            SendMessage(stream, json);
        }

        private void SendMessage(NetworkStream stream, string message)
        {
            var data = Encoding.UTF8.GetBytes(message + "\n");
            stream.Write(data, 0, data.Length);
        }
    }
}