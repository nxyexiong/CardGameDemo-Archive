using System;
using Newtonsoft.Json;

namespace CardGameDemoServer.Networking
{

    #region Enums
    public enum GameState : int
    {
        // default
        Unknown = 0,
        // basic states
        WaitingForPlayers = 1,
        PlayersTurn = 2,
        RoundResult = 3,
        MatchResult = 4,
        // additional states
        // ..
    }

    public enum GeneralAction : int
    {
        Unknown = 0,
        FollowBet = 1,
        RaiseBet = 2,
        Fold = 3,
        Showdown = 4,
    }

    #endregion

    #region GeneralStructs

    public class CSData
    {
        public enum DataType : int
        {
            Request = 0,
            Response = 1,
        }

        public DataType Type { get; set; } = DataType.Request;
        public string Data { get; set; } = string.Empty;

        public static CSData From(string rawData)
        {
            return JsonConvert.DeserializeObject<CSData>(rawData) ??
                throw new InvalidDataException("json parse failed");
        }

        public string RawData()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class Request
    {
        public int Seq { get; set; } = -1;
        public string Type { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;

        public static Request From(string rawData)
        {
            return JsonConvert.DeserializeObject<Request>(rawData) ??
                throw new InvalidDataException("json parse failed");
        }

        public string RawData()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class Response
    {
        public int Seq { get; set; } = -1;
        public string Data { get; set; } = string.Empty;

        public static Response From(string rawData)
        {
            return JsonConvert.DeserializeObject<Response>(rawData) ??
                throw new InvalidDataException("json parse failed");
        }

        public string RawData()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    #endregion

    #region RequestsAndResponses

    public class HandshakeRequest
    {
        public string ProfileId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public static HandshakeRequest From(string rawData)
        {
            return JsonConvert.DeserializeObject<HandshakeRequest>(rawData) ??
                throw new InvalidDataException("json parse failed");
        }

        public string RawData()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class HandshakeResponse
    {
        public bool Success { get; set; } = false;

        public static HandshakeResponse From(string rawData)
        {
            return JsonConvert.DeserializeObject<HandshakeResponse>(rawData) ??
                throw new InvalidDataException("json parse failed");
        }

        public string RawData()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class UpdateGameStateRequest
    {
        public long ServerTimestampMs { get; set; } = -1;
        public GameStateInfo GameStateInfo { get; set; } = new();

        public static UpdateGameStateRequest From(string rawData)
        {
            return JsonConvert.DeserializeObject<UpdateGameStateRequest>(rawData) ??
                throw new InvalidDataException("json parse failed");
        }

        public string RawData()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class UpdateGameStateResponse
    {
        public bool Success { get; set; } = false;

        public static UpdateGameStateResponse From(string rawData)
        {
            return JsonConvert.DeserializeObject<UpdateGameStateResponse>(rawData) ??
                throw new InvalidDataException("json parse failed");
        }

        public string RawData()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class DoGeneralActionRequest
    {
        public GeneralAction Action { get; set; } = GeneralAction.Unknown;
        public string Data { get; set; } = string.Empty;

        public static DoGeneralActionRequest From(string rawData)
        {
            return JsonConvert.DeserializeObject<DoGeneralActionRequest>(rawData) ??
                throw new InvalidDataException("json parse failed");
        }

        public string RawData()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class DoGeneralActionResponse
    {
        public bool Success { get; set; } = false;
        public string Data { get; set; } = string.Empty;

        public static DoGeneralActionResponse From(string rawData)
        {
            return JsonConvert.DeserializeObject<DoGeneralActionResponse>(rawData) ??
                throw new InvalidDataException("json parse failed");
        }

        public string RawData()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    #endregion

    #region GameStructs

    public class GameStateInfo
    {
        public GameState CurrentState { get; set; } = GameState.Unknown;
        public int PlayerId { get; set; } = -1;
        public List<PlayerInfo> PlayerInfos { get; set; } = new List<PlayerInfo>();
        public int Dealer { get; set; } = -1;
        public int Aggressor { get; set; } = -1;
        public int ActivePlayer { get; set; } = -1;
        public long TimerStartTimestampMs { get; set; } = -1;
        public long TimerIntervalMs { get; set; } = -1;

        public GameStateInfo Copy()
        {
            var ret = new GameStateInfo
            {
                CurrentState = CurrentState,
                PlayerId = PlayerId,
                PlayerInfos = new List<PlayerInfo>(),
                Dealer = Dealer,
                Aggressor = Aggressor,
                ActivePlayer = ActivePlayer,
                TimerStartTimestampMs = TimerStartTimestampMs,
                TimerIntervalMs = TimerIntervalMs,
            };

            foreach (var playerInfo in PlayerInfos)
                ret.PlayerInfos.Add(playerInfo.Copy());

            return ret;
        }
    }

    public class PlayerInfo
    {
        public string Name { get; set; } = string.Empty;
        public int NetWorth { get; set; } = -1;
        public int Bet { get; set; } = -1;
        public bool IsFolded { get; set; } = false;
        public List<string> MainHand { get; set; } = new List<string>();
        public string StateData { get; set; } = string.Empty;
        public string HiddenStateData { get; set; } = string.Empty;

        public PlayerInfo Copy()
        {
            var ret = new PlayerInfo
            {
                Name = Name,
                NetWorth = NetWorth,
                Bet = Bet,
                IsFolded = IsFolded,
                MainHand = new List<string>(),
                StateData = StateData,
                HiddenStateData = HiddenStateData,
            };

            foreach (var card in MainHand)
                ret.MainHand.Add(card);

            return ret;
        }
    }

    public class PlayersTurnStateData
    {
        public List<GeneralAction> GeneralActions { get; set; } = new List<GeneralAction>();

        public static PlayersTurnStateData From(string rawData)
        {
            return JsonConvert.DeserializeObject<PlayersTurnStateData>(rawData) ??
                throw new InvalidDataException("json parse failed");
        }

        public string RawData()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class RaiseBetData
    {
        public int Bet { get; set; } = -1;

        public static RaiseBetData From(string rawData)
        {
            return JsonConvert.DeserializeObject<RaiseBetData>(rawData) ??
                throw new InvalidDataException("json parse failed");
        }

        public string RawData()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class RoundResultStateData
    {
        public bool IsWinner { get; set; } = false;
        public List<string> Hand { get; set; } = new List<string>();

        public static RoundResultStateData From(string rawData)
        {
            return JsonConvert.DeserializeObject<RoundResultStateData>(rawData) ??
                throw new InvalidDataException("json parse failed");
        }

        public string RawData()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    #endregion

}
