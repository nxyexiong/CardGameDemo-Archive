using System;
using System.Net.Sockets;
using CardGameDemoServer.Common;
using CardGameDemoServer.Networking;

namespace CardGameDemoServer.States
{
    internal class WaitingForPlayersState : BaseState
    {
        public WaitingForPlayersState(
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
            SetInitNetWorth();
        }

        protected override void OnUpdate()
        {
            // TODO: timeout match end
        }

        protected override void OnLeave()
        {
        }

        protected override string? OnRequest(Socket socket, string requestType, string requestRaw, out Action? requestDoneCallback)
        {
            requestDoneCallback = null;
            return null;
        }

        private void SetInitNetWorth()
        {
            foreach (var playerInfo in _gameStateInfo.PlayerInfos)
                playerInfo.NetWorth = _serverGameStateInfo.InitNetWorth;
        }

    }
}
