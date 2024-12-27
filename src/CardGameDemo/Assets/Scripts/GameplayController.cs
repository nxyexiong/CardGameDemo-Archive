using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Networking;

public class GameplayController : MonoBehaviour
{
    public Text DealerText;
    public Text AggressorText;
    public Text ActivePlayerText;
    public Text TimerText;

    public Text Player0NameText;
    public Text Player0NetWorthText;
    public Text Player0BetText;
    public Text Player0IsFoldedText;

    public Text Player1NameText;
    public Text Player1NetWorthText;
    public Text Player1BetText;
    public Text Player1IsFoldedText;

    public Text Player2NameText;
    public Text Player2NetWorthText;
    public Text Player2BetText;
    public Text Player2IsFoldedText;

    public Text Player3NameText;
    public Text Player3NetWorthText;
    public Text Player3BetText;
    public Text Player3IsFoldedText;

    public Text InstructionText;
    public GameObject CardPrefab;
    public GameObject ActionButtonPrefab;
    public Transform MainHandTransform;
    public Transform ActionTransform;

    private string _profileId = string.Empty;
    private string _name = string.Empty;
    private Client _client;
    private bool _connected = false;
    private long _tsDelta = 0; // server time - local time
    private GameStateInfo _localGameStateInfo = new();
    private readonly List<GameObject> _mainHandCards = new(); // card objects
    private readonly List<GameObject> _actions = new(); // action button objects

    void Start()
    {
        // TODO: profile id
        _profileId = PlayerPrefs.GetString("PlayerName") ?? string.Empty;
        _name = PlayerPrefs.GetString("PlayerName") ?? string.Empty;

        // init client
        _client = GetComponent<Client>();
        _client.SetListener(nameof(UpdateGameStateRequest), x => OnUpdateGameStateRequest(x));
        _client.SetConnectionStatusListener((connected) =>
        {
            if (connected && !_connected)
            {
                // start handshake
                var request = new HandshakeRequest { ProfileId = _profileId, Name = _name };
                _ = _client.SendRequest(nameof(HandshakeRequest), request.RawData(), (string responseRaw) =>
                {
                    var response = HandshakeResponse.From(responseRaw);
                    if (!response.Success)
                        Debug.LogError($"HandshakeResponse failed");
                });
            }
            else if (!connected && _connected)
            {
                // TODO
            }
            _connected = connected;
        });
    }

    void Update()
    {
        SetTimer();
    }

    void OnDestroy()
    {
        _client.RemoveListener(nameof(UpdateGameStateRequest));
    }

    private string OnUpdateGameStateRequest(string requestRaw)
    {
        // parse
        var request = UpdateGameStateRequest.From(requestRaw);

        // sync time
        _tsDelta = request.ServerTimestampMs - Common.TimeUtils.GetTimestampMs(DateTime.Now);
        SetTimer();

        // update game state
        UpdateGameState(request.GameStateInfo);

        return new UpdateGameStateResponse { Success = true }.RawData();
    }

    private void UpdateGameState(GameStateInfo gameStateInfo)
    {
        // copy info
        _localGameStateInfo = gameStateInfo.Copy();
        _localGameStateInfo.PlayerInfos.Clear();
        var playerCount = gameStateInfo.PlayerInfos.Count;
        var curId = gameStateInfo.PlayerId;
        for (var i = 0; i < playerCount; i++)
        {
            var playerInfo = gameStateInfo.PlayerInfos[curId++ % playerCount];
            _localGameStateInfo.PlayerInfos.Add(playerInfo);
        }
        _localGameStateInfo.Dealer = (gameStateInfo.Dealer - gameStateInfo.PlayerId + playerCount) % playerCount;
        _localGameStateInfo.Aggressor = (gameStateInfo.Aggressor - gameStateInfo.PlayerId + playerCount) % playerCount;
        _localGameStateInfo.ActivePlayer = (gameStateInfo.ActivePlayer - gameStateInfo.PlayerId + playerCount) % playerCount;

        // set global state
        var dealer = _localGameStateInfo.Dealer;
        if (dealer >= 0)
            DealerText.text = _localGameStateInfo.PlayerInfos[dealer].Name;
        var aggressor = _localGameStateInfo.Aggressor;
        if (aggressor >= 0)
            AggressorText.text = _localGameStateInfo.PlayerInfos[aggressor].Name;
        var activePlayer = _localGameStateInfo.ActivePlayer;
        if (activePlayer >= 0)
        ActivePlayerText.text = _localGameStateInfo.PlayerInfos[activePlayer].Name;

        // TODO: handle different state using dedicated classes
        if (_localGameStateInfo.CurrentState == GameState.WaitingForPlayers)
        {
            ClearGeneralActionList();
            InstructionText.text = "waiting for other players to join...";
        }
        else if (_localGameStateInfo.CurrentState == GameState.PlayersTurn)
        {
            UpdateGeneralActionList();
            if (activePlayer == 0)
                InstructionText.text = "your turn";
            else
                InstructionText.text = $"waiting for {_localGameStateInfo.PlayerInfos[activePlayer].Name}...";
        }
        else if (_localGameStateInfo.CurrentState == GameState.RoundResult)
        {
            ClearGeneralActionList();

            PlayerInfo winnerInfo = null;
            RoundResultStateData winnerRoundRst = null;
            foreach (var playerInfo in _localGameStateInfo.PlayerInfos)
            {
                var roundRst = RoundResultStateData.From(playerInfo.StateData);
                if (roundRst.IsWinner)
                {
                    winnerInfo = playerInfo;
                    winnerRoundRst = roundRst;
                    break;
                }
            }
            InstructionText.text = $"round end, the winner is {winnerInfo.Name} "
                + $"with hand: {string.Join(',', winnerRoundRst.Hand)}";
        }
        else if (_localGameStateInfo.CurrentState == GameState.MatchResult)
        {
            ClearGeneralActionList();
            InstructionText.text = "match ended";
        }

        // set player state
        if (_localGameStateInfo.PlayerInfos.Count > 0)
        {
            Player0NameText.text = _localGameStateInfo.PlayerInfos[0].Name;
            Player0NetWorthText.text = _localGameStateInfo.PlayerInfos[0].NetWorth.ToString();
            Player0BetText.text = _localGameStateInfo.PlayerInfos[0].Bet.ToString();
            Player0IsFoldedText.text = _localGameStateInfo.PlayerInfos[0].IsFolded.ToString();
            UpdateMainHand(_localGameStateInfo.PlayerInfos[0].MainHand);
        }
        if (_localGameStateInfo.PlayerInfos.Count > 1)
        {
            Player1NameText.text = _localGameStateInfo.PlayerInfos[1].Name;
            Player1NetWorthText.text = _localGameStateInfo.PlayerInfos[1].NetWorth.ToString();
            Player1BetText.text = _localGameStateInfo.PlayerInfos[1].Bet.ToString();
            Player1IsFoldedText.text = _localGameStateInfo.PlayerInfos[1].IsFolded.ToString();
        }
        if (_localGameStateInfo.PlayerInfos.Count > 2)
        {
            Player2NameText.text = _localGameStateInfo.PlayerInfos[2].Name;
            Player2NetWorthText.text = _localGameStateInfo.PlayerInfos[2].NetWorth.ToString();
            Player2BetText.text = _localGameStateInfo.PlayerInfos[2].Bet.ToString();
            Player2IsFoldedText.text = _localGameStateInfo.PlayerInfos[2].IsFolded.ToString();
        }
        if (_localGameStateInfo.PlayerInfos.Count > 3)
        {
            Player3NameText.text = _localGameStateInfo.PlayerInfos[3].Name;
            Player3NetWorthText.text = _localGameStateInfo.PlayerInfos[3].NetWorth.ToString();
            Player3BetText.text = _localGameStateInfo.PlayerInfos[3].Bet.ToString();
            Player3IsFoldedText.text = _localGameStateInfo.PlayerInfos[3].IsFolded.ToString();
        }
    }

    private void SetTimer()
    {
        var nowMs = Common.TimeUtils.GetTimestampMs(DateTime.Now);
        var startTimeMs = _localGameStateInfo.TimerStartTimestampMs - _tsDelta;
        var intervalMs = _localGameStateInfo.TimerIntervalMs;
        var timeLeftMs = startTimeMs + intervalMs - nowMs;

        if (timeLeftMs <= 0)
            TimerText.text = "waiting...";
        else
            TimerText.text = (timeLeftMs / 1000).ToString();
    }

    private void UpdateMainHand(IEnumerable<string> cardFaceRaws)
    {
        // add or remove objects
        var newCardCount = cardFaceRaws.Count();
        while (_mainHandCards.Count > newCardCount)
        {
            Destroy(_mainHandCards.First());
            _mainHandCards.RemoveAt(0);
        }
        while (_mainHandCards.Count < newCardCount)
        {
            var card = Instantiate(CardPrefab);
            card.transform.SetParent(MainHandTransform, false);
            _mainHandCards.Add(card);
        }

        // set position
        for (var i = 0; i < _mainHandCards.Count; i++)
        {
            var card = _mainHandCards[i];
            var cardRectTransform = card.GetComponent<RectTransform>();
            var width = cardRectTransform.rect.width;
            var height = cardRectTransform.rect.height;
            cardRectTransform.anchoredPosition = new Vector2(
                width * (0.5f + 1.0f * i) + 5.0f * i,
                -height * 0.5f);
        }

        // set state
        for (var i = 0; i < _mainHandCards.Count; i++)
        {
            var card = _mainHandCards[i];
            var cardController = card.GetComponent<CardController>();
            cardController.SetCardFaceFromRaw(cardFaceRaws.ElementAt(i));
            cardController.IsFacingUp = true;
        }
    }

    private void UpdateGeneralActionList()
    {
        var playerInfo = _localGameStateInfo.PlayerInfos[0];
        var stateData = PlayersTurnStateData.From(playerInfo.StateData);

        // add or remove objects
        var newActionCount = stateData.GeneralActions.Count;
        while (_actions.Count > newActionCount)
        {
            Destroy(_actions.First());
            _actions.RemoveAt(0);
        }
        while (_actions.Count < newActionCount)
        {
            var actionBtn = Instantiate(ActionButtonPrefab);
            actionBtn.transform.SetParent(ActionTransform, false);
            _actions.Add(actionBtn);
        }

        // set position
        for (var i = 0; i < _actions.Count; i++)
        {
            var actionBtn = _actions[i];
            var actionBtnRectTransform = actionBtn.GetComponent<RectTransform>();
            var width = actionBtnRectTransform.rect.width;
            var height = actionBtnRectTransform.rect.height;
            actionBtnRectTransform.anchoredPosition = new Vector2(
                width * 0.5f,
                -height * (0.5f + 1.0f * i) + 5.0f * i);
        }

        // set content
        for (var i = 0; i < _actions.Count; i++)
        {
            var actionBtnController = _actions[i].GetComponent<ActionButtonController>();
            var action = stateData.GeneralActions.ElementAt(i);
            actionBtnController.ActionName = action.ToString();
            actionBtnController.Text.text = GetGeneralActionButtonText(action);
        }

        // add listener
        for (var i = 0; i < _actions.Count; i++)
        {
            var actionBtn = _actions[i];
            actionBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                OnActionButtonClick(actionBtn);
                ClearGeneralActionList();
            });
        }
    }

    private void ClearGeneralActionList()
    {
        foreach (var btn in _actions)
            Destroy(btn);
        _actions.Clear();
    }

    private string GetGeneralActionButtonText(GeneralAction action)
    {
        if (action == GeneralAction.FollowBet)
            return "Follow bet";
        else if (action == GeneralAction.RaiseBet)
            return "Raise bet";
        else if (action == GeneralAction.Fold)
            return "Fold";
        else if (action == GeneralAction.Showdown)
            return "Showdown";
        return "unassigned";
    }

    private void OnActionButtonClick(GameObject actionBtn)
    {
        var actionBtnController = actionBtn.GetComponent<ActionButtonController>();
        var actionRaw = actionBtnController.ActionName;
        if (Enum.TryParse(actionRaw, out GeneralAction action))
        {
            DoGeneralAction(action, string.Empty);
            return;
        }
        Debug.LogError($"OnActionButtonClick, unknown action {actionRaw}");
    }

    private void DoGeneralAction(GeneralAction action, string data)
    {
        // build request
        var request = new DoGeneralActionRequest { Action = action };
        if (action == GeneralAction.RaiseBet)
        {
            var raiseBetData = new RaiseBetData { Bet = 5 };
            request.Data = raiseBetData.RawData();
        }

        _client.SendRequest(nameof(DoGeneralActionRequest), request.RawData(), (string responseRaw) =>
        {
            var response = DoGeneralActionResponse.From(responseRaw);
            if (!response.Success)
                Debug.LogError($"DoGeneralAction, response failed");
        });
    }
}
