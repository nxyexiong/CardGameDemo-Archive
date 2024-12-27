using System;
using System.Net.Sockets;
using CardGameDemoServer.Common;
using CardGameDemoServer.GameLogic;
using CardGameDemoServer.Networking;

namespace CardGameDemoServer.States
{
    internal class RoundResultState : BaseState
    {
        private const int _waitTimeMs = 10 * 1000;

        public RoundResultState(
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
            CalculateResult();
            ResetTimer(_waitTimeMs);
            UpdateGameStateForClients();
        }

        protected override void OnUpdate()
        {
            var nowMs = TimeUtils.GetTimestampMs(DateTime.Now);
            if (nowMs >= _gameStateInfo.TimerStartTimestampMs + _gameStateInfo.TimerIntervalMs)
            {
                if (IsMatchEnd())
                {
                    Next(GameState.MatchResult, null);
                }
                else
                {
                    var playerCount = _gameStateInfo.PlayerInfos.Count;
                    var nextDealer = (_gameStateInfo.Dealer + 1) % playerCount;
                    InitNewRound(nextDealer);
                    Next(GameState.PlayersTurn, null);
                }
            }
        }

        protected override void OnLeave()
        {
        }

        protected override string? OnRequest(Socket socket, string requestType, string requestRaw, out Action? requestDoneCallback)
        {
            requestDoneCallback = null;
            return null;
        }

        private void CalculateResult()
        {
            // calculate winner
            var winner = -1;
            var playerCount = _gameStateInfo.PlayerInfos.Count;
            for (var i = 0; i < playerCount; i++)
            {
                var lost = false;
                var playerInfo = _gameStateInfo.PlayerInfos[i];
                if (playerInfo.IsFolded) continue;
                for (var j = 0; j < playerCount; j++)
                {
                    var targetInfo = _gameStateInfo.PlayerInfos[j];
                    if (targetInfo.IsFolded) continue;
                    var playerHand = playerInfo.MainHand.Select(x => PokerCard.From(x)).ToList();
                    var targetHand = targetInfo.MainHand.Select(x => PokerCard.From(x)).ToList();
                    if (PokerCard.CompareHands(playerHand!, targetHand!) < 0)
                    {
                        lost = true;
                        break;
                    }
                }
                if (!lost)
                {
                    winner = i;
                    break;
                }
            }

            // set state data
            for (var i = 0; i < playerCount; i++)
            {
                var playerInfo = _gameStateInfo.PlayerInfos[i];
                var stateData = new RoundResultStateData
                {
                    IsWinner = i == winner,
                    Hand = new List<string>(playerInfo.MainHand),
                };
                playerInfo.StateData = stateData.RawData();
            }

            // change networth
            var winnerInfo = _gameStateInfo.PlayerInfos[winner];
            for (var i = 0; i < playerCount; i++)
            {
                if (i == winner) continue;
                var playerInfo = _gameStateInfo.PlayerInfos[i];
                playerInfo.NetWorth -= playerInfo.Bet;
                winnerInfo.NetWorth += playerInfo.Bet;
            }
        }

        private bool IsMatchEnd()
        {
            foreach (var playerInfo in _gameStateInfo.PlayerInfos)
            {
                if (playerInfo.NetWorth <= 5)
                    return true;
            }
            return false;
        }

    }
}
