using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using CardGameDemoServer.States;
using CardGameDemoServer.Networking;
using CardGameDemoServer.GameLogic;

namespace CardGameDemoServer
{
    internal class ServerGameStateInfo
    {
        public int InitNetWorth { get; set; } = -1;
        public int TurnTimeMs { get; set; } = 30 * 1000;
        public int MaxBet { get; set; } = 50;
        public int DeckCount { get; set; } = 1;
        public PokerCardPile CardPile { get; set; } = new();
        public bool IsAggressorsFirstTurn { get; set; } = true;
    }

    internal class ClientInfo
    {
        public Socket? Socket { get; set; } = null;
        public int PlayerId { get; set; } = -1;
    }

    internal class ResponseContext
    {
        public int Seq { get; set; } = -1;
        public Action? Callback { get; set; } = null;
    }

    internal class GameServer
    {
        private readonly int _port;
        private readonly bool _isIpv6;

        private bool _running;
        private int _seq;
        private Task? _loopTask;
        private readonly ServerGameStateInfo _serverGameStateInfo;
        private readonly GameStateInfo _gameStateInfo;
        private readonly Dictionary<string, ClientInfo> _clients; // profile id -> ClientInfo
        private readonly Dictionary<int, ResponseContext> _responseContexts; // seq -> context
        private readonly Dictionary<GameState, BaseState> _stateMap; // state type -> state

        public GameServer(
            int port = 8800,
            bool isIpv6 = false,
            IEnumerable<string>? profileIds = null,
            int initNetWorth = 500,
            int turnTimeMs = 30 * 1000,
            int maxBet = 50,
            int deckCount = 1)
        {
            _port = port;
            _isIpv6 = isIpv6;

            _running = false;
            _seq = 0;
            _loopTask = null;
            _serverGameStateInfo = new ServerGameStateInfo
            {
                InitNetWorth = initNetWorth,
                TurnTimeMs = turnTimeMs,
                MaxBet = maxBet,
                DeckCount = deckCount,
            };
            _gameStateInfo = new();
            _clients = [];
            for (var i = 0; i < (profileIds?.Count() ?? 0); i++)
            {
                var profileId = profileIds?.ElementAt(i) ?? string.Empty;
                _clients[profileId] = new ClientInfo { Socket = null, PlayerId = i };
                _gameStateInfo.PlayerInfos.Add(new());
            }
            _responseContexts = [];

            _stateMap = [];
            _stateMap.Add(GameState.WaitingForPlayers,
                new WaitingForPlayersState(_serverGameStateInfo, _gameStateInfo, _clients, _stateMap, SendRequest));
            _stateMap.Add(GameState.PlayersTurn,
                new PlayersTurnState(_serverGameStateInfo, _gameStateInfo, _clients, _stateMap, SendRequest));
            _stateMap.Add(GameState.RoundResult,
                new RoundResultState(_serverGameStateInfo, _gameStateInfo, _clients, _stateMap, SendRequest));
            _stateMap.Add(GameState.MatchResult,
                new MatchResultState(_serverGameStateInfo, _gameStateInfo, _clients, _stateMap, SendRequest));
        }

        public void Start()
        {
            Console.WriteLine("[+] server is starting...");
            _running = true;
            _gameStateInfo.CurrentState = GameState.WaitingForPlayers;
            _stateMap[GameState.WaitingForPlayers].Enter(null);
            _loopTask = Task.Run(() => Loop());
            Console.WriteLine("[+] server is started");
        }

        public void Stop()
        {
            Console.WriteLine("[+] server is stopping...");
            _running = false;
            _loopTask?.Wait();
            Console.WriteLine("[+] server is stopped");
        }

        private void Loop()
        {
            while (_running)
            {
                Socket? sock = null;
                List<Socket> clients = [];
                Dictionary<Socket, List<byte>> buffers = [];
                var buf = new byte[2048];
                try
                {
                    sock = new Socket(
                        _isIpv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork,
                        SocketType.Stream,
                        ProtocolType.Tcp);
                    sock.Bind(new IPEndPoint(0, _port));
                    sock.Listen();

                    while (_running)
                    {
                        // handle server socket
                        if (sock.Poll(0, SelectMode.SelectError))
                        {
                            Console.WriteLine($"[-] server socket error");
                            break;
                        }
                        if (sock.Poll(0, SelectMode.SelectRead))
                        {
                            Console.WriteLine($"[+] server accept");
                            var client = sock.Accept();
                            clients.Add(client);
                            buffers[client] = [];
                        }

                        // handle client sockets
                        List<Socket> removeClients = [];
                        foreach (var client in clients)
                        {
                            if (client.Poll(0, SelectMode.SelectError))
                            {
                                Console.WriteLine($"[-] client socket error");
                                removeClients.Add(client);
                                continue;
                            }
                            if (client.Poll(0, SelectMode.SelectRead))
                            {
                                var r = client.Receive(buf);
                                if (r <= 0)
                                {
                                    Console.WriteLine($"[-] client disconnected");
                                    removeClients.Add(client);
                                    continue;
                                }
                                try
                                {
                                    buffers[client].AddRange(buf.Take(r));
                                    if (buffers[client].Count > 1000 * 1000)
                                        throw new InvalidDataException("client buffer exceeded");
                                    while (buffers[client].Count >= 2)
                                    {
                                        var msgLen = buffers[client].ElementAt(0) * 256 + buffers[client].ElementAt(1);
                                        if (buffers[client].Count < msgLen) break;
                                        var msgStr = Encoding.UTF8.GetString(buffers[client].Skip(2).Take(msgLen).ToArray());
                                        buffers[client].RemoveRange(0, 2 + msgLen);
                                        HandleClientMsg(client, msgStr);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[-] client logic exception: {ex}");
                                    removeClients.Add(client);
                                    continue;
                                }
                            }
                        }
                        foreach (var client in removeClients)
                        {
                            buffers.Remove(client);
                            clients.Remove(client);
                        }

                        // update
                        _stateMap[_gameStateInfo.CurrentState].Update();

                        // wait 1ms in case nothing happens
                        Thread.Sleep(1);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[-] server socket exception: {ex}");
                }
                finally
                {
                    buffers.Clear();
                    foreach (var client in clients)
                        client.Close();
                    clients.Clear();
                    sock?.Close();
                    sock = null;
                }
            }
        }

        private void HandleClientMsg(Socket client, string msg)
        {
            Console.WriteLine($"[+] handle client msg, {client.RemoteEndPoint}: {msg}");
            var csData = CSData.From(msg);
            if (csData?.Type == CSData.DataType.Request)
            {
                var request = Request.From(csData.Data) ?? throw new InvalidDataException("client request is null");
                HandleClientRequest(client, request);
            }
            else if (csData?.Type == CSData.DataType.Response)
            {
                var response = Response.From(csData.Data) ?? throw new InvalidDataException("client response is null");
                HandleClientResponse(client, response);
            }
        }

        private void HandleClientRequest(Socket client, Request request)
        {
            var seq = request.Seq;
            var typeName = request.Type;
            Console.WriteLine($"[+] handle client request, {client.RemoteEndPoint}: {seq}-{typeName}");

            // get response
            var state = _gameStateInfo.CurrentState;
            var responseStr = _stateMap[state]
                .GetResponseData(client, typeName, request.Data, out var requestDoneCallback) ??
                throw new InvalidDataException($"[-] handle client request, no response for {typeName} in {state}");

            // send response
            var response = new Response { Seq = seq, Data = responseStr };
            var csData = new CSData { Type = CSData.DataType.Response, Data = response.RawData() };
            SendMsg(client, csData.RawData());

            // on request done
            requestDoneCallback?.Invoke();
        }

        private void HandleClientResponse(Socket client, Response response)
        {
            var seq = response.Seq;
            Console.WriteLine($"[+] handle client response, {client.RemoteEndPoint}: {seq}");
            if (!_responseContexts.TryGetValue(seq, out var context))
            {
                Console.WriteLine($"[-] handle client response, unknown seq: {seq}");
                return;
            }
            context.Callback?.Invoke();
            _responseContexts.Remove(seq);
        }

        private void SendRequest(Socket client, string typeName, string requestRaw, Action? callback)
        {
            var req = new Request { Seq = _seq++, Type = typeName, Data = requestRaw };
            var csData = new CSData { Type = CSData.DataType.Request, Data = req.RawData() };
            var context = new ResponseContext { Seq = req.Seq, Callback = callback };
            _responseContexts[req.Seq] = context;
            SendMsg(client, csData.RawData());
        }

        private static void SendMsg(Socket sock, string msg)
        {
            var data = Encoding.UTF8.GetBytes(msg);
            var msgLen = data.Length;
            var send = new byte[2 + msgLen];
            send[0] = (byte)(msgLen / 256);
            send[1] = (byte)(msgLen % 256);
            Array.Copy(data, 0, send, 2, msgLen);
            while (send.Length > 0)
            {
                var w = sock.Send(send);
                if (w <= 0)
                    throw new InvalidDataException($"[-] send msg failed: {w}");
                send = send.Skip(w).ToArray();
            }
        }
    }
}
