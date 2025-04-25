using Fleck;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SeniorProjectRefactored.Networking
{
    public class NetworkManager
    {
        private WebSocketServer _server;
        private readonly List<IWebSocketConnection> _clients = new();

        private bool _hosting;
        public bool IsHosting => _hosting;

        private ClientWebSocket _client;
        private bool _connected;
        public string MyId { get; private set; } = Guid.NewGuid().ToString();

        public event Action OnServerStarted;
        public event Action<string> OnServerFailed;
        public event Action<string> OnClientConnectedWithId;
        public event Action<string> OnClientDisconnectedWithId;

        public event Action OnClientConnected;
        public event Action<string> OnClientFailed;
        public event Action<string> OnClientMessage;

        public void StartHosting(int port)
        {
            if (_hosting) return;

            try
            {
                _server = new WebSocketServer($"ws://0.0.0.0:{port}");
                var idToSocket = new Dictionary<string, IWebSocketConnection>();
                var lastPingTimes = new Dictionary<string, DateTime>();
                _hosting = true;

                _server.Start(socket =>
                {
                    string id = null;

                    socket.OnOpen = () =>
                    {
                        _clients.Add(socket);
                        Debug.WriteLine("Socket opened");
                        pingClients();
                    };

                    socket.OnMessage = msg =>
                    {
                        Debug.WriteLine("Host received: " + msg);
                        OnClientMessage?.Invoke(msg);

                        try
                        {
                            using var doc = JsonDocument.Parse(msg);
                            var type = doc.RootElement.GetProperty("type").GetString();

                            if (type == "Hello")
                            {
                                id = doc.RootElement.GetProperty("id").GetString();
                                OnClientConnectedWithId?.Invoke(id);
                                lastPingTimes[id] = DateTime.Now;
                                idToSocket[id] = socket;
                            }
                            else if (type == "Ping")
                            {
                                id = doc.RootElement.GetProperty("id").GetString();
                                lastPingTimes[id] = DateTime.Now;
                            }
                        }
                        catch (Exception) {
                            Debug.WriteLine("Bad JSON");
                        }
                    };

                    socket.OnClose = () =>
                    {
                        _clients.Remove(socket);
                        if (id != null)
                            OnClientDisconnectedWithId?.Invoke(id);

                        Debug.WriteLine($"Client {id ?? "(unknown)"} disconnected");
                    };
                });

                OnServerStarted?.Invoke();

                _ = Task.Run(async () =>
                {
                    while (_hosting)
                    {
                        var now = DateTime.Now;
                        var expired = lastPingTimes.Where(kvp => (now - kvp.Value).TotalSeconds > 15).Select(kvp => kvp.Key).ToList();

                        foreach (var deadId in expired)
                        {
                            lastPingTimes.Remove(deadId);

                            if (idToSocket.TryGetValue(deadId, out var socket))
                            {
                                _clients.Remove(socket);
                                socket.Close();
                                idToSocket.Remove(deadId);
                            }

                            OnClientDisconnectedWithId?.Invoke(deadId);
                            Debug.WriteLine($"[Heartbeat Timeout] Disconnected: {deadId}");
                        }

                        pingClients();

                        await Task.Delay(5000);
                    }
                });
            }
            catch (Exception ex)
            {
                _hosting = false;
                OnServerFailed?.Invoke(ex.Message);
            }
        }

        public void Broadcast(string payload)
        {
            if (!_hosting) return;
            foreach (var c in _clients) c.Send(payload);
        }

        public void pingClients()
        {
            var playerCount = _clients.Count;
            var pingJson = JsonSerializer.Serialize(new { type = "ServerPing", count = playerCount });
            
            foreach (var client in _clients)
            {
                _ = client.Send(pingJson);
            }
        }

        public async Task ConnectToServerAsync(string host, int port)
        {
            if (_connected) return;

            _client = new ClientWebSocket();
            try
            {
                await _client.ConnectAsync(new Uri($"ws://{host}:{port}"), CancellationToken.None);
                _connected = true;
                OnClientConnected?.Invoke();
                await ClientSendAsync($"{{\"type\":\"Hello\",\"id\":\"{MyId}\"}}");
             
                _ = Task.Run(async () =>
                {
                    while (_connected)
                    {
                        await ClientSendAsync($"{{\"type\":\"Ping\",\"id\":\"{MyId}\"}}");
                        await Task.Delay(5000);
                    }
                });

                _ = Task.Run(ReceiveLoop);
            }
            catch (Exception ex)
            {
                OnClientFailed?.Invoke(ex.Message);
            }
        }

        public async Task ClientSendAsync(string payload)
        {
            if (!_connected) return;
            var data = Encoding.UTF8.GetBytes(payload);
            await _client.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        private async Task ReceiveLoop()
        {
            var buf = new byte[4096];
            while (_connected)
            {
                var res = await _client.ReceiveAsync(buf, CancellationToken.None);
                if (res.MessageType == WebSocketMessageType.Close) break;
                string txt = Encoding.UTF8.GetString(buf, 0, res.Count);
                OnClientMessage?.Invoke(txt);
            }
        }
    }
}