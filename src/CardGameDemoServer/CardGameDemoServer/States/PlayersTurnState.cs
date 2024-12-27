using System;
using System.Net.Sockets;
using Newtonsoft.Json;
using CardGameDemoServer.Networking;
using CardGameDemoServer.Common;

namespace CardGameDemoServer.States
{
    internal class PlayersTurnState : BaseState
    {
        public PlayersTurnState(
            ServerGameStateInfo serverGameStateInfo,
            GameStateInfo gameStateInfo,
            Dictionary<string, ClientInfo> clients,
            Dictionary<GameState, BaseState> stateMap,
            Action<Socket, string, string, Action?> sendRequest) :
            base(serverGameStateInfo, gameStateInfo, clients, stateMap, sendRequest)
        {
        }

        protected override void OnEnter(object? data)
        {
            UpdateStateData();
            UpdateGameStateForClients();
        }

        protected override void OnUpdate()
        {
            var nowMs = TimeUtils.GetTimestampMs(DateTime.Now);
            if (nowMs >= _gameStateInfo.TimerStartTimestampMs + _gameStateInfo.TimerIntervalMs)
                FoldCurrentPlayer();
        }

        protected override void OnLeave()
        {
        }

        protected override string? OnRequest(Socket socket, string requestType, string requestRaw, out Action? requestDoneCallback)
        {
            requestDoneCallback = null;

            if (requestType == nameof(DoGeneralActionRequest))
                return GetResponseDataForDoGeneralAction(socket, requestRaw, out requestDoneCallback);

            return null;
        }

        private string GetResponseDataForDoGeneralAction(Socket client, string requestData, out Action? requestDoneCallback)
        {
            requestDoneCallback = null;

            var clientInfo = _clients.Where(x => x.Value.Socket == client).Select(x => x.Value).FirstOrDefault();
            if (clientInfo == null)
            {
                Console.WriteLine($"[-] client info is null: {client.RemoteEndPoint}");
                return JsonConvert.SerializeObject(new DoGeneralActionResponse { Success = false, Data = string.Empty });
            }

            requestDoneCallback = () =>
            {
                var request = JsonConvert.DeserializeObject<DoGeneralActionRequest>(requestData);
                if (request == null)
                {
                    Console.WriteLine($"[-] do action request parse failed: {requestData}");
                    return;
                }
                OnDoGeneralAction(clientInfo.PlayerId, request.Action, request.Data);
            };

            return JsonConvert.SerializeObject(new DoGeneralActionResponse { Success = true, Data = string.Empty });
        }

        private void OnDoGeneralAction(int playerId, GeneralAction action, string data)
        {
            var playerCount = _gameStateInfo.PlayerInfos.Count;
            var playerInfo = _gameStateInfo.PlayerInfos[playerId];
            var stateData = PlayersTurnStateData.From(playerInfo.StateData);

            if (!stateData.GeneralActions.Contains(action))
            {
                Console.WriteLine($"[-] player id {playerId} invalid general action {action}");
                return;
            }

            if (action == GeneralAction.FollowBet)
            {
                playerInfo.Bet = GetHighestBet();
            }
            else if (action == GeneralAction.RaiseBet)
            {
                var raiseBetData = RaiseBetData.From(data);
                if (raiseBetData.Bet <= 0 ||
                    (raiseBetData.Bet % 5) != 0 ||
                    raiseBetData.Bet > MaxRaiseAmount())
                {
                    Console.WriteLine($"[-] player id {playerId} invalid raise bet amount {raiseBetData.Bet}");
                    return;
                }
                playerInfo.Bet = GetHighestBet() + raiseBetData.Bet;
                _gameStateInfo.Aggressor = playerId;
            }
            else if (action == GeneralAction.Fold)
            {
                playerInfo.IsFolded = true;
                if (IsRoundEndByFolding())
                {
                    Next(GameState.RoundResult, null);
                    return;
                }
            }
            else if (action == GeneralAction.Showdown)
            {
                Next(GameState.RoundResult, null);
                return;
            }

            _gameStateInfo.ActivePlayer = (_gameStateInfo.ActivePlayer + 1) % playerCount;
            Next(GameState.PlayersTurn, null);
        }

        private void UpdateStateData()
        {

            for (var playerId = 0; playerId < _gameStateInfo.PlayerInfos.Count; playerId++)
            {
                var playerInfo = _gameStateInfo.PlayerInfos[playerId];
                var stateData = new PlayersTurnStateData { GeneralActions = [] };

                if (playerId == _gameStateInfo.ActivePlayer)
                {
                    if (playerId == _gameStateInfo.Aggressor)
                    {
                        if (_serverGameStateInfo.IsAggressorsFirstTurn)
                        {
                            // aggressor's first turn
                            stateData.GeneralActions.Add(GeneralAction.RaiseBet);
                            stateData.GeneralActions.Add(GeneralAction.Fold);
                            _serverGameStateInfo.IsAggressorsFirstTurn = false;
                        }
                        else
                        {
                            // aggressor's second turn
                            if (MaxRaiseAmount() > 0)
                                stateData.GeneralActions.Add(GeneralAction.RaiseBet);
                            stateData.GeneralActions.Add(GeneralAction.Showdown);
                        }
                    }
                    else
                    {
                        // non agressor
                        stateData.GeneralActions.Add(GeneralAction.FollowBet);
                        if (MaxRaiseAmount() > 0)
                            stateData.GeneralActions.Add(GeneralAction.RaiseBet);
                        stateData.GeneralActions.Add(GeneralAction.Fold);
                    }
                }
                else
                {
                    // non active player
                }

                playerInfo.StateData = stateData.RawData();
            }

            ResetTimer(_serverGameStateInfo.TurnTimeMs);
        }

        private int GetHighestBet()
        {
            var highestBet = 0;
            for (var i = 0; i < _gameStateInfo.PlayerInfos.Count; i++)
                if (_gameStateInfo.PlayerInfos[i].Bet > highestBet)
                    highestBet = _gameStateInfo.PlayerInfos[i].Bet;
            return highestBet;
        }

        private int MaxRaiseAmount()
        {
            var highestBet = GetHighestBet();
            var ret = _serverGameStateInfo.MaxBet - highestBet;
            for (var i = 0; i < _gameStateInfo.PlayerInfos.Count; i++)
            {
                if (ret > _gameStateInfo.PlayerInfos[i].NetWorth - highestBet)
                    ret = _gameStateInfo.PlayerInfos[i].NetWorth - highestBet;
            }
            return ret;
        }

        private void FoldCurrentPlayer()
        {
            var playerCount = _gameStateInfo.PlayerInfos.Count;
            _gameStateInfo.PlayerInfos[_gameStateInfo.ActivePlayer].IsFolded = true;

            if (IsRoundEndByFolding())
            {
                Next(GameState.RoundResult, null);
                return;
            }

            _gameStateInfo.ActivePlayer = (_gameStateInfo.ActivePlayer + 1) % playerCount;
            Next(GameState.PlayersTurn, null);
        }

        private bool IsRoundEndByFolding()
        {
            var unfoldPlayerCount = 0;
            foreach (var info in _gameStateInfo.PlayerInfos)
                if (!info.IsFolded)
                    unfoldPlayerCount++;
            if (unfoldPlayerCount <= 1)
                return true;
            return false;
        }

    }
}
