using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;
using Best.SocketIO;
using Best.SocketIO.Events;

public class SocketIOManager : MonoBehaviour
{
  [SerializeField] private UiManager uiManager;
  [SerializeField] private DealerController dealerController;
  [SerializeField] private BetPanelManager betPanelManager;
  internal bool isResultdone = false;
  protected string nameSpace = "playground-multiplayer"; //BackendChanges
  private Socket gameSocket; //BackendChanges
  private SocketManager manager;
  protected string SocketURI = null;
  protected string TestSocketURI = "https://devrealtime.dingdinghouse.com/";
  [SerializeField] internal JSFunctCalls JSManager;
  [SerializeField] private string testToken;
  [SerializeField] internal InitRoot initData;
  private string pendingSwitchLevel = null;

  private bool isConnected = false;
  private bool hasEverConnected = false;
  private const int MaxReconnectAttempts = 5;
  private const float ReconnectDelaySeconds = 2f;

  private float pingInterval = 2f;
  private bool waitingForPong = false;
  private int missedPongs = 0;
  private const int MaxMissedPongs = 15;
  private Coroutine PingRoutine; //Back2 end
  private bool hasFocus = true;
  private bool isExiting = false;
  private bool isBeingDestroyed = false;
  private float focusLostTime = 0f;
  private const float maxBackgroundTime = 120f;
  private Coroutine focusCheckRoutine;

  [SerializeField] private GameObject RaycastBlocker;

  void Awake()
  {
    RaycastBlocker.SetActive(true);
  }

  private void Start()
  {
    OpenSocket();
  }

  private void OnDestroy()
  {
    isBeingDestroyed = true;
  }

  private void OnApplicationFocus(bool focus)
  {
    hasFocus = focus;

    if (!focus)
    {
      focusLostTime = Time.time;

      if (focusCheckRoutine == null && !isExiting && !isBeingDestroyed)
        focusCheckRoutine = StartCoroutine(FocusTimeoutCheck());
    }
    else
    {
      if (focusCheckRoutine != null)
      {
        StopCoroutine(focusCheckRoutine);
        focusCheckRoutine = null;
      }
    }
  }

  public void CloseGame()
  {
    Debug.Log("Unity: Closing Game");
    StartCoroutine(CloseSocket());
  }

  void ReceiveAuthToken(string jsonData)
  {
    Debug.Log("Received data: " + jsonData);

    // Parse the JSON data
    var data = JsonUtility.FromJson<AuthTokenData>(jsonData);
    SocketURI = data.socketURL;
    myAuth = data.cookie;
  }

  string myAuth = null;

  private void OpenSocket()
  {
    //Create and setup SocketOptions
    SocketOptions options = new SocketOptions
    {
      AutoConnect = false,
      Reconnection = false,
      Timeout = TimeSpan.FromSeconds(3),
      ConnectWith = Best.SocketIO.Transports.TransportTypes.WebSocket //BackendChanges
    };

#if UNITY_WEBGL && !UNITY_EDITOR
    JSManager.SendCustomMessage("authToken");
    StartCoroutine(WaitForAuthToken(options));
#else
    Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
    {
      return new
      {
        token = testToken,
        // gameId = gameID
      };
    };
    options.Auth = authFunction;
    // Proceed with connecting to the server
    SetupSocketManager(options);
#endif
  }

  private IEnumerator WaitForAuthToken(SocketOptions options)
  {
    // Wait until myAuth is not null
    while (myAuth == null)
    {
      Debug.Log("My Auth is null");
      yield return null;
    }
    while (SocketURI == null)
    {
      Debug.Log("My Socket is null");
      yield return null;
    }
    Debug.Log("My Auth is not null");
    // Once myAuth is set, configure the authFunction
    Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
    {
      return new
      {
        token = myAuth,
        // gameId = gameID
      };
    };
    options.Auth = authFunction;

    Debug.Log("Auth function configured with token: " + myAuth);

    // Proceed with connecting to the server
    SetupSocketManager(options);
    yield return null;
  }

  private void SetupSocketManager(SocketOptions options)
  {
    // Create and setup SocketManager
#if UNITY_EDITOR
    this.manager = new SocketManager(new Uri(TestSocketURI), options);
#else
    this.manager = new SocketManager(new Uri(SocketURI), options);
#endif
    if (string.IsNullOrEmpty(nameSpace))
    {  //BackendChanges Start
      gameSocket = this.manager.Socket;
    }
    else
    {
      print("NameSpace: " + nameSpace);
      gameSocket = this.manager.GetSocket("/" + nameSpace);
    }
    // Set subscriptions
    gameSocket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnConnected);
    gameSocket.On(SocketIOEventTypes.Disconnect, OnDisconnected);
    gameSocket.On<Error>(SocketIOEventTypes.Error, OnError);
    gameSocket.On<string>("game:init", HandleInitData);
    gameSocket.On<string>("game:lobby_count", HandleLobbyCount);
    gameSocket.On<string>("game:round_start", HandleRoundStart);
    gameSocket.On<string>("game:betting_timer", HandleBettingTimer);
    gameSocket.On<string>("game:bonus", HandleBonus);
    gameSocket.On<string>("game:bet_placed", HandleBetPlaced);
    gameSocket.On<string>("game:card_dealt", HandleCardDealt);
    gameSocket.On<string>("game:round_end", HandleRoundEnd);
    gameSocket.On<string>("game:cashout", HandleCashout);
    gameSocket.On<string>("game:cashout_timer", HandleCashoutTimer);
    gameSocket.On<string>("game:leaderboard_update", HandleLeaderboardUpdate);
    gameSocket.On<string>("pong", OnPongReceived);
    manager.Open();
  }



  // Connected event handler implementation
  void OnConnected(ConnectResponse resp)
  {
    Debug.Log("✅ Connected to server.");

    if (hasEverConnected)
    {
      uiManager.CheckAndClosePopups();
    }

    isConnected = true;
    hasEverConnected = true;
  }

  private void OnPongReceived(string data)
  {
    // Debug.Log("✅ Received pong from server.");
    waitingForPong = false;
    missedPongs = 0;
    // Debug.Log($"⏱️ Updated last pong time: {lastPongTime}");
    // Debug.Log($"📦 Pong payload: {data}");
  }

  private void OnDisconnected()
  {
    Debug.LogWarning("⚠️ Disconnected from server.");
    isConnected = false;
    uiManager.OpenDisconnectPopup();
    ResetPingRoutine();
  }

  private void OnError(Error err)
  {
    Debug.LogError("Socket Error Message: " + err);
#if UNITY_WEBGL && !UNITY_EDITOR
    JSManager.SendCustomMessage("error");
#endif
  }

  private IEnumerator FocusTimeoutCheck()
  {
    while (!hasFocus && !isExiting && !isBeingDestroyed)
    {
      if (Time.time - focusLostTime >= maxBackgroundTime)
      {
        Debug.LogWarning("[SOCKET] Background timeout");
        isConnected = false;
        ResetPingRoutine();

        if (manager != null)
        {
          try { manager.Close(); }
          catch (Exception e) { Debug.LogWarning($"[SOCKET] Focus close error: {e.Message}"); }
        }

        uiManager.OpenDisconnectPopup();
        focusCheckRoutine = null;
        yield break;
      }

      yield return new WaitForSecondsRealtime(1f);
    }

    focusCheckRoutine = null;
  }

  private void SendPing()
  {
    waitingForPong = false;
    missedPongs = 0;
    ResetPingRoutine();
    PingRoutine = StartCoroutine(PingCheck());
  }

  void ResetPingRoutine()
  {
    if (PingRoutine != null)
    {
      StopCoroutine(PingRoutine);
    }
    PingRoutine = null;
  }

  private IEnumerator PingCheck()
  {
    while (true)
    {
      // Debug.Log($"🟡 PingCheck | waitingForPong: {waitingForPong}, missedPongs: {missedPongs}, timeSinceLastPong: {Time.time - lastPongTime}");

      if (missedPongs == 0)
      {
        uiManager.CheckAndClosePopups();
      }

      // If waiting for pong, and timeout passed
      if (waitingForPong)
      {
        if (missedPongs == 1)
        {
          uiManager.OpenReconnectPopup();
        }
        missedPongs++;
        Debug.LogWarning($"⚠️ Pong missed #{missedPongs}/{MaxMissedPongs}");

        if (missedPongs >= MaxMissedPongs)
        {
          Debug.LogError("❌ Unable to connect to server — 5 consecutive pongs missed.");
          isConnected = false;
          uiManager.OpenDisconnectPopup();
          yield break;
        }
      }

      // Send next ping
      waitingForPong = true;
      gameSocket.Emit("ping");
      yield return new WaitForSeconds(pingInterval);
    }
  }

  internal void EmitPlaceBet(int amountIndex, string betOption, Action<PlaceBetResponse> callback)
  {
    var payload = new { amountIndex = amountIndex, betType = "main_bets", betOption = betOption };
    EmitRequest("PLACE_BET", payload, (string json) =>
    {
      try
      {
        PlaceBetResponse response = JsonConvert.DeserializeObject<PlaceBetResponse>(json);
        Debug.Log("PLACE_BET RESPONSE: " + json);
        callback?.Invoke(response);
      }
      catch (Exception ex)
      {
        Debug.LogError("Error parsing PLACE_BET response: " + ex.Message);
        callback?.Invoke(null);
      }
    });
  }

  internal void EmitCancelBet(Action<CancelBetResponse> callback)
  {
    EmitRequest("CANCEL_BET", new { }, (string json) =>
    {
      try
      {
        CancelBetResponse response = JsonConvert.DeserializeObject<CancelBetResponse>(json);
        Debug.Log("CANCEL_BET RESPONSE: " + json);
        callback?.Invoke(response);
      }
      catch (Exception ex)
      {
        Debug.LogError("Error parsing CANCEL_BET response: " + ex.Message);
        callback?.Invoke(null);
      }
    });
  }

  internal void EmitDoubleBet(Action<DoubleBetResponse> callback)
  {
    EmitRequest("DOUBLE_BET", new { }, (string json) =>
    {
      try
      {
        DoubleBetResponse response = JsonConvert.DeserializeObject<DoubleBetResponse>(json);
        Debug.Log("DOUBLE_BET RESPONSE: " + json);
        callback?.Invoke(response);
      }
      catch (Exception ex)
      {
        Debug.LogError("Error parsing DOUBLE_BET response: " + ex.Message);
        callback?.Invoke(null);
      }
    });
  }

  internal void EmitRepeatBet(Action<RepeatBetResponse> callback)
  {
    EmitRequest("REPEAT_BET", new { }, (string json) =>
    {
      try
      {
        RepeatBetResponse response = JsonConvert.DeserializeObject<RepeatBetResponse>(json);
        Debug.Log("REPEAT_BET RESPONSE: " + json);
        callback?.Invoke(response);
      }
      catch (Exception ex)
      {
        Debug.LogError("Error parsing REPEAT_BET response: " + ex.Message);
        callback?.Invoke(null);
      }
    });
  }

  internal void EmitBetHistory(int page, Action<BetHistoryResponse> callback)
  {
    EmitRequest("BET_HISTORY", new { page = page }, (string json) =>
    {
      try
      {
        BetHistoryResponse response = JsonConvert.DeserializeObject<BetHistoryResponse>(json);
        Debug.Log("BET_HISTORY RESPONSE: " + json);
        callback?.Invoke(response);
      }
      catch (Exception ex)
      {
        Debug.LogError("Error parsing BET_HISTORY response: " + ex.Message);
        callback?.Invoke(null);
      }
    });
  }

  internal void EmitUndoBet(Action<UndoBetResponse> callback)
  {
    EmitRequest("UNDO_BET", new { }, (string json) =>
    {
      try
      {
        UndoBetResponse response = JsonConvert.DeserializeObject<UndoBetResponse>(json);
        Debug.Log("UNDO_BET RESPONSE: " + json);
        callback?.Invoke(response);
      }
      catch (Exception ex)
      {
        Debug.LogError("Error parsing UNDO_BET response: " + ex.Message);
        callback?.Invoke(null);
      }
    });
  }

  internal void EmitLeaveRoom()
  {
    EmitRequest("HOME", new { }, HandleLevelLeaveAck);
  }

  internal void EmitJoinRoom(string roomName)
  {
    if (string.IsNullOrEmpty(roomName))
    {
      Debug.LogError("EmitJoinRoom called with empty roomName.");
      return;
    }

    var payload = new { level = roomName };
    EmitRequest("JOIN_LEVEL", payload, HandleJoinLevelAck);
  }

  internal void EmitSwitchLevel(string roomName)
  {
    if (string.IsNullOrEmpty(roomName))
    {
      Debug.LogError("EmitSwitchLevel called with empty roomName.");
      return;
    }

    pendingSwitchLevel = roomName;
    EmitRequest("HOME", new { }, HandleSwitchLevelLeaveAck);
  }

  private void EmitRequest<T>(string requestType, T payload, Action<string> ackCallback)
  {
    try
    {
      string json = JsonConvert.SerializeObject(new { type = requestType, payload = payload });
      Debug.Log($"[EMIT] {json}");
      gameSocket.ExpectAcknowledgement(ackCallback).Emit("request", json);
    }
    catch (Exception e)
    {
      Debug.LogError($"[EMIT] {requestType} error: {e.Message}");
    }
  }

  internal IEnumerator CloseSocket()
  {
    isExiting = true;
    RaycastBlocker.SetActive(true);
    ResetPingRoutine();

    Debug.Log("Closing Socket");

    manager?.Close();
    manager = null;

    Debug.Log("Waiting for socket to close");

    yield return new WaitForSeconds(0.5f);

    Debug.Log("Socket Closed");

#if UNITY_WEBGL && !UNITY_EDITOR
    JSManager.SendCustomMessage("OnExit"); //Telling the react platform user wants to quit and go back to homepage
#endif
  }

  private void HandleInitData(string jsonObject)
  {
    Debug.Log("INIT: " + jsonObject);
    try
    {
      initData = JsonConvert.DeserializeObject<InitRoot>(jsonObject);
      if (initData != null)
      {
        SendPing();
        uiManager.SetBalanceText(initData.player.balance);
        uiManager.OnInit(initData);
#if UNITY_WEBGL && !UNITY_EDITOR
        JSManager.SendCustomMessage("OnEnter");
#endif
        if (uiManager != null && uiManager.ShouldShowStartupGuide())
        {
          uiManager.OpenStartupGuidePopup();
        }
        RaycastBlocker.SetActive(false);
      }
      else
      {
        Debug.LogError("Init data is null");
      }
    }
    catch (Exception ex)
    {
      Debug.LogError("Error parsing init data: " + ex.Message);
      return;
    }
  }

  private void HandleLobbyCount(string jsonObject)
  {
    Debug.Log("LOBBY_COUNT: " + jsonObject);
    try
    {
      LobbyCountEvent response = JsonConvert.DeserializeObject<LobbyCountEvent>(jsonObject);
      if (response != null)
      {
        uiManager.SetLobbyPlayerCounts(response.lobby);
        uiManager.SetGamePagePlayerCount(response.lobby);
        uiManager.SetLobbyTotalPlayerCount(response.totalCount);
      }
      else
      {
        Debug.LogError("Lobby count data is null");
      }
    }
    catch (Exception ex)
    {
      Debug.LogError("Error parsing lobby count data: " + ex.Message);
    }
  }

  private void HandleRoundStart(string jsonObject)
  {
    Debug.Log("ROUND_START: " + jsonObject);
    try
    {
      RoundStartEvent response = JsonConvert.DeserializeObject<RoundStartEvent>(jsonObject);
      if (response != null)
      {
        uiManager.OnRoundStart(response);
        if (dealerController != null)
        {
          float remaining = (float)((response.bettingEndTime - response.serverTime) / 1000.0);
          dealerController.OnBettingStart(remaining);
        }
      }
      else
      {
        Debug.LogError("Round start data is null");
      }
    }
    catch (Exception ex)
    {
      Debug.LogError("Error parsing round start data: " + ex.Message);
    }
  }

  private void HandleBettingTimer(string jsonObject)
  {
    Debug.Log("BETTING_TIMER: " + jsonObject);
    try
    {
      BettingTimerEvent response = JsonConvert.DeserializeObject<BettingTimerEvent>(jsonObject);
      if (response != null)
        uiManager.OnBettingTimerSync(response);
    }
    catch (Exception ex)
    {
      Debug.LogError("Error parsing betting timer data: " + ex.Message);
    }
  }

  private void HandleBonus(string jsonObject)
  {
    Debug.Log("BONUS: " + jsonObject);
    try
    {
      BonusEvent response = JsonConvert.DeserializeObject<BonusEvent>(jsonObject);
      if (response != null)
      {
        uiManager.OnBonus(response);
      }
      else
      {
        Debug.LogError("Bonus data is null");
      }
    }
    catch (Exception ex)
    {
      Debug.LogError("Error parsing bonus data: " + ex.Message);
    }
  }

  private void HandleBetPlaced(string jsonObject)
  {
    Debug.Log("BET_PLACED: " + jsonObject);
    try
    {
      BetPlacedEvent response = JsonConvert.DeserializeObject<BetPlacedEvent>(jsonObject);
      if (response != null && initData != null && response.username != initData.player.username)
      {
        if (betPanelManager != null)
        {
          if (response.amount > 0)
            betPanelManager.OnOpponentBetPlaced(response);
          else if (response.amount < 0)
            betPanelManager.OnOpponentBetUndo(response);
        }
      }
    }
    catch (Exception ex)
    {
      Debug.LogError("Error parsing bet placed data: " + ex.Message);
    }
  }

  private void HandleCardDealt(string jsonObject)
  {
    Debug.Log("CARD_DEALT: " + jsonObject);
    try
    {
      CardDealtEvent response = JsonConvert.DeserializeObject<CardDealtEvent>(jsonObject);
      if (response != null)
      {
        bool wasPending = uiManager.IsPendingLevelEntry;
        int previousCardsDealt = uiManager.PendingCardsDealt;
        uiManager.OnCardDealt(response);

        if (wasPending)
        {
          // Joined mid-deal: spawn previous cards silently then animate current
          if (dealerController != null)
            dealerController.OnJoinDuringDeal(response, previousCardsDealt);
        }
        else
        {
          if (dealerController != null)
            dealerController.OnCardDealt(response);
        }
      }
      else
      {
        Debug.LogError("Card dealt data is null");
      }
    }
    catch (Exception ex)
    {
      Debug.LogError("Error parsing card dealt data: " + ex.Message);
    }
  }

  private void HandleRoundEnd(string jsonObject)
  {
    Debug.Log("ROUND_END: " + jsonObject);
    try
    {
      RoundEndEvent response = JsonConvert.DeserializeObject<RoundEndEvent>(jsonObject);
      if (response != null)
      {
        bool wasPending = uiManager.IsPendingLevelEntry;
        uiManager.OnRoundResult(response.winner);
        if (!wasPending)
        {
          if (dealerController != null)
            dealerController.OnRoundEnd(response.winner);
          if (betPanelManager != null)
            betPanelManager.OnRoundEnd(response.winner);
        }
      }
    }
    catch (Exception ex)
    {
      Debug.LogError("Error parsing round end data: " + ex.Message);
    }
  }

  private void HandleCashout(string jsonObject)
  {
    Debug.Log("CASHOUT: " + jsonObject);
    try
    {
      CashoutEvent response = JsonConvert.DeserializeObject<CashoutEvent>(jsonObject);
      if (response != null)
      {
        bool wasPending = uiManager.IsPendingLevelEntry;
        if (!wasPending && dealerController != null)
          dealerController.OnCashout();
        uiManager.OnCashout(response);
        if (!wasPending && betPanelManager != null)
          betPanelManager.SetCashoutData(response);
      }
    }
    catch (Exception ex)
    {
      Debug.LogError("Error parsing cashout data: " + ex.Message);
    }
  }

  private void HandleCashoutTimer(string jsonObject)
  {
    Debug.Log("CASHOUT_TIMER: " + jsonObject);
    try
    {
      CashoutTimerEvent response = JsonConvert.DeserializeObject<CashoutTimerEvent>(jsonObject);
      if (response != null && betPanelManager != null)
        betPanelManager.OnCashoutTimerSync(response);
    }
    catch (Exception ex)
    {
      Debug.LogError("Error parsing cashout timer data: " + ex.Message);
    }
  }

  private void HandleLeaderboardUpdate(string jsonObject)
  {
    Debug.Log("LEADERBOARD_UPDATE: " + jsonObject);
    try
    {
      LeaderboardUpdateEvent response = JsonConvert.DeserializeObject<LeaderboardUpdateEvent>(jsonObject);
      if (response != null && response.leaderboards != null)
      {
        uiManager.OnLeaderboardUpdated(response.leaderboards);
        uiManager.SetGamePagePlayerCount(response.playerCount);
      }

    }
    catch (Exception ex)
    {
      Debug.LogError("Error parsing leaderboard update: " + ex.Message);
    }
  }

  private void HandleJoinLevelAck(string jsonObject)
  {
    Debug.Log("JOIN_LEVEL RESPONSE: " + jsonObject);
    try
    {
      JoinLevelResponse response = JsonConvert.DeserializeObject<JoinLevelResponse>(jsonObject);
      if (response != null && response.success)
      {
        uiManager.OnEnterLevelWithData(response.payload);
        pendingSwitchLevel = null;
      }
      else
      {
        Debug.LogError("Failed to join level");
        if (!string.IsNullOrEmpty(pendingSwitchLevel))
        {
          Debug.LogError("Switch level flow failed during JOIN_LEVEL.");
          pendingSwitchLevel = null;
        }
      }
    }
    catch (Exception ex)
    {
      Debug.LogError("Error parsing join level response: " + ex.Message);
    }
  }

  void HandleLevelLeaveAck(string json)
  {
    Debug.Log("LEAVE RESP: " + json);
    try
    {
      LeaveLevelResponse response = JsonConvert.DeserializeObject<LeaveLevelResponse>(json);
      if (response != null && response.success)
      {
        uiManager.SetLobbyPlayerCounts(response.payload.lobby);
        uiManager.SetBalanceText(response.payload.balance);
        uiManager.OnLeaveLevel();
      }
      else
      {
        Debug.LogError("Failed to leave level");
      }
    }
    catch (Exception ex)
    {
      Debug.LogError("Error parsing leave level response: " + ex.Message);
    }
  }

  void HandleSwitchLevelLeaveAck(string json)
  {
    Debug.Log("SWITCH LEAVE RESP: " + json);
    try
    {
      LeaveLevelResponse response = JsonConvert.DeserializeObject<LeaveLevelResponse>(json);
      if (response != null && response.success)
      {
        uiManager.SetLobbyPlayerCounts(response.payload.lobby);
        uiManager.SetBalanceText(response.payload.balance);

        if (string.IsNullOrEmpty(pendingSwitchLevel))
        {
          Debug.LogError("Switch level target is missing after leave ack.");
          return;
        }

        EmitJoinRoom(pendingSwitchLevel);
      }
      else
      {
        Debug.LogError("Failed to leave level during switch flow.");
        pendingSwitchLevel = null;
      }
    }
    catch (Exception ex)
    {
      Debug.LogError("Error parsing switch leave response: " + ex.Message);
      pendingSwitchLevel = null;
    }
  }
}

//BET PLACED EVENT
[Serializable]
public class BetPlacedEvent
{
  public string username;
  public string betId;
  public string betType;
  public string betOption;
  public double amount;
}

//AUTH
[Serializable]
public class AuthTokenData
{
  public string cookie;
  public string socketURL;
}

//COMMON

[Serializable]
public class Stats
{
  public string matchSide;
}

[Serializable]
public class Leaderboards
{
  public List<LeaderboardEntry> richest;
  public List<LeaderboardEntry> winners;
}

[Serializable]
public class LeaderboardEntry
{
  public string username;
  public double balance;
  public double totalWins;
  public int rank;
}

//LOBBY COUNT EVENT
[Serializable]
public class LobbyCountEvent
{
  public Lobby lobby;
  public int totalCount;
  public int playerCount;
}

//ROUND START EVENT
[Serializable]
public class RoundStartEvent
{
  public string roundId;
  public long startedAt;
  public long bettingEndTime;
  public long serverTime;
  public int playerCount;
}

[Serializable]
public class RoundEndEvent
{
  public string roundId;
  public int winner; //Incomepelete we receive more data here. Maybe usefull
}

[Serializable]
public class BonusEvent
{
  public string roundId;
  public int bonusPlayer;
  public double bonusMultiplier;
}

[Serializable]
public class CardDealtScores
{
  public int player_8;
  public int player_9;
  public int player_10;
  public int player_11;
}

[Serializable]
public class CardDealtEvent
{
  public string roundId;
  public Card card;
  public int player;
  public int bonusPlayer;
  public double bonusMultiplier;
  public List<Card> player8Cards;
  public List<Card> player9Cards;
  public List<Card> player10Cards;
  public List<Card> player11Cards;
  public int cardsDealt;
  public CardDealtScores scores;
}

[Serializable]
public class Card
{
  public string suit;
  public string rank;
  public string color;
}

//JOIN LEVEL ACK
[Serializable]
public class JoinLevelResponse
{
  public bool success;
  public JoinLevelResponsePayload payload;
}

[Serializable]
public class JoinLevelResponsePayload
{
  public string roomId;
  public string oldRoomId;
  public int playerCount;
  public string level;
  public List<string> stats;
  public List<BetPlacedEvent> bets;
  public Leaderboards leaderboards;
  public RoundState roundState;
}

[Serializable]
public class RoundState
{
  public string roundId;
  public long startedAt;
  public long bettingEndTime;
  public long serverTime;
  public int timeRemaining;
  public string phase;
  public int cardsDealt;
}

[Serializable]
public class BettingTimerEvent
{
  public string roundId;
  public long serverTime;
  public long bettingEndTime;
  public int timeRemaining;
}

[Serializable]
public class CashoutTimerEvent
{
  public string roundId;
  public long serverTime;
  public long cashoutEndTime;
  public int timeRemaining; // ms remaining in cashout interval
}

//LEAVE LEVEL ACK
[Serializable]
public class LeaveLevelResponse
{
  public bool success;
  public LeaveLevelResponsePayload payload;
}

[Serializable]
public class LeaveLevelResponsePayload
{
  public string message;
  public int playerCount;
  public string roomId;
  public string oldRoomId;
  public Lobby lobby;
  public double balance;
}

//INIT
[Serializable]
public class InitRoot
{
  public string id;
  public GameData gameData;
  public Player player;
}

[Serializable]
public class Bets
{
  public List<double> casual;
  public List<double> novice;
  public List<double> expert;
  public List<double> high_roller;
}

[Serializable]
public class BonusMultipliers
{
  public int player_8;
  public double player_9;
  public double player_10;
  public double player_11;
}

[Serializable]
public class GameData
{
  public List<string> betOptions;
  public int roundInterval;
  public int cardLimit;
  public int statsLimit;
  public Bets bets;
  public List<string> levels;
  public Wagers wagers;
  public Lobby lobby;
  public Leaderboards leaderboards;
  public List<object> stats;
  public BonusMultipliers bonusMultipliers;
}

[Serializable]
public class LeaderboardUpdateEvent
{
  public Leaderboards leaderboards;
  public int playerCount;
}

[Serializable]
public class CashoutEvent
{
  public Leaderboards leaderboards;
  public List<CashoutPayout> payouts;
}

[Serializable]
public class CashoutPayout
{
  public double win;
  public double balance;
  public string username;
  public string userId;
}

[Serializable]
public class Lobby
{
  public int casual;
  public int novice;
  public int expert;
  public int high_roller;
}

[Serializable]
public class MainBets
{
  public Player8 player_8;
  public Player9 player_9;
  public Player10 player_10;
  public Player11 player_11;
}

[Serializable]
public class MaxBetLimit
{
  public double casual;
  public double novice;
  public double expert;
  public double high_roller;
}

[Serializable]
public class Player
{
  public double balance;
  public string username;
}

[Serializable]
public class Player10
{
  public List<double> payout;
  public MaxBetLimit max_bet_limit;
}

[Serializable]
public class Player11
{
  public List<int> payout;
  public MaxBetLimit max_bet_limit;
}

[Serializable]
public class Player8
{
  public List<int> payout;
  public MaxBetLimit max_bet_limit;
}

[Serializable]
public class Player9
{
  public List<double> payout;
  public MaxBetLimit max_bet_limit;
}

[Serializable]
public class Wagers
{
  public MainBets main_bets;
}

//PLACE BET ACK
[Serializable]
public class PlaceBetResponse
{
  public bool success;
  public PlaceBetPayload payload;
}

[Serializable]
public class PlaceBetPayload
{
  public string username;
  public string betId;
  public double totalBet;
  public string betOption;
  public double amount;
  public double balance;
  public string message;
}

//CANCEL BET ACK
[Serializable]
public class CancelBetResponse
{
  public bool success;
  public CancelBetPayload payload;
}

[Serializable]
public class CancelBetPayload
{
  public string message;
  public double amount;
  public double balance;
  public List<CancelledBetEntry> bets;
}

[Serializable]
public class CancelledBetEntry
{
  public string betId;
  public string betType;
  public string betOption;
  public double amount;
}

//DOUBLE BET ACK
[Serializable]
public class DoubleBetResponse
{
  public bool success;
  public DoubleBetPayload payload;
}

[Serializable]
public class DoubleBetPayload
{
  public string message;
  public double balance;
  public List<DoubledBetEntry> bets;
  public double totalBet;
}

[Serializable]
public class DoubledBetEntry
{
  public string betId;
  public double amount;
  public double delta;
  public string betType;
  public string betOption;
}

//REPEAT BET ACK
[Serializable]
public class RepeatBetResponse
{
  public bool success;
  public RepeatBetPayload payload;
}

[Serializable]
public class RepeatBetPayload
{
  public List<RepeatedBetEntry> bets;
  public double totalBet;
  public double balance;
  public string message;
}

[Serializable]
public class RepeatedBetEntry
{
  public string betId;
  public double amount;
  public string betOption;
  public string betType;
}

//UNDO BET ACK
[Serializable]
public class UndoBetResponse
{
  public bool success;
  public UndoBetPayload payload;
}

[Serializable]
public class UndoBetPayload
{
  public string message;
  public double refundAmount;
  public double balance;
  public double totalBet;
  public UndoBetEntry bet;
}

[Serializable]
public class UndoBetEntry
{
  public string betId;
  public string betType;
  public string betOption;
  public double amount;
}

//BET HISTORY ACK
[Serializable]
public class BetHistoryResponse
{
  public bool success;
  public BetHistoryPayload payload;
}

[Serializable]
public class BetHistoryPayload
{
  public List<BetHistoryEntry> history;
  public BetHistoryMeta meta;
}

[Serializable]
public class BetHistoryEntry
{
  public string round_id;
  public double bet_amount;
  public double win_amount;
  public string level;
  public int win_score;
  public int cards_dealt;
  public string match_side;
  public string created_at;
}

[Serializable]
public class BetHistoryMeta
{
  public int total;
  public int page;
  public int limit;
  public int pages;
}
