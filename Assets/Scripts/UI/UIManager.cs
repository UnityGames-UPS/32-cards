using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using JetBrains.Annotations;
using System;

public class UiManager : MonoBehaviour
{
  [SerializeField] private AudioManager audioController;
  [SerializeField] private SocketIOManager socketManager;
  [SerializeField] private JSFunctCalls jsFunctCalls;
  [SerializeField] private BetPanelManager betPanelManager;
  [SerializeField] private DealerController dealerController;
  [SerializeField] private LeaderboardController leaderboardController;
  [SerializeField] private GameStatsUIController gameStatsUIController;
  [SerializeField] private BetHistoryManager betHistoryManager;
  [SerializeField] private Button HistoryClose_button;
  [SerializeField] private Button InfoClose_button;
  [SerializeField] private Button InfoLeft_button;
  [SerializeField] private Button InfoRight_button;
  [SerializeField] private List<GameObject> InfoPages_Objects;
  [SerializeField] private TMP_Text InfoPageNumberText;

  [Space(10)]
  [Header("Popus UI")]
  [SerializeField] private GameObject MainPopup_Object;
  [SerializeField] private GameObject PaytablePopup_Object;
  [SerializeField] private GameObject HistoryPopup_Object;
  [SerializeField] private GameObject InfoPopup_Object;
  [SerializeField] private GameObject StartupPanel;
  [SerializeField] private RectTransform ReconnectPopupRectTransform;
  [SerializeField] private RectTransform DisconnectPopupRectTransform;
  [SerializeField] private Button DisconnectPopupCloseButton;
  [SerializeField] private float popupBGTargetAlpha255 = 230f;

  [Space(10)]
  [Header("HomePage")]
  [SerializeField] private GameObject homePage;
  [SerializeField] private Button History;
  [SerializeField] private Button CloseStartupPanelBtn;
  [SerializeField] private Button StartupDoNotShowAgainBtn;
  [SerializeField] private GameObject StartupDoNotShowAgainCheckmark;
  [SerializeField] private Button ReadmoreStartupPanelBtn;
  [SerializeField] private RectTransform ScrollingTextRect;
  [SerializeField] private Button[] LevelButtons;
  [SerializeField] private TMP_Text[] LevelButtonsMinBetText;
  [SerializeField] private TMP_Text[] LevelButtonsMaxBetText;
  [SerializeField] private TMP_Text[] LevelButtonsPeopleText;
  [SerializeField] private TMP_Text[] LevelButtonsPeopleStaticText;
  [SerializeField] private Image[] LevelButtonsPlayerCountBg;
  [SerializeField] private Sprite LevelPlayerCountGreenBgSprite;
  [SerializeField] private Sprite LevelPlayerCountRedBgSprite;
  [SerializeField] private GameObject[] LevelButtonsHotAnimation;
  [SerializeField] private TMP_Text TotalPlayersText;
  [SerializeField] private TMP_Text HPusernameText;
  [SerializeField] private TMP_Text HPbalanceText;
  [Header("SidePanel")]
  [SerializeField] private Button MenuButton;
  [SerializeField] private Button GameRules;
  [SerializeField] private Button Sound;
  [SerializeField] private Button Music;
  [SerializeField] private Button ExpandShrink;
  [SerializeField] private Button Home;
  [SerializeField] private GameObject sidepanel;
  [SerializeField] private Button sidepanelCloseButton;
  [SerializeField] private Transform lobbyMenuButtonOpenParent;

  [Space(10)]
  [Header("GamePage")]
  [SerializeField] private GameObject gamePage;
  [SerializeField] private ImageAnimation roundAnimation;

  [SerializeField] private TMP_Text GPUsernameText;
  [SerializeField] private TMP_Text GPUsernameText2;
  [SerializeField] private TMP_Text GPminBetText;
  [SerializeField] private TMP_Text GPseparateMinBetText;
  [SerializeField] private TMP_Text GPseparateMaxBetText;
  [SerializeField] private TMP_Text GProundIDText;
  [SerializeField] private TMP_Text GPbalanceText;
  [SerializeField] private TMP_Text GPPeopleCountText;
  [Header("SidePanel")]
  [SerializeField] private Button MenuButtonGP;
  [SerializeField] private Button GameRulesGP;
  [SerializeField] private Button HistoryGP;
  [SerializeField] private Button SoundGP;
  [SerializeField] private Button MusicGP;
  [SerializeField] private Button ExpandShrinkGP;
  [SerializeField] private Button HomeGP;
  [SerializeField] private GameObject sidepanelGP;
  [SerializeField] private Button sidepanelGPCloseButton;
  [SerializeField] private Transform gpMenuButtonOpenParent;

  [Space(10)]
  [Header("LoadingPage")]
  [SerializeField] private GameObject loadingPage;
  [SerializeField] private TMP_Text loadingPageText;

  [Space(10)]
  [Header("Sound/Music Sprites")]
  [SerializeField] private Sprite SoundOnSprite;
  [SerializeField] private Sprite SoundOffSprite;
  [SerializeField] private Sprite MusicOnSprite;
  [SerializeField] private Sprite MusicOffSprite;
  [SerializeField] private Sprite ExpandSprite;
  [SerializeField] private Sprite ShrinkSprite;

  [Space(10)]
  [Header("Animation Settings")]
  [SerializeField] private float MenuButtonsDuration = 0.5f;
  [SerializeField] private float lobbyButtonsCollapsedY = 440f;
  [SerializeField] private float gpButtonsCollapsedY = 440f;

  private string currentLevel = "";
  private double currentBalance;
  private bool pendingLevelEntry = false;
  private int pendingCardsDealt = 0;
  private bool pendingCashoutReveal = false;
  private Coroutine cashoutRevealRoutine;

  internal bool IsPendingLevelEntry => pendingLevelEntry;
  internal int PendingCardsDealt => pendingCardsDealt;


  private int currentInfoPage = 0;
  private List<Button> menuButtons;
  private List<Button> menuButtonsGP;
  private bool isLobbyMenuExpanded = false;
  private bool isGPMenuExpanded = false;
  private Image mainPopupBGImage;
  private readonly List<RectTransform> menuButtonsRects = new List<RectTransform>();
  private readonly List<Vector3> menuButtonsPositions = new List<Vector3>();
  private readonly List<Vector2> menuButtonsSizes = new List<Vector2>();
  private Transform lobbyMenuButtonOriginalParent;
  private int lobbyMenuButtonOriginalSiblingIndex;
  private Tween lobbyMenuToggleTween;
  private readonly List<RectTransform> menuButtonsGPRects = new List<RectTransform>();
  private readonly List<Vector3> menuButtonsGPPositions = new List<Vector3>();
  private readonly List<Vector2> menuButtonsGPSizes = new List<Vector2>();
  private Transform gpMenuButtonOriginalParent;
  private int gpMenuButtonOriginalSiblingIndex;
  private Tween gpMenuToggleTween;
  private Vector3 scrollTextStartPosi;
  private const string DoNotShowAgainPrefKey = "doNotShowAgain";
  bool isExit;
  bool isMusic;
  bool isSound;
  bool isExpanded;
  bool disconnectedPopupOpen;
  bool reconnectPopupOpen;

  internal string CurrentLevel => currentLevel;

  private void Awake()
  {
    loadingPage.SetActive(false);
    gamePage.SetActive(false);
    homePage.SetActive(true);

    if (LevelButtons != null)
      foreach (var btn in LevelButtons)
        if (btn != null) btn.transform.localScale = Vector3.zero;
    InitializePopupViews();
    AssignButtonListeners();

    // Homepage toggle text scroll
    scrollTextStartPosi = ScrollingTextRect.anchoredPosition;
    StartScroll();

    // HomePage Menu Toggle Setup
    menuButtons = new List<Button> { GameRules, Sound, Music, ExpandShrink, Home };

    menuButtonsRects.Clear();
    menuButtonsPositions.Clear();
    menuButtonsSizes.Clear();
    foreach (var btn in menuButtons)
    {
      if (btn == null) continue;
      var rect = btn.GetComponent<RectTransform>();
      if (rect == null) continue;
      menuButtonsRects.Add(rect);
      menuButtonsPositions.Add(rect.localPosition);
      menuButtonsSizes.Add(rect.sizeDelta);
      if (btn.targetGraphic != null) btn.targetGraphic.raycastTarget = false;
    }

    SetLobbyButtonsCollapsedImmediate();

    MenuButton.onClick.RemoveAllListeners();
    MenuButton.onClick.AddListener(ToggleMenu);
    lobbyMenuButtonOriginalParent = MenuButton != null ? MenuButton.transform.parent : null;
    lobbyMenuButtonOriginalSiblingIndex = MenuButton != null ? MenuButton.transform.GetSiblingIndex() : 0;

    if (sidepanelCloseButton) sidepanelCloseButton.onClick.RemoveAllListeners();
    if (sidepanelCloseButton) sidepanelCloseButton.onClick.AddListener(RetractMenu);
    if (sidepanelCloseButton) sidepanelCloseButton.interactable = false;

    // GamePage Menu Toggle Setup
    menuButtonsGP = new List<Button> { GameRulesGP, HistoryGP, SoundGP, MusicGP, ExpandShrinkGP, HomeGP };

    menuButtonsGPRects.Clear();
    menuButtonsGPPositions.Clear();
    menuButtonsGPSizes.Clear();
    foreach (var btn in menuButtonsGP)
    {
      if (btn == null) continue;
      var rect = btn.GetComponent<RectTransform>();
      if (rect == null) continue;
      menuButtonsGPRects.Add(rect);
      menuButtonsGPPositions.Add(rect.localPosition);
      menuButtonsGPSizes.Add(rect.sizeDelta);
      if (btn.targetGraphic != null) btn.targetGraphic.raycastTarget = false;
    }

    SetGPButtonsCollapsedImmediate();

    MenuButtonGP.onClick.RemoveAllListeners();
    MenuButtonGP.onClick.AddListener(ToggleMenuGP);
    gpMenuButtonOriginalParent = MenuButtonGP != null ? MenuButtonGP.transform.parent : null;
    gpMenuButtonOriginalSiblingIndex = MenuButtonGP != null ? MenuButtonGP.transform.GetSiblingIndex() : 0;

    if (sidepanelGPCloseButton) sidepanelGPCloseButton.onClick.RemoveAllListeners();
    if (sidepanelGPCloseButton) sidepanelGPCloseButton.onClick.AddListener(RetractMenuGP);
    if (sidepanelGPCloseButton) sidepanelGPCloseButton.interactable = false;

    //HomePage Level Buttons Setup
    if (LevelButtons != null)
    {
      for (int i = 0; i < LevelButtons.Length; i++)
      {
        int levelIndex = i;
        if (LevelButtons[i] != null)
        {
          LevelButtons[i].onClick.RemoveAllListeners();
          LevelButtons[i].onClick.AddListener(delegate { StartCoroutine(TryEnterLevel(levelIndex)); });
        }
      }
    }

    RefreshStartupDoNotShowAgainVisual();

    ReconnectPopupRectTransform.localScale = Vector3.zero;
    ReconnectPopupRectTransform.gameObject.SetActive(true);
    DisconnectPopupRectTransform.localScale = Vector3.zero;
    DisconnectPopupRectTransform.gameObject.SetActive(true);

    if (jsFunctCalls != null)
      jsFunctCalls.RegisterFullscreenListener(gameObject.name);
    if (jsFunctCalls != null)
      jsFunctCalls.RegisterVisibilityListener(gameObject.name);
  }

  private void InitializePopupViews()
  {
    SetupPopupRoot(PaytablePopup_Object);
    SetupPopupRoot(HistoryPopup_Object);
    SetupPopupRoot(InfoPopup_Object);
    SetupPopupRoot(StartupPanel);

    if (MainPopup_Object != null)
    {
      MainPopup_Object.SetActive(true);
      if (MainPopup_Object.transform.childCount > 0)
      {
        Transform bgChild = MainPopup_Object.transform.GetChild(0);
        if (bgChild != null)
          mainPopupBGImage = bgChild.GetComponent<Image>();
      }

      if (mainPopupBGImage != null)
      {
        mainPopupBGImage.enabled = true;
        mainPopupBGImage.raycastTarget = false;
        Color bgColor = mainPopupBGImage.color;
        bgColor.a = 0f;
        mainPopupBGImage.color = bgColor;
      }
    }
  }

  private void SetupPopupRoot(GameObject popupRoot)
  {
    if (popupRoot == null)
      return;

    popupRoot.SetActive(true);
    popupRoot.transform.DOKill();
    popupRoot.transform.localScale = Vector3.zero;
  }

  private void AssignButtonListeners()
  {
    isMusic = true;
    isSound = true;

    if (DisconnectPopupCloseButton)
    {
      DisconnectPopupCloseButton.onClick.RemoveAllListeners();
      DisconnectPopupCloseButton.onClick.AddListener(delegate { ClientExited(); });
    }

    if (GameRules) GameRules.onClick.RemoveAllListeners();
    if (GameRules) GameRules.onClick.AddListener(delegate { OpenPopup(InfoPopup_Object); });

    if (History) History.onClick.RemoveAllListeners();
    if (History) History.onClick.AddListener(delegate { OpenHistoryPopup(); });

    if (Sound) Sound.onClick.RemoveAllListeners();
    if (Sound) Sound.onClick.AddListener(delegate { ToggleSound(); });

    if (Music) Music.onClick.RemoveAllListeners();
    if (Music) Music.onClick.AddListener(delegate { ToggleMusic(); });

    if (ExpandShrink) ExpandShrink.onClick.RemoveAllListeners();
    if (ExpandShrink) ExpandShrink.onClick.AddListener(delegate { ToggleExpandShrink(); });

    if (Home) Home.onClick.RemoveAllListeners();
    if (Home) Home.onClick.AddListener(() => ClientExited());

    if (GameRulesGP) GameRulesGP.onClick.RemoveAllListeners();
    if (GameRulesGP) GameRulesGP.onClick.AddListener(delegate { OpenPopup(InfoPopup_Object); });

    if (HistoryGP) HistoryGP.onClick.RemoveAllListeners();
    if (HistoryGP) HistoryGP.onClick.AddListener(delegate { OpenHistoryPopup(); });

    if (SoundGP) SoundGP.onClick.RemoveAllListeners();
    if (SoundGP) SoundGP.onClick.AddListener(delegate { ToggleSound(); });

    if (MusicGP) MusicGP.onClick.RemoveAllListeners();
    if (MusicGP) MusicGP.onClick.AddListener(delegate { ToggleMusic(); });

    if (ExpandShrinkGP) ExpandShrinkGP.onClick.RemoveAllListeners();
    if (ExpandShrinkGP) ExpandShrinkGP.onClick.AddListener(delegate { ToggleExpandShrink(); });

    if (HomeGP) HomeGP.onClick.RemoveAllListeners();
    if (HomeGP) HomeGP.onClick.AddListener(delegate { StartCoroutine(GoHomeButton()); });

    if (InfoLeft_button) InfoLeft_button.onClick.RemoveAllListeners();
    if (InfoLeft_button) InfoLeft_button.onClick.AddListener(delegate { GoToPreviousInfoPage(); });

    if (InfoRight_button) InfoRight_button.onClick.RemoveAllListeners();
    if (InfoRight_button) InfoRight_button.onClick.AddListener(delegate { GoToNextInfoPage(); });

    if (InfoClose_button) InfoClose_button.onClick.RemoveAllListeners();
    if (InfoClose_button) InfoClose_button.onClick.AddListener(delegate { ClosePopup(InfoPopup_Object); });

    if (HistoryClose_button) HistoryClose_button.onClick.RemoveAllListeners();
    if (HistoryClose_button) HistoryClose_button.onClick.AddListener(delegate { ClosePopup(HistoryPopup_Object); });

    if (CloseStartupPanelBtn) CloseStartupPanelBtn.onClick.RemoveAllListeners();
    if (CloseStartupPanelBtn) CloseStartupPanelBtn.onClick.AddListener(delegate { ClosePopup(StartupPanel); });

    if (StartupDoNotShowAgainBtn) StartupDoNotShowAgainBtn.onClick.RemoveAllListeners();
    if (StartupDoNotShowAgainBtn) StartupDoNotShowAgainBtn.onClick.AddListener(delegate { ToggleStartupDoNotShowAgain(); });

    if (ReadmoreStartupPanelBtn) ReadmoreStartupPanelBtn.onClick.RemoveAllListeners();
    if (ReadmoreStartupPanelBtn) ReadmoreStartupPanelBtn.onClick.AddListener(delegate { StartupPanel.SetActive(false); OpenPopup(InfoPopup_Object); });
  }

  internal bool ShouldShowStartupGuide()
  {
    return !GetDoNotShowAgain();
  }

  internal void OpenStartupGuidePopup()
  {
    RefreshStartupDoNotShowAgainVisual();
    OpenPopup(StartupPanel);
  }

  private void ToggleStartupDoNotShowAgain()
  {
    bool nextValue = !GetDoNotShowAgain();
    SetDoNotShowAgain(nextValue);
    RefreshStartupDoNotShowAgainVisual();
    if (audioController) audioController.PlaySFX(SoundEffect.ButtonClick);
  }

  private bool GetDoNotShowAgain()
  {
    return PlayerPrefs.GetInt(DoNotShowAgainPrefKey, 0) == 1;
  }

  private void SetDoNotShowAgain(bool value)
  {
    PlayerPrefs.SetInt(DoNotShowAgainPrefKey, value ? 1 : 0);
    PlayerPrefs.Save();
  }

  private void RefreshStartupDoNotShowAgainVisual()
  {
    if (StartupDoNotShowAgainCheckmark)
      StartupDoNotShowAgainCheckmark.SetActive(GetDoNotShowAgain());
  }

  IEnumerator TryEnterLevel(int level)
  {
    if (audioController) audioController.PlaySFX(SoundEffect.LobbyButton);
    string levelName = "";
    switch (level)
    {
      case 0: levelName = "casual"; break;
      case 1: levelName = "novice"; break;
      case 2: levelName = "expert"; break;
      case 3: levelName = "high_roller"; break;
    }

    if (string.IsNullOrEmpty(levelName)) Debug.LogError("Invalid level index: " + level);
    else
    {
      loadingPageText.text = "Entering a secure room";
      loadingPage.SetActive(true);

      yield return new WaitForSecondsRealtime(0.5f);

      currentLevel = levelName;
      if (socketManager) socketManager.EmitJoinRoom(levelName);
    }
  }

  private string FormatAmount(double value, bool skipKFormat = false)
  {
    return GameUtility.FormatCurrency(value, skipKFormat);
  }

  internal void SetBalanceText(double balance)
  {
    if (Math.Abs(balance - currentBalance) < 0.01) return;

    DOTween.To(() => currentBalance, x => currentBalance = x, balance, 0.5f)
        .OnUpdate(() =>
        {
          string formattedBalance = FormatAmount(currentBalance, true);
          HPbalanceText.text = formattedBalance;
          GPbalanceText.text = formattedBalance;
        });
  }

  internal void OnInit(InitRoot initData)
  {
    HPusernameText.text = initData.player.username;
    GPUsernameText.text = GameUtility.FormatUsername(initData.player.username);
    GPUsernameText2.text = initData.player.username;
    if (leaderboardController != null)
    {
      leaderboardController.Initialize();
      leaderboardController.SetLocalPlayer(initData.player.username, null);
    }

    SetLobbyPlayerCounts(initData.gameData.lobby);

    LevelButtonsMinBetText[0].text = FormatAmount(initData.gameData.bets.casual[0]);
    LevelButtonsMinBetText[1].text = FormatAmount(initData.gameData.bets.novice[0]);
    LevelButtonsMinBetText[2].text = FormatAmount(initData.gameData.bets.expert[0]);
    LevelButtonsMinBetText[3].text = FormatAmount(initData.gameData.bets.high_roller[0]);

    LevelButtonsMaxBetText[0].text = FormatAmount(initData.gameData.wagers.main_bets.player_11.max_bet_limit.casual);
    LevelButtonsMaxBetText[1].text = FormatAmount(initData.gameData.wagers.main_bets.player_11.max_bet_limit.novice);
    LevelButtonsMaxBetText[2].text = FormatAmount(initData.gameData.wagers.main_bets.player_11.max_bet_limit.expert);
    LevelButtonsMaxBetText[3].text = FormatAmount(initData.gameData.wagers.main_bets.player_11.max_bet_limit.high_roller);

    AnimateLevelButtonsIn();
  }

  private void AnimateLevelButtonsIn()
  {
    if (LevelButtons == null) return;
    for (int i = 0; i < LevelButtons.Length; i++)
    {
      if (LevelButtons[i] == null) continue;
      LevelButtons[i].transform.DOKill();
      LevelButtons[i].transform.localScale = Vector3.zero;
      LevelButtons[i].transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutQuad);
    }
  }

  internal void OnBettingTimerSync(BettingTimerEvent data)
  {
    if (betPanelManager != null)
      betPanelManager.OnBettingTimerSync(data);
  }

  internal void OnRoundStart(RoundStartEvent roundData)
  {
    if (pendingCashoutReveal)
    {
      pendingCashoutReveal = false;
      if (cashoutRevealRoutine != null) { StopCoroutine(cashoutRevealRoutine); cashoutRevealRoutine = null; }
      gamePage.SetActive(true);
      loadingPage.SetActive(false);
    }

    if (roundAnimation != null)
    {
      roundAnimation.gameObject.SetActive(true);
      roundAnimation.StartAnimation();
    }

    UpdateGamePageMinMaxTexts();

    audioController?.PlaySFX(SoundEffect.RoundStart);

    if (roundData.roundId != null && !string.IsNullOrEmpty(roundData.roundId))
      GProundIDText.text = roundData.roundId;

    if (roundData.playerCount > 0)
      GPPeopleCountText.text = roundData.playerCount.ToString();

    if (betPanelManager != null)
      betPanelManager.OnRoundStart(roundData);
  }

  internal void OnBonus(BonusEvent bonusData)
  {
    if (betPanelManager != null)
      betPanelManager.OnBonus(bonusData);
  }

  internal void OnCardDealt(CardDealtEvent cardDealtData)
  {
    if (pendingLevelEntry)
    {
      pendingLevelEntry = false;
      pendingCardsDealt = 0;
      gamePage.SetActive(true);
      loadingPage.SetActive(false);
    }

    if (betPanelManager != null)
      betPanelManager.OnCardDealt(cardDealtData);
  }

  internal void SetGamePagePlayerCount(Lobby lobby)
  {
    int count = 0;
    switch (currentLevel)
    {
      case "casual": count = lobby.casual; break;
      case "novice": count = lobby.novice; break;
      case "expert": count = lobby.expert; break;
      case "high_roller": count = lobby.high_roller; break;
    }

    GPPeopleCountText.text = count.ToString();
  }

  internal void SetGamePagePlayerCount(int count)
  {
    if (int.TryParse(GPPeopleCountText.text, out int currentCount))
    {
      if (currentCount != count)
      {
        GPPeopleCountText.text = count.ToString();
      }
    }
  }

  internal void SetLobbyPlayerCounts(Lobby lobby)
  {
    LevelButtonsPeopleStaticText[0].text = lobby.casual > 1 ? "Players" : "Player";
    LevelButtonsPeopleText[0].text = lobby.casual.ToString();

    LevelButtonsPeopleStaticText[1].text = lobby.novice > 1 ? "Players" : "Player";
    LevelButtonsPeopleText[1].text = lobby.novice.ToString();
    
    LevelButtonsPeopleStaticText[2].text = lobby.expert > 1 ? "Players" : "Player";
    LevelButtonsPeopleText[2].text = lobby.expert.ToString();
    
    LevelButtonsPeopleStaticText[3].text = lobby.high_roller > 1 ? "Players" : "Player";
    LevelButtonsPeopleText[3].text = lobby.high_roller.ToString();
    UpdateLevelButtonHotState(lobby);
  }

  private void UpdateLevelButtonHotState(Lobby lobby)
  {
    int[] counts = { lobby.casual, lobby.novice, lobby.expert, lobby.high_roller };

    // Find the hot level: highest player count; on a tie, higher index (higher bets) wins
    int hotIndex = -1;
    int maxCount = 0;
    for (int i = 0; i < counts.Length; i++)
    {
      if (counts[i] > maxCount || (counts[i] == maxCount && maxCount > 0))
      {
        maxCount = counts[i];
        hotIndex = i;
      }
    }

    for (int i = 0; i < counts.Length; i++)
    {
      bool hasPlayers = counts[i] > 0;
      bool isHot = hasPlayers && i == hotIndex;

      if (LevelButtonsPlayerCountBg != null && i < LevelButtonsPlayerCountBg.Length && LevelButtonsPlayerCountBg[i] != null)
      {
        LevelButtonsPlayerCountBg[i].gameObject.SetActive(hasPlayers);
        if (hasPlayers)
          LevelButtonsPlayerCountBg[i].sprite = isHot ? LevelPlayerCountRedBgSprite : LevelPlayerCountGreenBgSprite;
      }

      if (LevelButtonsHotAnimation != null && i < LevelButtonsHotAnimation.Length && LevelButtonsHotAnimation[i] != null)
        LevelButtonsHotAnimation[i].SetActive(isHot);
    }
  }

  internal void SetLobbyTotalPlayerCount(int count)
  {
    if (int.TryParse(TotalPlayersText.text, out int currentCount))
    {
      if (currentCount != count && count != 0)
      {
        TotalPlayersText.text = count.ToString();
      }
    }
  }

  internal void OnEnterLevelWithData(JoinLevelResponsePayload data)
  {
    pendingLevelEntry = false;
    if (cashoutRevealRoutine != null) { StopCoroutine(cashoutRevealRoutine); cashoutRevealRoutine = null; }
    pendingCashoutReveal = false;

    // Always stop any in-progress dealer/card/chip animations before processing
    // the new level state — covers all switching paths including mid-deal joins.
    if (dealerController != null) dealerController.ResetImmediate();
    if (betPanelManager != null) betPanelManager.ResetOnJoinIdle();

    if (!string.IsNullOrEmpty(data.level))
      currentLevel = data.level;

    if (data.roundState != null && !string.IsNullOrEmpty(data.roundState.roundId))
      GProundIDText.text = data.roundState.roundId;

    if (int.TryParse(GPPeopleCountText.text, out int currentCount))
    {
      if (currentCount != data.playerCount)
      {
        GPPeopleCountText.text = data.playerCount.ToString();
      }
    }

    UpdateGamePageMinMaxTexts();
    UpdateBetChipTextsForCurrentLevel();
    if (gameStatsUIController != null)
      gameStatsUIController.InitializeFromJoinStats(data.stats);

    if (leaderboardController != null)
    {
      leaderboardController.Initialize();
      leaderboardController.UpdateLeaderboard(data.leaderboards);
    }

    homePage.SetActive(false);

    // roundState == null: joined after cashout or as first player — show immediately
    if (data.roundState == null)
    {
      gamePage.SetActive(true);
      loadingPage.SetActive(false);
      return;
    }

    // Joined mid-deal — keep loading page up, wait for the next card_dealt, round_end, or cashout
    if (data.roundState.phase == "dealing")
    {
      if (betPanelManager != null && data.bets != null && data.bets.Count > 0)
        betPanelManager.SetupOpponentChipsImmediate(data.bets, socketManager?.initData?.player?.username ?? "");
      pendingLevelEntry = true;
      pendingCardsDealt = data.roundState.cardsDealt;
      return;
    }

    // Joined during cashout interval — keep loading page, reveal when countdown starts
    if (data.roundState.phase == "cashout")
    {
      pendingCashoutReveal = true;
      if (cashoutRevealRoutine != null) StopCoroutine(cashoutRevealRoutine);
      cashoutRevealRoutine = StartCoroutine(WaitAndRevealForCashout(data.roundState.timeRemaining));
      return;
    }

    // Joined during betting phase — sync countdown and show immediately
    if (betPanelManager != null) betPanelManager.OnJoinDuringBetting(data.roundState);
    if (betPanelManager != null && data.bets != null && data.bets.Count > 0)
    {
      string localUsername = socketManager?.initData?.player?.username ?? "";
      betPanelManager.SetupOpponentChipsImmediate(data.bets, localUsername);
      betPanelManager.SetupLocalChipsImmediate(data.bets, localUsername);
    }
    if (dealerController != null)
    {
      float remaining = (float)data.roundState.timeRemaining;
      dealerController.OnBettingStart(remaining);
    }
    gamePage.SetActive(true);
    loadingPage.SetActive(false);
  }

  internal void OnRoundResult(int sideValue)
  {
    audioController.PlaySFX(SoundEffect.OnRoundEnd);
    // If we joined mid-deal, cashout will arrive right after and handle the reset + page reveal
    if (pendingLevelEntry)
      return;

    if (gameStatsUIController != null)
      gameStatsUIController.OnNewRoundResult(sideValue);
  }

  internal void OnCashout(CashoutEvent cashoutEvent)
  {
    if (cashoutEvent != null && cashoutEvent.leaderboards != null && leaderboardController != null)
      leaderboardController.UpdateLeaderboard(cashoutEvent.leaderboards);

    // Joined mid-deal and the round ended before we could show the game — reset to idle and reveal
    if (pendingLevelEntry)
    {
      pendingLevelEntry = false;
      pendingCardsDealt = 0;
      if (betPanelManager != null) betPanelManager.ResetOnJoinIdle();
      if (dealerController != null) dealerController.ResetImmediate();
      gamePage.SetActive(true);
      loadingPage.SetActive(false);
      return;
    }

    if (betPanelManager != null)
      betPanelManager.OnCashout();
  }

  private IEnumerator WaitAndRevealForCashout(int timeRemainingSeconds)
  {
    int waitSeconds = Mathf.Max(0, timeRemainingSeconds - 5);
    if (waitSeconds > 0)
      yield return new WaitForSecondsRealtime(waitSeconds);

    int countdownMs = Mathf.Min(timeRemainingSeconds, 5) * 1000;
    if (betPanelManager != null)
      betPanelManager.OnJoinDuringCashout(countdownMs);

    pendingCashoutReveal = false;
    gamePage.SetActive(true);
    loadingPage.SetActive(false);

    cashoutRevealRoutine = null;
  }

  private void UpdateGamePageMinMaxTexts()
  {
    if (socketManager == null || socketManager.initData == null || socketManager.initData.gameData == null || socketManager.initData.gameData.bets == null || socketManager.initData.gameData.wagers == null || socketManager.initData.gameData.wagers.main_bets == null)
      return;

    double minBet = 0d;
    double maxBet = 0d;
    switch (currentLevel)
    {
      case "casual":
        if (socketManager.initData.gameData.bets.casual != null && socketManager.initData.gameData.bets.casual.Count > 0)
          minBet = socketManager.initData.gameData.bets.casual[0];
        maxBet = socketManager.initData.gameData.wagers.main_bets.player_11.max_bet_limit.casual;
        break;
      case "novice":
        if (socketManager.initData.gameData.bets.novice != null && socketManager.initData.gameData.bets.novice.Count > 0)
          minBet = socketManager.initData.gameData.bets.novice[0];
        maxBet = socketManager.initData.gameData.wagers.main_bets.player_11.max_bet_limit.novice;
        break;
      case "expert":
        if (socketManager.initData.gameData.bets.expert != null && socketManager.initData.gameData.bets.expert.Count > 0)
          minBet = socketManager.initData.gameData.bets.expert[0];
        maxBet = socketManager.initData.gameData.wagers.main_bets.player_11.max_bet_limit.expert;
        break;
      case "high_roller":
        if (socketManager.initData.gameData.bets.high_roller != null && socketManager.initData.gameData.bets.high_roller.Count > 0)
          minBet = socketManager.initData.gameData.bets.high_roller[0];
        maxBet = socketManager.initData.gameData.wagers.main_bets.player_11.max_bet_limit.high_roller;
        break;
      default:
        return;
    }

    string minFormatted = FormatAmount(minBet);
    string maxFormatted = FormatAmount(maxBet);

    if (GPminBetText != null)
      GPminBetText.text = minFormatted;

    if (GPseparateMinBetText != null)
      GPseparateMinBetText.text = minFormatted;

    if (GPseparateMaxBetText != null)
      GPseparateMaxBetText.text = maxFormatted;
  }

  private void UpdateBetChipTextsForCurrentLevel()
  {
    if (socketManager == null || socketManager.initData == null || socketManager.initData.gameData == null || socketManager.initData.gameData.bets == null)
      return;

    List<double> levelBets = GetBetsForLevel(currentLevel, socketManager.initData.gameData.bets);
    betPanelManager.SetChipSpritesForLevel(currentLevel);
    betPanelManager.SetChipValues(levelBets);
  }

  private List<double> GetBetsForLevel(string levelName, Bets bets)
  {
    switch (levelName)
    {
      case "casual":
        return bets.casual;
      case "novice":
        return bets.novice;
      case "expert":
        return bets.expert;
      case "high_roller":
        return bets.high_roller;
      default:
        return null;
    }
  }

  internal void OnLeaveLevel()
  {
    homePage.SetActive(true);
    gamePage.SetActive(false);
    loadingPage.SetActive(false);

    if (leaderboardController != null)
      leaderboardController.Hide();

    AnimateLevelButtonsIn();
  }

  internal IEnumerator SwitchLevelFromBetOptions(string targetLevel)
  {
    if (string.IsNullOrEmpty(targetLevel))
    {
      Debug.LogError("SwitchLevelFromBetOptions called with empty targetLevel.");
      yield break;
    }

    // Stop all dealer/card/chip state while gamePage is still active so
    // ImageAnimation.StopAnimation() (Invoke-based) can cancel cleanly.
    if (dealerController != null) dealerController.ResetImmediate();
    if (betPanelManager != null) betPanelManager.ResetOnJoinIdle();

    loadingPageText.text = "Entering a secure room";
    loadingPage.SetActive(true);
    gamePage.SetActive(false);

    yield return new WaitForSecondsRealtime(0.5f);

    socketManager.EmitSwitchLevel(targetLevel);
  }

  internal void OnLeaderboardUpdated(Leaderboards leaderboards)
  {
    if (leaderboardController == null) return;
    leaderboardController.UpdateLeaderboard(leaderboards);
  }

  IEnumerator GoHomeButton()
  {
    if (audioController) audioController.PlaySFX(SoundEffect.ButtonClick);
    RetractMenuGP();

    // Stop all dealer/card/chip state while gamePage is still active so
    // ImageAnimation.StopAnimation() (Invoke-based) can cancel cleanly.
    if (dealerController != null) dealerController.ResetImmediate();
    if (betPanelManager != null) betPanelManager.ResetOnJoinIdle();

    loadingPageText.text = "Leaving table....";
    loadingPage.SetActive(true);
    gamePage.SetActive(false);

    yield return new WaitForSecondsRealtime(0.5f);

    currentLevel = "";
    socketManager.EmitLeaveRoom();
  }

  private void ToggleMenu()
  {
    if (isLobbyMenuExpanded)
      RetractMenu();
    else
      ExpandMenu();
  }

  private void SetLobbyButtonsCollapsedImmediate()
  {
    for (int i = 0; i < menuButtonsRects.Count; i++)
    {
      var rect = menuButtonsRects[i];
      if (rect == null) continue;
      rect.localPosition = new Vector3(rect.localPosition.x, lobbyButtonsCollapsedY, rect.localPosition.z);
      rect.sizeDelta = new Vector2(menuButtonsSizes[i].x, 0f);
    }
  }

  private void AnimateLobbyButtonsOpen()
  {
    for (int i = 0; i < menuButtonsRects.Count; i++)
    {
      var rect = menuButtonsRects[i];
      if (rect == null) continue;
      rect.DOKill();
      rect.DOLocalMoveY(menuButtonsPositions[i].y, MenuButtonsDuration).SetEase(Ease.Linear);
      rect.DOSizeDelta(menuButtonsSizes[i], MenuButtonsDuration).SetEase(Ease.Linear);
    }
    if (menuButtons != null)
    {
      foreach (var btn in menuButtons)
      {
        if (btn == null) continue;
        if (btn.targetGraphic != null) btn.targetGraphic.raycastTarget = true;
      }
    }
  }

  private void AnimateLobbyButtonsClose()
  {
    for (int i = 0; i < menuButtonsRects.Count; i++)
    {
      var rect = menuButtonsRects[i];
      if (rect == null) continue;
      rect.DOKill();
      rect.DOLocalMoveY(lobbyButtonsCollapsedY, MenuButtonsDuration).SetEase(Ease.Linear);
      rect.DOSizeDelta(new Vector2(menuButtonsSizes[i].x, 0f), MenuButtonsDuration).SetEase(Ease.Linear);
    }
    if (menuButtons != null)
    {
      foreach (var btn in menuButtons)
      {
        if (btn == null) continue;
        if (btn.targetGraphic != null) btn.targetGraphic.raycastTarget = false;
      }
    }
  }

  private void ExpandMenu()
  {
    audioController.PlaySFX(SoundEffect.ButtonClick);
    sidepanel.SetActive(true); // show panel immediately
    if (sidepanelCloseButton) sidepanelCloseButton.interactable = false;
    AnimateLobbyButtonsOpen();
    if (MenuButton != null && lobbyMenuButtonOpenParent != null)
    {
      MenuButton.transform.SetParent(lobbyMenuButtonOpenParent, true);
    }

    lobbyMenuToggleTween?.Kill();
    lobbyMenuToggleTween = DOVirtual.DelayedCall(MenuButtonsDuration, () =>
    {
      if (sidepanelCloseButton) sidepanelCloseButton.interactable = true;
    });

    isLobbyMenuExpanded = true;
  }

  private void RetractMenu()
  {
    audioController.PlaySFX(SoundEffect.ButtonClick);
    if (sidepanelCloseButton) sidepanelCloseButton.interactable = false;
    AnimateLobbyButtonsClose();
    if (MenuButton != null && lobbyMenuButtonOriginalParent != null)
    {
      MenuButton.transform.SetParent(lobbyMenuButtonOriginalParent, true);
      MenuButton.transform.SetSiblingIndex(lobbyMenuButtonOriginalSiblingIndex);
    }
    lobbyMenuToggleTween?.Kill();
    lobbyMenuToggleTween = DOVirtual.DelayedCall(MenuButtonsDuration, () =>
    {
      sidepanel.SetActive(false);
    });

    isLobbyMenuExpanded = false;
  }

  internal void OpenDisconnectPopup()
  {
    if (isExit)
      return;

    disconnectedPopupOpen = true;
    DisconnectPopupRectTransform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
  }

  internal void OpenReconnectPopup()
  {
    if (isExit)
      return;

    reconnectPopupOpen = true;
    ReconnectPopupRectTransform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
  }

  internal void CheckAndClosePopups()
  {
    if (reconnectPopupOpen)
    {
      ReconnectPopupRectTransform.DOKill();
      ReconnectPopupRectTransform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack);
      reconnectPopupOpen = false;
    }
    if (disconnectedPopupOpen)
    {
      DisconnectPopupRectTransform.DOKill();
      DisconnectPopupRectTransform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack);
      disconnectedPopupOpen = false;
    }
  }

  private void ClientExited()
  {
    audioController?.PlaySFX(SoundEffect.ButtonClick);
    isExit = true;
    StartCoroutine(socketManager.CloseSocket());
  }

  private void OpenHistoryPopup()
  {
    if (betHistoryManager != null) betHistoryManager.OpenPanel();
    OpenPopup(HistoryPopup_Object);
  }

  internal void OpenPopup(GameObject Popup)
  {
    if (audioController) audioController.PlaySFX(SoundEffect.ButtonClick);
    if (mainPopupBGImage != null)
    {
      mainPopupBGImage.DOKill();
      mainPopupBGImage.raycastTarget = true;
      Color bgColor = mainPopupBGImage.color;
      bgColor.a = 0f;
      mainPopupBGImage.color = bgColor;
      mainPopupBGImage.DOFade(Mathf.Clamp01(popupBGTargetAlpha255 / 255f), 0.5f);
    }

    if (Popup)
    {
      Popup.SetActive(true);
      if (Popup == InfoPopup_Object)
      {
        currentInfoPage = 0;
        UpdateInfoUI();
      }
      var rect = Popup.transform;
      rect.DOKill();

      // Start from small
      rect.localScale = Vector3.zero;

      // Scale up with bounce
      rect.DOScale(Vector3.one, 0.35f)
          .SetEase(Ease.OutBack);
    }
  }

  internal void ClosePopup(GameObject Popup)
  {
    if (audioController) audioController.PlaySFX(SoundEffect.ButtonClick);

    if (mainPopupBGImage != null)
    {
      mainPopupBGImage.DOKill();
      mainPopupBGImage.DOFade(0f, 0.6f).OnComplete(() =>
      {
        if (mainPopupBGImage != null)
          mainPopupBGImage.raycastTarget = false;
      });
    }

    if (Popup)
    {
      var rect = Popup.transform;
      rect.DOKill();

      // Scale down smoothly
      rect.DOScale(Vector3.zero, 0.45f)
          .SetEase(Ease.InBack)
          .OnComplete(() =>
          {
            Popup.SetActive(false);
          });
    }
    else
    {
      if (mainPopupBGImage != null)
        mainPopupBGImage.raycastTarget = false;
    }
  }

  private void ToggleMusic()
  {
    audioController?.PlaySFX(SoundEffect.ButtonClick);
    isMusic = !isMusic;
    Sprite musicSprite = isMusic ? MusicOnSprite : MusicOffSprite;
    if (Music != null && musicSprite != null)
    {
      var img = Music.GetComponent<Image>();
      if (img != null) img.sprite = musicSprite;
    }
    if (MusicGP != null && musicSprite != null)
    {
      var img = MusicGP.GetComponent<Image>();
      if (img != null) img.sprite = musicSprite;
    }
    if (audioController) audioController.ToggleMusicMute(!isMusic);
  }

  private void ToggleSound()
  {
    audioController?.PlaySFX(SoundEffect.ButtonClick);
    isSound = !isSound;
    Sprite soundSprite = isSound ? SoundOnSprite : SoundOffSprite;
    if (Sound != null && soundSprite != null)
    {
      var img = Sound.GetComponent<Image>();
      if (img != null) img.sprite = soundSprite;
    }
    if (SoundGP != null && soundSprite != null)
    {
      var img = SoundGP.GetComponent<Image>();
      if (img != null) img.sprite = soundSprite;
    }
    bool mute = !isSound;
    if (audioController) audioController.ToggleSoundMute(mute);
  }

  private void ToggleExpandShrink()
  {
    audioController?.PlaySFX(SoundEffect.ButtonClick);
    isExpanded = !isExpanded;
    ApplyExpandShrinkSprites();
    if (jsFunctCalls != null)
    {
      if (isExpanded)
        jsFunctCalls.RequestExpandGame();
      else
        jsFunctCalls.RequestShrinkGame();
    }
  }

  // Called by the JS visibility/focus listener when the tab or window gains/loses focus
  public void OnFocusChanged(string value)
  {
    bool focused = value == "1";
    if (focused)
      audioController?.ResumeAudio();
    else
      audioController?.PauseAllAudio();
    socketManager?.HandleFocusChange(focused);
  }

  // Called by the JS fullscreen change listener when the user exits fullscreen externally (e.g. Escape key)
  public void OnFullscreenChanged(string value)
  {
    isExpanded = value == "1";
    ApplyExpandShrinkSprites();
  }

  private void ApplyExpandShrinkSprites()
  {
    Sprite sprite = isExpanded ? ShrinkSprite : ExpandSprite;
    string label = isExpanded ? "Shrink" : "Expand";
    if (ExpandShrink != null)
    {
      if (sprite != null)
      {
        var img = ExpandShrink.GetComponent<Image>();
        if (img != null) img.sprite = sprite;
      }
      if (ExpandShrink.transform.childCount > 0)
      {
        var txt = ExpandShrink.transform.GetChild(0).GetComponent<TMP_Text>();
        if (txt != null) txt.text = label;
      }
    }
    if (ExpandShrinkGP != null)
    {
      if (sprite != null)
      {
        var img = ExpandShrinkGP.GetComponent<Image>();
        if (img != null) img.sprite = sprite;
      }
      if (ExpandShrinkGP.transform.childCount > 0)
      {
        var txt = ExpandShrinkGP.transform.GetChild(0).GetComponent<TMP_Text>();
        if (txt != null) txt.text = label;
      }
    }
  }

  private void UpdateInfoUI()
  {
    if (InfoPages_Objects == null || InfoPages_Objects.Count == 0)
    {
      if (InfoPageNumberText != null) InfoPageNumberText.text = "0/0";
      if (InfoLeft_button) InfoLeft_button.interactable = false;
      if (InfoRight_button) InfoRight_button.interactable = false;
      return;
    }

    currentInfoPage = Mathf.Clamp(currentInfoPage, 0, InfoPages_Objects.Count - 1);
    for (int i = 0; i < InfoPages_Objects.Count; i++)
      InfoPages_Objects[i].SetActive(i == currentInfoPage);

    if (InfoPageNumberText != null)
      InfoPageNumberText.text = $"{currentInfoPage + 1}/{InfoPages_Objects.Count}";

    if (InfoLeft_button) InfoLeft_button.interactable = currentInfoPage > 0;
    if (InfoRight_button) InfoRight_button.interactable = currentInfoPage < InfoPages_Objects.Count - 1;
  }

  private void GoToPreviousInfoPage()
  {
    if (InfoPages_Objects == null || InfoPages_Objects.Count == 0) return;
    if (currentInfoPage <= 0) return;
    if (audioController) audioController.PlaySFX(SoundEffect.ButtonClick);
    currentInfoPage--;
    UpdateInfoUI();
  }

  private void GoToNextInfoPage()
  {
    if (InfoPages_Objects == null || InfoPages_Objects.Count == 0) return;
    if (currentInfoPage >= InfoPages_Objects.Count - 1) return;
    if (audioController) audioController.PlaySFX(SoundEffect.ButtonClick);
    currentInfoPage++;
    UpdateInfoUI();
  }

  void StartScroll()
  {
    // Start at "fromX"
    ScrollingTextRect.anchoredPosition = new Vector2(1000f, scrollTextStartPosi.y);

    // Tween to "toX"
    ScrollingTextRect.DOAnchorPosX(-1000f, 10f)
        .SetEase(Ease.Linear)
        .OnComplete(() =>
        {
          ScrollingTextRect.anchoredPosition = new Vector2(1000f, scrollTextStartPosi.y);
          StartScroll(); // repeat
        });
  }

  private void ToggleMenuGP()
  {
    if (isGPMenuExpanded)
      RetractMenuGP();
    else
      ExpandMenuGP();
  }

  private void SetGPButtonsCollapsedImmediate()
  {
    for (int i = 0; i < menuButtonsGPRects.Count; i++)
    {
      var rect = menuButtonsGPRects[i];
      if (rect == null) continue;
      rect.localPosition = new Vector3(rect.localPosition.x, gpButtonsCollapsedY, rect.localPosition.z);
      rect.sizeDelta = new Vector2(menuButtonsGPSizes[i].x, 0f);
    }
  }

  private void AnimateGPButtonsOpen()
  {
    for (int i = 0; i < menuButtonsGPRects.Count; i++)
    {
      var rect = menuButtonsGPRects[i];
      if (rect == null) continue;
      rect.DOKill();
      rect.DOLocalMoveY(menuButtonsGPPositions[i].y, MenuButtonsDuration).SetEase(Ease.Linear);
      rect.DOSizeDelta(menuButtonsGPSizes[i], MenuButtonsDuration).SetEase(Ease.Linear);
    }
    if (menuButtonsGP != null)
    {
      foreach (var btn in menuButtonsGP)
      {
        if (btn == null) continue;
        if (btn.targetGraphic != null) btn.targetGraphic.raycastTarget = true;
      }
    }
  }

  private void AnimateGPButtonsClose()
  {
    for (int i = 0; i < menuButtonsGPRects.Count; i++)
    {
      var rect = menuButtonsGPRects[i];
      if (rect == null) continue;
      rect.DOKill();
      rect.DOLocalMoveY(gpButtonsCollapsedY, MenuButtonsDuration).SetEase(Ease.Linear);
      rect.DOSizeDelta(new Vector2(menuButtonsGPSizes[i].x, 0f), MenuButtonsDuration).SetEase(Ease.Linear);
    }
    if (menuButtonsGP != null)
    {
      foreach (var btn in menuButtonsGP)
      {
        if (btn == null) continue;
        if (btn.targetGraphic != null) btn.targetGraphic.raycastTarget = false;
      }
    }
  }

  private void ExpandMenuGP()
  {
    audioController.PlaySFX(SoundEffect.ButtonClick);
    sidepanelGP.SetActive(true); // show panel immediately
    if (sidepanelGPCloseButton) sidepanelGPCloseButton.interactable = false;
    AnimateGPButtonsOpen();
    if (MenuButtonGP != null && gpMenuButtonOpenParent != null)
    {
      MenuButtonGP.transform.SetParent(gpMenuButtonOpenParent, true);
    }

    gpMenuToggleTween?.Kill();
    gpMenuToggleTween = DOVirtual.DelayedCall(MenuButtonsDuration, () =>
    {
      if (sidepanelGPCloseButton) sidepanelGPCloseButton.interactable = true;
    });

    isGPMenuExpanded = true;
  }

  private void RetractMenuGP()
  {
    audioController.PlaySFX(SoundEffect.ButtonClick);
    if (sidepanelGPCloseButton) sidepanelGPCloseButton.interactable = false;
    AnimateGPButtonsClose();
    if (MenuButtonGP != null && gpMenuButtonOriginalParent != null)
    {
      MenuButtonGP.transform.SetParent(gpMenuButtonOriginalParent, true);
      MenuButtonGP.transform.SetSiblingIndex(gpMenuButtonOriginalSiblingIndex);
    }
    gpMenuToggleTween?.Kill();
    gpMenuToggleTween = DOVirtual.DelayedCall(MenuButtonsDuration, () =>
    {
      sidepanelGP.SetActive(false);
    });

    isGPMenuExpanded = false;
  }
}
