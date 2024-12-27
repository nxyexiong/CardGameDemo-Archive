using System;
using System.Net.Sockets;
using Newtonsoft.Json;
using CardGameDemoServer.Common;
using CardGameDemoServer.Networking;
using CardGameDemoServer.GameLogic;

namespace CardGameDemoServer.States
{
    internal abstract class BaseState
    {
        protected readonly ServerGameStateInfo _serverGameStateInfo;
        protected readonly GameStateInfo _gameStateInfo;
        protected readonly Dictionary<string, ClientInfo> _clients;
        protected readonly Dictionary<GameState, BaseState> _stateMap;
        private readonly Action<Socket, string, string, Action?> _sendRequest;

        protected BaseState(
            ServerGameStateInfo serverGameStateInfo,
            GameStateInfo gameStateInfo,
            Dictionary<string, ClientInfo> clients,
            Dictionary<GameState, BaseState> stateMap,
            Action<Socket, string, string, Action?> sendRequest)
        {
            _serverGameStateInfo = serverGameStateInfo;
            _gameStateInfo = gameStateInfo;
            _clients = clients;
            _stateMap = stateMap;
            _sendRequest = sendRequest;
        }

        public void Enter(object? data)
        {
            Console.WriteLine($"[+] enter state {GetType().Name}");
            OnEnter(data);
        }

        public void Update()
        {
            OnUpdate();
        }

        public void Leave()
        {
            OnLeave();
            Console.WriteLine($"[+] leave state {GetType().Name}");
        }

        public string? GetResponseData(
            Socket socket,
            string requestType,
            string requestRaw,
            out Action? requestDoneCallback)
        {
            requestDoneCallback = null;

            if (requestType == nameof(HandshakeRequest))
                return GetResponseDataForHandshake(socket, requestRaw, out requestDoneCallback);

            return OnRequest(socket, requestType, requestRaw, out requestDoneCallback);
        }

        public void Next(GameState stateType, object? data)
        {
            if (!_stateMap.TryGetValue(stateType, out var state))
            {
                Console.WriteLine($"[-] cannot find state {stateType}");
                return;
            }
            Leave();
            _gameStateInfo.CurrentState = stateType;
            state.Enter(data);
        }

        protected abstract void OnEnter(object? data);

        protected abstract void OnUpdate();

        protected abstract void OnLeave();

        protected abstract string? OnRequest(
            Socket socket,
            string requestType,
            string requestRaw,
            out Action? requestDoneCallback);

        protected void SendRequest(Socket client, string typeName, string requestRaw, Action? callback)
        {
            _sendRequest.Invoke(client, typeName, requestRaw, callback);
        }

        protected void UpdateGameStateForClients()
        {
            foreach (var kv in _clients)
            {
                var profileId = kv.Key;
                UpdateGameStateForClient(profileId);
            }
        }

        protected void UpdateGameStateForClient(string profileId)
        {
            Console.WriteLine($"[+] update game state for client: {profileId}");
            var clientInfo = _clients[profileId];
            if (clientInfo.Socket == null)
            {
                Console.WriteLine($"[*] missing socket for client: {profileId}");
                return;
            }

            var sendGameStateInfo = _gameStateInfo.Copy();
            var playerId = clientInfo.PlayerId;
            sendGameStateInfo.PlayerId = playerId;
            for (var i = 0; i < sendGameStateInfo.PlayerInfos.Count; i++)
            {
                if (i != playerId)
                {
                    sendGameStateInfo.PlayerInfos[i].MainHand.Clear();
                    sendGameStateInfo.PlayerInfos[i].HiddenStateData = string.Empty;
                }
            }

            var request = new UpdateGameStateRequest
            {
                ServerTimestampMs = TimeUtils.GetTimestampMs(DateTime.Now),
                GameStateInfo = sendGameStateInfo,
            };
            SendRequest(clientInfo.Socket, nameof(UpdateGameStateRequest), request.RawData(), null);
        }

        private string GetResponseDataForHandshake(Socket client, string requestData, out Action? requestDoneCallback)
        {
            requestDoneCallback = null;
            var request = JsonConvert.DeserializeObject<HandshakeRequest>(requestData) ??
                throw new InvalidDataException("parse HandshakeRequest failed");

            // check profile id
            var profileId = request.ProfileId;
            if (!_clients.TryGetValue(profileId, out ClientInfo? clientInfo))
            {
                Console.WriteLine($"[-] invalid profile id: {profileId}");
                return JsonConvert.SerializeObject(new HandshakeResponse { Success = false });
            }

            // update client info
            clientInfo.Socket = client;
            _gameStateInfo.PlayerInfos[clientInfo.PlayerId].Name = request.Name;

            // on request done
            requestDoneCallback = () =>
            {
                if (_gameStateInfo.CurrentState == GameState.WaitingForPlayers)
                {
                    var missingPlayer = false;
                    foreach (var kv in _clients)
                        if (kv.Value.Socket == null)
                            missingPlayer = true;
                    if (!missingPlayer)
                    {
                        InitNewRound(0);
                        Next(GameState.PlayersTurn, null);
                        return;
                    }
                }
                UpdateGameStateForClients();
            };

            return JsonConvert.SerializeObject(new HandshakeResponse { Success = true });
        }

        protected void InitNewRound(int dealer)
        {
            _serverGameStateInfo.CardPile.Init(_serverGameStateInfo.DeckCount, false);
            _serverGameStateInfo.CardPile.Shuffle();
            _serverGameStateInfo.IsAggressorsFirstTurn = true;

            _gameStateInfo.Dealer = dealer;
            _gameStateInfo.Aggressor = dealer;
            _gameStateInfo.ActivePlayer = dealer;

            for (var playerId = 0; playerId < _gameStateInfo.PlayerInfos.Count; playerId++)
            {
                var playerInfo = _gameStateInfo.PlayerInfos[playerId];
                playerInfo.Bet = 5;
                playerInfo.IsFolded = false;
                playerInfo.MainHand = [];
                var cardList = new List<PokerCard>();
                for (var i = 0; i < 3; i++)
                {
                    var card = _serverGameStateInfo.CardPile.Draw() ??
                        throw new InvalidOperationException("cannot draw card");
                    cardList.Add(card);
                };
                cardList.Sort();
                foreach (var card in cardList)
                    playerInfo.MainHand.Add(card.RawData());
                playerInfo.StateData = string.Empty;
                playerInfo.HiddenStateData = string.Empty;
            }

            Console.WriteLine($"[+] init new round complete: {JsonConvert.SerializeObject(_gameStateInfo)}");
        }

        protected void ResetTimer(int intervalMs)
        {
            _gameStateInfo.TimerStartTimestampMs = TimeUtils.GetTimestampMs(DateTime.Now);
            _gameStateInfo.TimerIntervalMs = intervalMs;
        }

    }
}
