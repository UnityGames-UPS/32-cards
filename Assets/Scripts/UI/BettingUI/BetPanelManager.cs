using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BetPanelManager : MonoBehaviour
{
  [Serializable]
  private class AnnouncerView
  {
    public CanvasGroup canvasGroup;
  }

  [Serializable]
  private class ChipButtonView
  {
    public Button button;
    public Image chipImage;
    public TMP_Text chipValueText;
  }

  [Serializable]
  private class LevelChipSprites
  {
    public string levelName;
    public Sprite mainChipSprite;
    public List<Sprite> chipOptionSprites;
  }

  [Serializable]
  private class BetSpotView
  {
    public Button betButton;
    public RectTransform chipParent;
    public RectTransform chipSpawnArea;
    public RectTransform totalBetRoot;
    public TMP_Text totalBetText;
    public CanvasGroup lightGlow;
    public CanvasGroup darkGlow;
    public ImageAnimation winningSpotAnimBg;
    public ImageAnimation winningSpotAnimFg;
    public RectTransform combinedTotalRoot;
    public TMP_Text combinedTotalText;
    public TMP_Text bonusMultText;
    public GameObject bonusElectricEffect;
    public ImageAnimation bonusBgAnim;
    public ImageAnimation bonusLightningAnim;
    public Image borderGlow;
    [NonSerialized] public Vector2 totalBetBaseSize;
  }

  private struct BetUndoEntry
  {
    public int SpotIndex;
    public BetChipView ChipView;
  }

  private class OpponentChipEntry
  {
    public BetChipView ChipView;
    public string Username;
  }

  [SerializeField] private AudioManager audioController;

  [Header("Chip Selector")]
  [SerializeField] private ChipButtonView mainChip;
  [SerializeField] private List<ChipButtonView> chipOptions;
  [SerializeField] private Button chipOptionsBGCloseButton;
  [SerializeField] private float chipOptionsFirstOffsetY = 142f;
  [SerializeField] private float chipOptionsSpacingY = 122f;
  [SerializeField] private float chipOptionsAnimDuration = 0.2f;

  [Header("Bet Spots")]
  [SerializeField] private List<BetSpotView> betSpots;
  [SerializeField] private BetChipView betChipPrefab;
  [SerializeField] private float chipSpawnYOffset = 80f;
  [SerializeField] private float chipSpawnDuration = 0.25f;
  [SerializeField] private RectTransform chipUndoDestroyTarget;
  [SerializeField] private float chipUndoDuration = 0.5f;

  [Header("Bet Action Buttons")]
  [SerializeField] private RectTransform betActionsPanel;
  [SerializeField] private Button undoBetButton;
  [SerializeField] private Button cancelBetButton;
  [SerializeField] private Button doubleBetButton;
  [SerializeField] private float betActionsAnimDuration = 0.5f;
  [SerializeField] private float betActionsExpandedWidth = 396f;
  [SerializeField] private float undoButtonExpandedX = -320f;
  [SerializeField] private float cancelButtonExpandedX = -229f;
  [SerializeField] private float doubleButtonExpandedX = -117f;

  [Header("References")]
  [SerializeField] private SocketIOManager socketManager;
  [SerializeField] private UiManager uiManager;

  [Header("Repeat Bet Button")]
  [SerializeField] private Button repeatBetButton;
  [SerializeField] private float rebetButtonExpandedX = -200f;
  [SerializeField] private float rebetPanelExpandedWidth = 200f;
  [SerializeField] private float rebetAnimDuration = 0.5f;

  [Header("Betting Controls Parent")]
  [SerializeField] private RectTransform bettingControlsParent;
  [SerializeField] private float bettingControlsHideOffsetY = -150f;
  [SerializeField] private float bettingControlsAnimDuration = 0.4f;

  [Header("Total Stake Display")]
  [SerializeField] private RectTransform TotalStakeParent;
  [SerializeField] private TMP_Text TotalStakeValueText;
  [SerializeField] private float totalStakeHideOffsetY = -150f;
  [SerializeField] private float totalStakeAnimDuration = 0.25f;
  [SerializeField] private float totalStakePreDelay = 1f;
  [SerializeField] private float totalStakeHoldDuration = 5f;

  [Header("Opponent Chips")]
  [SerializeField] private BetChipView opponentChipPrefab;
  [SerializeField] private Transform opponentChipStartRef;
  [SerializeField] private Transform opponentChipMovingParent;
  [SerializeField] private float opponentChipMoveDuration = 0.4f;
  [SerializeField] private float opponentChipMoveScale = 0.8f;
  [SerializeField] private float opponentChipScaleUpDuration = 0.3f;
  [SerializeField] private LeaderboardController leaderboardController;

  [Header("Chip Ripple Effects")]
  [SerializeField] private List<Sprite> goldenRippleSprites;
  [SerializeField] private List<Sprite> blueRippleSprites;

  [Header("Level Chip Sprites")]
  [SerializeField] private List<LevelChipSprites> levelChipSpriteConfigs;

  [Header("Round End Overlays")]
  [SerializeField] private float roundEndDelay = 2f;
  [SerializeField] private float overlayFadeDuration = 0.5f;
  [SerializeField] private float losingChipsFadeOutDuration = 0.3f;
  [SerializeField] private float overlayStayDuration = 1.5f;

  [Header("Winning Chips")]
  [SerializeField] private Transform winningChipStartRef;
  [SerializeField] private Transform winningChipMovingParent;
  [SerializeField] private Transform leaderboardChipMovingParent;
  [SerializeField] private float preWinningChipDelay = 0.5f;
  [SerializeField] private float winningChipSpawnInterval = 0.2f;
  [SerializeField] private float winningChipMoveDuration = 0.5f;
  [SerializeField] private float winningChipHoldDuration = 0.5f;
  [SerializeField] private float chipReturnDuration = 0.5f;
  [SerializeField] private float winTotalLerpDuration = 0.8f;

  [Header("Error Popup")]
  [SerializeField] private RectTransform errorPopupRoot;
  [SerializeField] private CanvasGroup errorPopupCanvasGroup;
  [SerializeField] private TMP_Text errorPopupText;
  [SerializeField] private float popupFadeInDuration = 0.35f;
  [SerializeField] private float popupStayDuration = 2f;
  [SerializeField] private float popupFadeOutDuration = 0.35f;

  [Header("Combined Total")]
  [SerializeField] private float combinedTotalScaleDuration = 0.25f;
  [SerializeField] private float combinedTotalClearDelay = 0.3f;

  [Header("Border Glow Pulse")]
  [SerializeField] private float borderGlowFadeDuration = 1.5f;
  [SerializeField] private float BorderGlowHoldDuration = 0.5f;

  [Header("Bonus Multiplier")]
  [SerializeField] private float bonusMultShowDelay = 0.5f;
  [SerializeField] private float bonusMultScaleFactor = 1.25f;
  [SerializeField] private float bonusMultScaleUpDuration = 0.2f;
  [SerializeField] private float bonusMultScaleDownDuration = 0.2f;
  [SerializeField] private float bonusMultFadeOutDuration = 0.3f;
  [SerializeField] private float bonusWinnerScaleDelay = 0.3f;
  [SerializeField] private float bonusMultHideDelay = 0.4f;

  [Header("Round Result Text")]
  [SerializeField] private RectTransform roundResultParent;
  [SerializeField] private CanvasGroup roundResultCanvasGroup;
  [SerializeField] private TMP_Text roundResultText;
  [SerializeField] private float roundResultHideOffsetY = -150f;
  [SerializeField] private float roundResultFadeInDuration = 0.5f;
  [SerializeField] private float roundResultSlowMoveDuration = 1.5f;
  [SerializeField] private float roundResultFadeOutDuration = 0.5f;

  [Header("Round Announcer")]
  [SerializeField] private RectTransform announcerParent;
  [SerializeField] private AnnouncerView lightGreenAnnouncer;
  [SerializeField] private AnnouncerView yellowAnnouncer;
  [SerializeField] private AnnouncerView pinkAnnouncer;
  [SerializeField] private AnnouncerView darkGreenAnnouncer;
  [SerializeField] private RectTransform timerTextRoot;
  [SerializeField] private CanvasGroup timerTextCanvasGroup;
  [SerializeField] private TMP_Text timerText;
  [SerializeField] private Color bettingTimerColor = Color.white;
  [SerializeField] private Color finalCountdownTimerColor = Color.white;
  [SerializeField] private float announcerFadeDuration = 0.15f;
  [SerializeField] private float timerFadeDuration = 0.15f;
  [SerializeField] private float timerScalePunch = 1.65f;
  [SerializeField] private float announcerScalePunch = 1.2f;
  [SerializeField] private float scaleUpDuration = 0.15f;
  [SerializeField] private float scaleDownDuration = 0.15f;
  [SerializeField] private float postRoundAnimationDelay = 10f;

  private readonly Stack<BetUndoEntry> betUndoStack = new Stack<BetUndoEntry>();
  private readonly List<List<BetChipView>> chipsPerSpot = new List<List<BetChipView>>();
  private readonly List<List<OpponentChipEntry>> opponentChipsPerSpot = new List<List<OpponentChipEntry>>();
  private readonly List<Sprite> cachedChipOptionSprites = new List<Sprite>();
  private Sprite cachedMainChipSprite;

  private static readonly string[] BetOptionNames = { "player_8", "player_9", "player_10", "player_11" };

  private Vector3 errorPopupInitLocalPos;
  private Sequence errorPopupSequence;

  private float bettingControlsBaseY;
  private bool playerPlacedBetThisRound;
  private bool hasPreviousBetData;
  private bool isRebetExpanded;

  private bool areChipOptionsExpanded;
  private bool areBetActionsExpanded;
  private Coroutine roundCountdownRoutine;
  private Coroutine nextRoundRoutine;
  private bool hasReceivedFirstCardDealt;
  private string activeRoundId;
  private Vector3 timerTextBaseScale = Vector3.one;
  private Vector3 announcerParentBaseScale = Vector3.one;

  private int currentWinner = -1;
  private int currentBonusPlayer = -1;
  private Coroutine bonusMultAnimRoutine;
  private Coroutine bonusLightningAnimRoutine;
  private CashoutEvent pendingCashoutData;
  private Coroutine cashoutAnimationRoutine;
  private readonly List<BetChipView> winningClientChips = new List<BetChipView>();
  private readonly List<OpponentChipEntry> winningOpponentChips = new List<OpponentChipEntry>();

  private Sequence borderGlowSequence;

  private float totalStakeBaseY;
  private Coroutine totalStakeRoutine;

  private float roundResultBaseY;
  private double roundTotalBet;
  private Coroutine roundResultRoutine;


  private void Awake()
  {
    if (errorPopupRoot != null)
    {
      errorPopupInitLocalPos = errorPopupRoot.localPosition;
      if (errorPopupCanvasGroup != null)
        errorPopupCanvasGroup.alpha = 0f;
    }

    if (bettingControlsParent != null)
      bettingControlsBaseY = bettingControlsParent.anchoredPosition.y;

    if (TotalStakeParent != null)
    {
      totalStakeBaseY = TotalStakeParent.anchoredPosition.y;
      Vector2 tsPos = TotalStakeParent.anchoredPosition;
      TotalStakeParent.anchoredPosition = new Vector2(tsPos.x, totalStakeBaseY + totalStakeHideOffsetY);
    }

    if (roundResultParent != null)
    {
      roundResultBaseY = roundResultParent.anchoredPosition.y;
      Vector2 rrPos = roundResultParent.anchoredPosition;
      roundResultParent.anchoredPosition = new Vector2(rrPos.x, roundResultBaseY + roundResultHideOffsetY);
      if (roundResultCanvasGroup != null)
        roundResultCanvasGroup.alpha = 0f;
    }

    if (betSpots != null)
      foreach (var spot in betSpots)
      {
        if (spot?.combinedTotalRoot != null)
          spot.combinedTotalRoot.localScale = Vector3.zero;
        if (spot?.bonusLightningAnim != null)
          spot.bonusLightningAnim.gameObject.SetActive(false);
      }
  }

  private void Start()
  {
    CacheChipSprites();
    InitializeSpotState();
    InitializeRoundAnnouncerState();
    BindButtonListeners();
  }

  private void OnDisable()
  {
    if (errorPopupSequence != null && errorPopupSequence.IsActive())
      errorPopupSequence.Kill();
    StopRoundRoutines();
    StopCashoutAnimation();
    DOTween.Kill(timerTextRoot);
    KillAnnouncerTweens(lightGreenAnnouncer);
    KillAnnouncerTweens(yellowAnnouncer);
    KillAnnouncerTweens(pinkAnnouncer);
    KillAnnouncerTweens(darkGreenAnnouncer);
    if (announcerParent != null)
      announcerParent.DOKill();
    if (timerTextCanvasGroup != null)
      timerTextCanvasGroup.DOKill();
    if (bettingControlsParent != null)
      bettingControlsParent.DOKill();
    if (roundResultRoutine != null) { StopCoroutine(roundResultRoutine); roundResultRoutine = null; }
    if (roundResultParent != null) roundResultParent.DOKill();
    if (roundResultCanvasGroup != null) roundResultCanvasGroup.DOKill();
  }

  private void InitializeSpotState()
  {
    if (betSpots == null)
      betSpots = new List<BetSpotView>();

    if (chipOptions == null)
      chipOptions = new List<ChipButtonView>();

    chipsPerSpot.Clear();
    opponentChipsPerSpot.Clear();
    for (int i = 0; i < betSpots.Count; i++)
    {
      if (betSpots[i] != null && betSpots[i].totalBetRoot != null)
        betSpots[i].totalBetBaseSize = betSpots[i].totalBetRoot.sizeDelta;

      chipsPerSpot.Add(new List<BetChipView>());
      opponentChipsPerSpot.Add(new List<OpponentChipEntry>());
      UpdateSpotTotal(i);
      UpdateCombinedTotal(i, true);
    }

    if (repeatBetButton != null)
      repeatBetButton.gameObject.SetActive(false);

    if (chipOptionsBGCloseButton != null)
    {
      chipOptionsBGCloseButton.gameObject.SetActive(false);
    }

    if (mainChip != null && mainChip.button != null)
    {
      float baseY = mainChip.button.transform.localPosition.y;
      for (int i = 0; i < chipOptions.Count; i++)
      {
        var option = chipOptions[i];
        if (option == null || option.button == null)
          continue;

        option.button.transform.localPosition = new Vector3(option.button.transform.localPosition.x, baseY, option.button.transform.localPosition.z);
        option.button.gameObject.SetActive(false);
      }
    }
  }

  private void BindButtonListeners()
  {
    if (mainChip != null && mainChip.button != null)
    {
      mainChip.button.onClick.RemoveAllListeners();
      mainChip.button.onClick.AddListener(ToggleChipOptions);
    }

    if (chipOptionsBGCloseButton != null)
    {
      chipOptionsBGCloseButton.onClick.RemoveAllListeners();
      chipOptionsBGCloseButton.onClick.AddListener(RetractChipOptions);
    }

    for (int i = 0; i < chipOptions.Count; i++)
    {
      var option = chipOptions[i];
      if (option == null || option.button == null)
        continue;

      option.button.onClick.RemoveAllListeners();
      option.button.onClick.AddListener(() => ApplySelectedChipOption(option));
    }

    for (int i = 0; i < betSpots.Count; i++)
    {
      int spotIndex = i;
      var spot = betSpots[i];
      if (spot == null || spot.betButton == null)
        continue;

      spot.betButton.onClick.RemoveAllListeners();
      spot.betButton.onClick.AddListener(() => PlaceBetOnSpot(spotIndex));
    }

    if (undoBetButton != null)
    {
      undoBetButton.onClick.RemoveAllListeners();
      undoBetButton.onClick.AddListener(UndoLastBet);
    }

    if (cancelBetButton != null)
    {
      cancelBetButton.onClick.RemoveAllListeners();
      cancelBetButton.onClick.AddListener(CancelAllBets);
    }

    if (doubleBetButton != null)
    {
      doubleBetButton.onClick.RemoveAllListeners();
      doubleBetButton.onClick.AddListener(DoubleAllBets);
    }

    if (repeatBetButton != null)
    {
      repeatBetButton.onClick.RemoveAllListeners();
      repeatBetButton.onClick.AddListener(RepeatLastRoundBets);
    }
  }

  internal void SetChipValues(List<double> orderedBets)
  {
    if (orderedBets == null || orderedBets.Count == 0)
      return;

    if (mainChip != null && mainChip.chipValueText != null)
      mainChip.chipValueText.text = GameUtility.FormatCurrency(orderedBets[0]);

    for (int i = 0; i < chipOptions.Count; i++)
    {
      if (i + 1 >= orderedBets.Count)
        break;

      ChipButtonView option = chipOptions[i];
      if (option == null || option.chipValueText == null)
        continue;

      option.chipValueText.text = GameUtility.FormatCurrency(orderedBets[i + 1]);
    }
  }

  internal void RestoreCachedChipSprites()
  {
    if (mainChip != null && mainChip.chipImage != null)
      mainChip.chipImage.sprite = cachedMainChipSprite;

    for (int i = 0; i < chipOptions.Count && i < cachedChipOptionSprites.Count; i++)
    {
      ChipButtonView option = chipOptions[i];
      if (option == null || option.chipImage == null)
        continue;

      option.chipImage.sprite = cachedChipOptionSprites[i];
    }
  }

  internal void SetChipSpritesForLevel(string levelName)
  {
    LevelChipSprites config = null;
    if (levelChipSpriteConfigs != null)
    {
      foreach (var c in levelChipSpriteConfigs)
      {
        if (c != null && c.levelName == levelName)
        {
          config = c;
          break;
        }
      }
    }

    if (config == null)
    {
      RestoreCachedChipSprites();
      return;
    }

    if (mainChip != null && mainChip.chipImage != null && config.mainChipSprite != null)
      mainChip.chipImage.sprite = config.mainChipSprite;

    if (config.chipOptionSprites != null)
    {
      for (int i = 0; i < chipOptions.Count && i < config.chipOptionSprites.Count; i++)
      {
        ChipButtonView option = chipOptions[i];
        if (option == null || option.chipImage == null) continue;
        if (config.chipOptionSprites[i] != null)
          option.chipImage.sprite = config.chipOptionSprites[i];
      }
    }
  }

  internal void ResetOnJoinIdle()
  {
    StopRoundRoutines();
    StopBorderGlowLoop();
    StopCashoutAnimation();
    activeRoundId = null;
    hasReceivedFirstCardDealt = false;
    currentWinner = -1;
    pendingCashoutData = null;
    playerPlacedBetThisRound = false;
    hasPreviousBetData = false;
    ClearAllChipVisuals();
    HideAllAnnouncers();
    SetTimerVisible(false, false);
    ResetOverlays();
    CollapseBetActionButtons();
    CollapseRebetButton(true);
    if (areChipOptionsExpanded)
      RetractChipOptions();
    HideTotalStakeImmediate();
    ResetRoundResultImmediate();
    AnimateBettingControlsY(bettingControlsBaseY + bettingControlsHideOffsetY, true);
  }

  internal void OnJoinDuringBetting(RoundState roundState)
  {
    if (roundState == null) return;

    StopRoundRoutines();
    StopCashoutAnimation();
    activeRoundId = roundState.roundId;
    hasReceivedFirstCardDealt = false;
    currentWinner = -1;
    pendingCashoutData = null;
    playerPlacedBetThisRound = false;
    hasPreviousBetData = false;
    ClearAllChipVisuals();
    HideAllAnnouncers();
    SetTimerVisible(false, false);
    ResetOverlays();
    CollapseBetActionButtons();
    CollapseRebetButton(true);
    if (areChipOptionsExpanded)
      RetractChipOptions();
    AnimateBettingControlsY(bettingControlsBaseY + bettingControlsHideOffsetY, true);
    AnimateBettingControlsY(bettingControlsBaseY, false);

    RoundStartEvent synced = new RoundStartEvent
    {
      roundId = roundState.roundId,
      startedAt = roundState.startedAt,
      bettingEndTime = roundState.bettingEndTime,
      serverTime = roundState.serverTime
    };
    roundCountdownRoutine = StartCoroutine(RunBettingCountdown(synced));
  }

  internal void OnCashoutTimerSync(CashoutTimerEvent data)
  {
    if (data == null) return;
    if (!string.IsNullOrEmpty(activeRoundId) && !string.IsNullOrEmpty(data.roundId) && activeRoundId != data.roundId) return;

    long serverRemainingMs = data.cashoutEndTime - data.serverTime;
    if (serverRemainingMs <= 0) return;

    // In the visible countdown phase, skip resync if display drift is within 1 tick
    bool inCountdown = serverRemainingMs <= 5000;
    if (inCountdown && timerText != null && int.TryParse(timerText.text, out int displayed))
    {
      int expected = Mathf.Clamp(Mathf.CeilToInt(serverRemainingMs / 1000f) - 1, 0, 4);
      if (Mathf.Abs(displayed - expected) <= 1)
        return;
    }

    // Always resync — the default postRoundAnimationDelay may drift vs actual server interval
    if (nextRoundRoutine != null)
    {
      StopCoroutine(nextRoundRoutine);
      nextRoundRoutine = null;
    }
    nextRoundRoutine = StartCoroutine(RunNextRoundCountdown((int)serverRemainingMs));
  }

  internal void OnJoinDuringCashout(int timeRemainingMs)
  {
    StopRoundRoutines();
    StopCashoutAnimation();
    activeRoundId = null;
    hasReceivedFirstCardDealt = false;
    currentWinner = -1;
    pendingCashoutData = null;
    playerPlacedBetThisRound = false;
    hasPreviousBetData = false;
    ClearAllChipVisuals();
    HideAllAnnouncers();
    SetTimerVisible(false, false);
    ResetOverlays();
    CollapseBetActionButtons();
    CollapseRebetButton(true);
    if (areChipOptionsExpanded)
      RetractChipOptions();
    AnimateBettingControlsY(bettingControlsBaseY + bettingControlsHideOffsetY, true);

    if (timeRemainingMs > 0)
      nextRoundRoutine = StartCoroutine(RunNextRoundCountdown(timeRemainingMs));
  }

  internal void OnBettingTimerSync(BettingTimerEvent data)
  {
    if (data == null) return;
    if (!string.IsNullOrEmpty(activeRoundId) && !string.IsNullOrEmpty(data.roundId) && activeRoundId != data.roundId) return;

    long serverRemainingMs = data.bettingEndTime - data.serverTime;
    int serverSeconds = Mathf.Clamp(Mathf.CeilToInt(serverRemainingMs / 1000f) - 1, 0, 14);

    // Only resync if there is noticeable drift (>1 second off)
    if (timerText != null && int.TryParse(timerText.text, out int displayed) && Mathf.Abs(displayed - serverSeconds) <= 1)
      return;

    if (roundCountdownRoutine != null)
    {
      StopCoroutine(roundCountdownRoutine);
      roundCountdownRoutine = null;
    }

    RoundStartEvent synced = new RoundStartEvent
    {
      roundId = data.roundId,
      bettingEndTime = data.bettingEndTime,
      serverTime = data.serverTime
    };
    roundCountdownRoutine = StartCoroutine(RunBettingCountdown(synced));
  }

  internal void OnRoundStart(RoundStartEvent roundData)
  {
    if (roundData == null)
      return;

    activeRoundId = roundData.roundId;
    hasReceivedFirstCardDealt = false;
    roundTotalBet = 0;

    if (bonusMultAnimRoutine != null)
    {
      StopCoroutine(bonusMultAnimRoutine);
      bonusMultAnimRoutine = null;
    }
    currentBonusPlayer = -1;

    StopCashoutAnimation();
    ResetOverlays();
    ClearAllChipVisuals();
    CollapseBetActionButtons();
    StartBorderGlowLoop();

    bool shouldExpandRebet = hasPreviousBetData;
    playerPlacedBetThisRound = false;
    AnimateBettingControlsY(bettingControlsBaseY, false);
    if (shouldExpandRebet)
      ExpandRebetButton();
    else
      CollapseRebetButton(true);

    if (nextRoundRoutine != null)
    {
      StopCoroutine(nextRoundRoutine);
      nextRoundRoutine = null;
    }

    if (roundCountdownRoutine != null)
      StopCoroutine(roundCountdownRoutine);

    roundCountdownRoutine = StartCoroutine(RunBettingCountdown(roundData));
  }

  internal void OnBonus(BonusEvent bonusData)
  {
    if (bonusData == null)
      return;

    if (!string.IsNullOrEmpty(activeRoundId) && !string.IsNullOrEmpty(bonusData.roundId) && activeRoundId != bonusData.roundId)
      return;

    if (roundCountdownRoutine != null)
    {
      StopCoroutine(roundCountdownRoutine);
      roundCountdownRoutine = null;
    }

    StopBorderGlowLoop();

    audioController.PlaySFX(SoundEffect.BetLocked);

    FadeTimer(false);
    FadeToAnnouncer(pinkAnnouncer, true);

    bool hasChips = HasAnyClientChips();
    playerPlacedBetThisRound = hasChips;
    if (hasChips) hasPreviousBetData = true;
    roundTotalBet = GetTotalClientBet();

    CollapseRebetButton(true);

    AnimateBettingControlsY(bettingControlsBaseY + bettingControlsHideOffsetY, false);

    currentBonusPlayer = bonusData.bonusPlayer;
    int bSpot = bonusData.bonusPlayer - 8;
    if (IsValidSpotIndex(bSpot))
    {
      var spot = betSpots[bSpot];
      double mult = GetBonusMultiplier(bonusData.bonusPlayer);
      if (spot.bonusMultText != null)
        spot.bonusMultText.text = "x" + (mult % 1 == 0 ? ((long)mult).ToString() : mult.ToString("F1"));
      if (bonusMultAnimRoutine != null) StopCoroutine(bonusMultAnimRoutine);
      bonusMultAnimRoutine = StartCoroutine(ShowBonusMultAnim(bSpot));
    }

    if (bonusLightningAnimRoutine != null)
    {
      StopCoroutine(bonusLightningAnimRoutine);
      bonusLightningAnimRoutine = null;
    }
    bonusLightningAnimRoutine = StartCoroutine(PlayLightningAnimStaggered());

    ShowTotalStake();
  }

  internal void OnCardDealt(CardDealtEvent cardDealtData)
  {
    if (cardDealtData == null || hasReceivedFirstCardDealt)
      return;

    if (!string.IsNullOrEmpty(activeRoundId) && !string.IsNullOrEmpty(cardDealtData.roundId) && activeRoundId != cardDealtData.roundId)
      return;

    hasReceivedFirstCardDealt = true;
    HideAllAnnouncers();
  }

  internal void OnRoundEnd(int winner)
  {
    currentWinner = winner;
    if (cashoutAnimationRoutine != null)
      StopCoroutine(cashoutAnimationRoutine);
    cashoutAnimationRoutine = StartCoroutine(RunCashoutAnimation());
  }

  internal void SetCashoutData(CashoutEvent cashoutData)
  {
    pendingCashoutData = cashoutData;
  }

  internal void OnCashout()
  {
    if (nextRoundRoutine != null)
      StopCoroutine(nextRoundRoutine);

    nextRoundRoutine = StartCoroutine(RunNextRoundCountdown());
  }

  internal void OnOpponentBetPlaced(BetPlacedEvent betData)
  {
    if (betData == null || betData.amount <= 0)
      return;

    int spotIndex = BetOptionToSpotIndex(betData.betOption);
    if (spotIndex < 0 || !IsValidSpotIndex(spotIndex))
      return;

    SpawnOpponentChipOnSpot(spotIndex, betData.amount, betData.username);
  }

  internal void OnOpponentBetUndo(BetPlacedEvent betData)
  {
    if (betData == null) return;

    int spotIndex = BetOptionToSpotIndex(betData.betOption);
    if (spotIndex < 0 || !IsValidSpotIndex(spotIndex)) return;

    // Find the last chip from this opponent on this spot
    var list = opponentChipsPerSpot[spotIndex];
    OpponentChipEntry entry = null;
    for (int i = list.Count - 1; i >= 0; i--)
    {
      if (list[i] != null && list[i].Username == betData.username)
      {
        entry = list[i];
        list.RemoveAt(i);
        UpdateCombinedTotal(spotIndex);
        break;
      }
    }

    if (entry?.ChipView == null || entry.ChipView.ChipRect == null) return;

    Vector3 targetPos;
    Transform moveParent = null;

    if (leaderboardController != null && !string.IsNullOrEmpty(entry.Username))
    {
      RectTransform lbRect = leaderboardController.GetPlayerPosition(entry.Username, false);
      if (lbRect == null)
        lbRect = leaderboardController.GetPlayerPosition(entry.Username, true);

      if (lbRect != null)
      {
        targetPos = lbRect.position;
        moveParent = leaderboardChipMovingParent;
      }
      else
      {
        targetPos = opponentChipStartRef != null ? opponentChipStartRef.position : entry.ChipView.ChipRect.position;
      }
    }
    else
    {
      targetPos = opponentChipStartRef != null ? opponentChipStartRef.position : entry.ChipView.ChipRect.position;
    }

    if (moveParent != null)
      entry.ChipView.ChipRect.SetParent(moveParent);

    entry.ChipView.ChipRect.DOKill();
    var chipView = entry.ChipView;
    entry.ChipView.ChipRect.DOMove(targetPos, chipUndoDuration)
      .SetEase(Ease.InBack)
      .OnComplete(() => { if (chipView != null) Destroy(chipView.gameObject); });
  }

  internal void SetupOpponentChipsImmediate(List<BetPlacedEvent> bets, string localUsername)
  {
    if (bets == null || bets.Count == 0) return;

    foreach (var bet in bets)
    {
      if (bet == null || bet.amount <= 0) continue;
      if (bet.username == localUsername) continue;

      int spotIndex = BetOptionToSpotIndex(bet.betOption);
      if (spotIndex < 0 || !IsValidSpotIndex(spotIndex)) continue;

      SpawnOpponentChipImmediate(spotIndex, bet.amount, bet.username);
    }
  }

  private void SpawnOpponentChipImmediate(int spotIndex, double amount, string username)
  {
    if (!IsValidSpotIndex(spotIndex) || opponentChipPrefab == null) return;

    var spot = betSpots[spotIndex];
    if (spot == null || spot.chipParent == null || spot.chipSpawnArea == null) return;

    BetChipView spawnedChip = Instantiate(opponentChipPrefab, spot.chipParent);
    if (spawnedChip == null || spawnedChip.ChipRect == null || spawnedChip.ChipCanvasGroup == null) return;

    spawnedChip.ChipCanvasGroup.alpha = 1f;
    spawnedChip.ChipRect.localScale = Vector3.one;
    spawnedChip.ChipRect.localRotation = Quaternion.identity;
    spawnedChip.SetChipValueText(GameUtility.FormatCurrency(amount));

    var rippleSprites = GetRippleSpritesForUser(username);
    if (rippleSprites != null) spawnedChip.ShowRipple(rippleSprites);

    Vector2 finalPos = GetRandomAnchoredPosition(spawnedChip.ChipRect, spot.chipSpawnArea);
    spawnedChip.ChipRect.anchoredPosition = finalPos;

    opponentChipsPerSpot[spotIndex].Add(new OpponentChipEntry { ChipView = spawnedChip, Username = username });
    UpdateCombinedTotal(spotIndex, true);
  }

  internal void SetupLocalChipsImmediate(List<BetPlacedEvent> bets, string localUsername)
  {
    if (bets == null || bets.Count == 0) return;

    bool anySpawned = false;
    foreach (var bet in bets)
    {
      if (bet == null || bet.amount <= 0) continue;
      if (bet.username != localUsername) continue;

      int spotIndex = BetOptionToSpotIndex(bet.betOption);
      if (spotIndex < 0 || !IsValidSpotIndex(spotIndex)) continue;

      SpawnLocalChipImmediate(spotIndex, bet.amount);
      anySpawned = true;
    }

    if (anySpawned)
    {
      playerPlacedBetThisRound = true;
      hasPreviousBetData = true;
      CollapseRebetButton(true);
      ExpandBetActionButtonsImmediate();
    }
  }

  private void SpawnLocalChipImmediate(int spotIndex, double amount)
  {
    if (!IsValidSpotIndex(spotIndex) || betChipPrefab == null)
      return;

    var spot = betSpots[spotIndex];
    if (spot == null || spot.chipParent == null || spot.chipSpawnArea == null)
      return;

    BetChipView spawnedChip = Instantiate(betChipPrefab, spot.chipParent);
    if (spawnedChip == null || spawnedChip.ChipRect == null || spawnedChip.ChipCanvasGroup == null)
      return;

    spawnedChip.ChipCanvasGroup.alpha = 1f;
    spawnedChip.ChipRect.localScale = Vector3.one;
    spawnedChip.ChipRect.localRotation = Quaternion.identity;

    string chipValueText = GameUtility.FormatCurrency(amount);
    Sprite chipSprite = mainChip != null && mainChip.chipImage != null ? mainChip.chipImage.sprite : null;
    spawnedChip.SetChipVisuals(chipSprite, chipValueText);

    string localUsername = socketManager != null && socketManager.initData != null ? socketManager.initData.player.username : null;
    var rippleSprites = GetRippleSpritesForUser(localUsername);
    if (rippleSprites != null) spawnedChip.ShowRipple(rippleSprites);

    Vector2 finalPos = GetRandomAnchoredPosition(spawnedChip.ChipRect, spot.chipSpawnArea);
    spawnedChip.ChipRect.anchoredPosition = finalPos;

    chipsPerSpot[spotIndex].Add(spawnedChip);
    betUndoStack.Push(new BetUndoEntry { SpotIndex = spotIndex, ChipView = spawnedChip });

    UpdateSpotTotal(spotIndex);
    UpdateCombinedTotal(spotIndex);
  }

  private void SpawnOpponentChipOnSpot(int spotIndex, double amount, string username)
  {
    if (!IsValidSpotIndex(spotIndex) || opponentChipPrefab == null)
      return;

    var spot = betSpots[spotIndex];
    if (spot == null || spot.chipParent == null || spot.chipSpawnArea == null)
      return;

    Transform movingParent = opponentChipMovingParent != null ? opponentChipMovingParent : spot.chipParent;
    BetChipView spawnedChip = Instantiate(opponentChipPrefab, movingParent);
    if (spawnedChip == null || spawnedChip.ChipRect == null || spawnedChip.ChipCanvasGroup == null)
      return;

    spawnedChip.ChipCanvasGroup.alpha = 1f;
    spawnedChip.ChipRect.localScale = Vector3.one * opponentChipMoveScale;
    spawnedChip.ChipRect.localRotation = Quaternion.identity;
    spawnedChip.SetChipValueText(GameUtility.FormatCurrency(amount));

    var rippleSprites = GetRippleSpritesForUser(username);
    if (rippleSprites != null) spawnedChip.ShowRipple(rippleSprites);

    Vector2 finalPos = GetRandomAnchoredPosition(spawnedChip.ChipRect, spot.chipSpawnArea);
    spawnedChip.ChipRect.SetParent(spot.chipParent);
    spawnedChip.ChipRect.anchoredPosition = finalPos;
    Vector3 endWorldPos = spawnedChip.ChipRect.position;
    spawnedChip.ChipRect.SetParent(movingParent);

    Vector3 startPos = opponentChipStartRef != null ? opponentChipStartRef.position : endWorldPos;
    if (leaderboardController != null && !string.IsNullOrEmpty(username))
    {
      RectTransform lbRect = leaderboardController.GetPlayerPosition(username, false);
      if (lbRect == null)
        lbRect = leaderboardController.GetPlayerPosition(username, true);
      if (lbRect != null)
        startPos = lbRect.position;
    }

    spawnedChip.ChipRect.position = startPos;

    Sequence seq = DOTween.Sequence();
    seq.Join(spawnedChip.ChipRect.DOMove(endWorldPos, opponentChipMoveDuration).SetEase(Ease.OutQuad));
    seq.Append(spawnedChip.ChipRect.DOScale(Vector3.one, opponentChipScaleUpDuration).SetEase(Ease.OutQuad));
    Vector2 capturedFinalPos = finalPos;
    seq.OnComplete(() =>
    {
      if (spawnedChip != null && spawnedChip.ChipRect != null && spot.chipParent != null)
      {
        spawnedChip.ChipRect.SetParent(spot.chipParent);
        spawnedChip.ChipRect.anchoredPosition = capturedFinalPos;
      }
    });

    opponentChipsPerSpot[spotIndex].Add(new OpponentChipEntry { ChipView = spawnedChip, Username = username });
    UpdateCombinedTotal(spotIndex);
  }

  private void ToggleChipOptions()
  {
    if (areChipOptionsExpanded)
      RetractChipOptions();
    else
      ExpandChipOptions();
  }

  private void ExpandChipOptions()
  {
    audioController?.PlaySFX(SoundEffect.BetOption);
    if (mainChip == null || mainChip.button == null)
      return;

    if (chipOptionsBGCloseButton != null)
      chipOptionsBGCloseButton.gameObject.SetActive(true);

    float baseY = mainChip.button.transform.localPosition.y;

    for (int i = 0; i < chipOptions.Count; i++)
    {
      var option = chipOptions[i];
      if (option == null || option.button == null)
        continue;

      option.button.gameObject.SetActive(true);
      option.button.transform.DOLocalMoveY(baseY + chipOptionsFirstOffsetY + chipOptionsSpacingY * i, chipOptionsAnimDuration)
        .SetEase(Ease.Linear);
    }

    areChipOptionsExpanded = true;
  }

  private void RetractChipOptions()
  {
    audioController?.PlaySFX(SoundEffect.BetOption);
   
    if (mainChip == null || mainChip.button == null)
      return;

    if (chipOptionsBGCloseButton != null)
      chipOptionsBGCloseButton.gameObject.SetActive(false);

    float baseY = mainChip.button.transform.localPosition.y;

    for (int i = 0; i < chipOptions.Count; i++)
    {
      var option = chipOptions[i];
      if (option == null || option.button == null)
        continue;

      option.button.transform.DOLocalMoveY(baseY, chipOptionsAnimDuration)
        .SetEase(Ease.Linear)
        .OnComplete(() =>
        {
          if (option != null && option.button != null)
            option.button.gameObject.SetActive(false);
        });
    }

    areChipOptionsExpanded = false;
  }

  private void ApplySelectedChipOption(ChipButtonView selectedOption)
  {
    if (mainChip == null || selectedOption == null)
      return;

    if (mainChip.chipValueText != null && selectedOption.chipValueText != null)
    {
      (mainChip.chipValueText.text, selectedOption.chipValueText.text) = (selectedOption.chipValueText.text, mainChip.chipValueText.text);
      (mainChip.chipImage.sprite, selectedOption.chipImage.sprite) = (selectedOption.chipImage.sprite, mainChip.chipImage.sprite);
    }

    RetractChipOptions();
  }

  private int GetChipAmountIndex()
  {
    if (mainChip == null || mainChip.chipValueText == null || socketManager == null || socketManager.initData == null)
      return 0;

    string chipText = mainChip.chipValueText.text;
    float chipValue = GameUtility.ParseFormattedCurrency(chipText);
    string currentLevel = uiManager != null ? uiManager.CurrentLevel : "";
    List<double> levelBets = GetLevelBets(currentLevel);

    if (levelBets == null)
      return 0;

    for (int i = 0; i < levelBets.Count; i++)
    {
      if (Mathf.Approximately((float)levelBets[i], chipValue))
        return i;
    }

    return 0;
  }

  private List<double> GetLevelBets(string level)
  {
    if (socketManager == null || socketManager.initData == null || socketManager.initData.gameData == null)
      return null;

    Bets bets = socketManager.initData.gameData.bets;
    switch (level)
    {
      case "casual": return bets.casual;
      case "novice": return bets.novice;
      case "expert": return bets.expert;
      case "high_roller": return bets.high_roller;
      default: return bets.casual;
    }
  }

  private void PlaceBetOnSpot(int spotIndex)
  {
    if (!IsValidSpotIndex(spotIndex) || mainChip == null || betChipPrefab == null)
      return;

    if (spotIndex >= BetOptionNames.Length)
      return;

    var spot = betSpots[spotIndex];
    if (spot == null || spot.chipParent == null || spot.chipSpawnArea == null)
      return;

    if(areChipOptionsExpanded)
      RetractChipOptions();

    string betOption = BetOptionNames[spotIndex];
    int amountIndex = GetChipAmountIndex();

    socketManager.EmitPlaceBet(amountIndex, betOption, (PlaceBetResponse response) =>
    {
      if (response == null || !response.success)
      {
        string errorMsg = response?.payload?.message ?? "Bet failed";
        Debug.LogWarning("PlaceBet failed: " + errorMsg);
        ShowErrorPopup(errorMsg);
        return;
      }

      uiManager.SetBalanceText(response.payload.balance);
      SpawnChipOnSpot(spotIndex, response.payload.amount);

      CollapseRebetButton(false);
      if (!areBetActionsExpanded)
        StartCoroutine(ExpandBetActionButtons());
    });
  }

  private void SpawnChipOnSpot(int spotIndex, double amount)
  {
    if (!IsValidSpotIndex(spotIndex) || betChipPrefab == null)
      return;

    var spot = betSpots[spotIndex];
    if (spot == null || spot.chipParent == null || spot.chipSpawnArea == null)
      return;

    BetChipView spawnedChip = Instantiate(betChipPrefab, spot.chipParent);
    if (spawnedChip == null || spawnedChip.ChipRect == null || spawnedChip.ChipCanvasGroup == null)
      return;

    spawnedChip.ChipCanvasGroup.alpha = 0f;
    spawnedChip.ChipRect.localScale = Vector3.one;
    spawnedChip.ChipRect.localRotation = Quaternion.identity;

    string chipValueText = GameUtility.FormatCurrency(amount);
    Sprite chipSprite = mainChip != null && mainChip.chipImage != null ? mainChip.chipImage.sprite : null;
    spawnedChip.SetChipVisuals(chipSprite, chipValueText);

    string localUsername = socketManager != null && socketManager.initData != null ? socketManager.initData.player.username : null;
    var rippleSprites = GetRippleSpritesForUser(localUsername);
    if (rippleSprites != null) spawnedChip.ShowRipple(rippleSprites);

    Vector2 finalPos = GetRandomAnchoredPosition(spawnedChip.ChipRect, spot.chipSpawnArea);
    spawnedChip.ChipRect.anchoredPosition = finalPos + new Vector2(0f, chipSpawnYOffset);

    Sequence seq = DOTween.Sequence();
    seq.Join(spawnedChip.ChipRect.DOAnchorPos(finalPos, chipSpawnDuration).SetEase(Ease.OutBack));
    seq.Join(spawnedChip.ChipCanvasGroup.DOFade(1f, chipSpawnDuration * 0.5f).SetEase(Ease.Linear));
    audioController?.PlaySFX(SoundEffect.OnBet);

    chipsPerSpot[spotIndex].Add(spawnedChip);
    betUndoStack.Push(new BetUndoEntry { SpotIndex = spotIndex, ChipView = spawnedChip });

    UpdateSpotTotal(spotIndex);
    UpdateCombinedTotal(spotIndex);
  }

  private Vector2 GetRandomAnchoredPosition(RectTransform chipRect, RectTransform spawnArea)
  {
    Rect area = spawnArea.rect;
    Vector2 chipSize = chipRect.rect.size;

    float halfW = chipSize.x * 0.5f;
    float halfH = chipSize.y * 0.5f;

    float minX = area.xMin + 5f + halfW;
    float maxX = area.xMax - 5f - halfW;
    float minY = area.yMin + 5f + halfH;
    float maxY = area.yMax - 5f - halfH;

    if (minX > maxX)
      minX = maxX = 0f;

    if (minY > maxY)
      minY = maxY = 0f;

    return new Vector2(UnityEngine.Random.Range(minX, maxX), UnityEngine.Random.Range(minY, maxY));
  }

  private void UpdateSpotTotal(int spotIndex)
  {
    if (!IsValidSpotIndex(spotIndex))
      return;

    var spot = betSpots[spotIndex];
    if (spot == null || spot.totalBetText == null || spot.totalBetRoot == null)
      return;

    float totalBet = 0f;
    for (int i = chipsPerSpot[spotIndex].Count - 1; i >= 0; i--)
    {
      BetChipView chip = chipsPerSpot[spotIndex][i];
      if (chip == null)
      {
        chipsPerSpot[spotIndex].RemoveAt(i);
        continue;
      }

      totalBet += chip.ChipValue;
    }

    if (totalBet > 0f)
    {
      spot.totalBetText.text = GameUtility.FormatCurrency(totalBet);
      if (!spot.totalBetRoot.gameObject.activeInHierarchy)
      {
        spot.totalBetRoot.DOKill();
        spot.totalBetRoot.gameObject.SetActive(true);
        spot.totalBetRoot.sizeDelta = Vector2.one * 0.7f;
        spot.totalBetRoot.DOSizeDelta(spot.totalBetBaseSize, 0.5f).SetEase(Ease.OutBack, 2f);
      }
      else
      {
        spot.totalBetRoot.DOKill();
        spot.totalBetRoot.sizeDelta = spot.totalBetBaseSize;
        spot.totalBetRoot.DOSizeDelta(spot.totalBetBaseSize * 1.1f, 0.25f).OnComplete(() =>
        {
          if (spot != null && spot.totalBetRoot != null)
            spot.totalBetRoot.DOSizeDelta(spot.totalBetBaseSize, 0.25f);
        });
      }
    }
    else
    {
      spot.totalBetRoot.gameObject.SetActive(false);
      spot.totalBetText.text = GameUtility.FormatCurrency(0);
    }
  }

  private void UpdateCombinedTotal(int spotIndex, bool immediate = false)
  {
    if (!IsValidSpotIndex(spotIndex)) return;

    var spot = betSpots[spotIndex];
    if (spot?.combinedTotalRoot == null || spot.combinedTotalText == null) return;

    double total = 0;
    foreach (var chip in chipsPerSpot[spotIndex])
      if (chip != null) total += chip.ChipValue;
    foreach (var entry in opponentChipsPerSpot[spotIndex])
      if (entry?.ChipView != null) total += entry.ChipView.ChipValue;

    spot.combinedTotalRoot.DOKill();

    if (immediate)
    {
      spot.combinedTotalText.text = total > 0 ? GameUtility.FormatCurrency(total) : "0";
      spot.combinedTotalRoot.localScale = total > 0 ? Vector3.one : Vector3.zero;
      return;
    }

    if (total <= 0)
    {
      spot.combinedTotalRoot.DOScale(Vector3.zero, combinedTotalScaleDuration)
        .SetEase(Ease.InBack)
        .OnComplete(() =>
        {
          if (spot.combinedTotalText != null)
            spot.combinedTotalText.text = "0";
        });
    }
    else
    {
      string formatted = GameUtility.FormatCurrency(total);
      bool wasZero = spot.combinedTotalRoot.localScale.x < 0.1f;
      spot.combinedTotalText.text = formatted;

      if (wasZero)
      {
        spot.combinedTotalRoot.localScale = Vector3.zero;
        spot.combinedTotalRoot.DOScale(Vector3.one, combinedTotalScaleDuration)
          .SetEase(Ease.OutBack);
      }
      else
      {
        spot.combinedTotalRoot.DOScale(Vector3.one * 1.25f, combinedTotalScaleDuration * 0.5f)
          .SetEase(Ease.OutBack)
          .OnComplete(() =>
          {
            if (spot?.combinedTotalRoot != null)
              spot.combinedTotalRoot.DOScale(Vector3.one, combinedTotalScaleDuration * 0.5f)
                .SetEase(Ease.InQuad);
          });
      }
    }
  }

  private void UndoLastBet()
  {
    if (betUndoStack.Count == 0)
      return;

    audioController?.PlaySFX(SoundEffect.OnCancelUndo);
    socketManager.EmitUndoBet((UndoBetResponse response) =>
    {
      if (response == null || !response.success)
      {
        string errorMsg = response?.payload?.message ?? "Undo failed";
        Debug.LogWarning("UndoBet failed: " + errorMsg);
        ShowErrorPopup(errorMsg);
        return;
      }

      uiManager.SetBalanceText(response.payload.balance);
      RemoveLastChipVisual();


      if (betUndoStack.Count == 0)
      {
        CollapseBetActionButtons();
        if (hasPreviousBetData)
          DOVirtual.DelayedCall(betActionsAnimDuration, ExpandRebetButton);
      }
    });
  }

  private void RemoveLastChipVisual()
  {
    if (betUndoStack.Count == 0)
      return;

    BetUndoEntry entry = betUndoStack.Pop();
    if (entry.ChipView == null || !IsValidSpotIndex(entry.SpotIndex))
      return;

    RectTransform chipRect = entry.ChipView.ChipRect;
    if (chipRect == null)
      return;

    chipRect.DOKill();
    chipsPerSpot[entry.SpotIndex].Remove(entry.ChipView);
    UpdateSpotTotal(entry.SpotIndex);
    UpdateCombinedTotal(entry.SpotIndex);

    if (chipUndoDestroyTarget != null)
    {
      chipRect.DOMove(chipUndoDestroyTarget.position, chipUndoDuration).SetEase(Ease.InBack)
        .OnComplete(() =>
        {
          if (entry.ChipView != null)
            Destroy(entry.ChipView.gameObject);
        });
    }
    else
    {
      Destroy(entry.ChipView.gameObject);
      UpdateSpotTotal(entry.SpotIndex);
    }
  }

  private void CancelAllBets()
  {
    audioController.PlaySFX(SoundEffect.OnCancelUndo);
    socketManager.EmitCancelBet((CancelBetResponse response) =>
    {
      if (response == null || !response.success)
      {
        string errorMsg = response?.payload?.message ?? "Cancel failed";
        Debug.LogWarning("CancelBet failed: " + errorMsg);
        ShowErrorPopup(errorMsg);
        return;
      }

      uiManager.SetBalanceText(response.payload.balance);
      CollapseBetActionButtons();


      AnimateCancelChips(() =>
      {
        if (hasPreviousBetData)
          ExpandRebetButton();
      });
    });
  }

  private void DoubleAllBets()
  {
    socketManager.EmitDoubleBet((DoubleBetResponse response) =>
    {
      if (response == null || !response.success)
      {
        string errorMsg = response?.payload?.message ?? "Double failed";
        Debug.LogWarning("DoubleBet failed: " + errorMsg);
        ShowErrorPopup(errorMsg);
        return;
      }

      uiManager.SetBalanceText(response.payload.balance);

      // Spawn additional chips for each doubled bet
      foreach (var bet in response.payload.bets)
      {
        int spotIndex = BetOptionToSpotIndex(bet.betOption);
        if (spotIndex >= 0 && bet.delta > 0)
          SpawnChipOnSpot(spotIndex, bet.delta);
      }
    });
  }

  private void RepeatLastRoundBets()
  {
    socketManager.EmitRepeatBet((RepeatBetResponse response) =>
    {
      if (response == null || !response.success)
      {
        string errorMsg = response?.payload?.message ?? "Repeat failed";
        Debug.LogWarning("RepeatBet failed: " + errorMsg);
        ShowErrorPopup(errorMsg);
        return;
      }

      uiManager.SetBalanceText(response.payload.balance);

      playerPlacedBetThisRound = false;
      CollapseRebetButton(true);

      // Clear existing visuals first, then spawn for each repeated bet
      ClearAllChipVisuals();
      foreach (var bet in response.payload.bets)
      {
        int spotIndex = BetOptionToSpotIndex(bet.betOption);
        if (spotIndex >= 0)
          SpawnChipOnSpot(spotIndex, bet.amount);
      }

      if (!areBetActionsExpanded && response.payload.bets != null && response.payload.bets.Count > 0)
        StartCoroutine(ExpandBetActionButtons());
    });
  }

  private int BetOptionToSpotIndex(string betOption)
  {
    for (int i = 0; i < BetOptionNames.Length; i++)
    {
      if (BetOptionNames[i] == betOption)
        return i;
    }
    return -1;
  }

  private void ClearAllChipVisuals()
  {
    betUndoStack.Clear();
    for (int i = 0; i < chipsPerSpot.Count; i++)
    {
      foreach (var chip in chipsPerSpot[i])
      {
        if (chip != null)
        {
          chip.ChipRect.DOKill();
          Destroy(chip.gameObject);
        }
      }
      chipsPerSpot[i].Clear();
      UpdateSpotTotal(i);
    }

    for (int i = 0; i < opponentChipsPerSpot.Count; i++)
    {
      foreach (var entry in opponentChipsPerSpot[i])
      {
        if (entry?.ChipView != null)
        {
          entry.ChipView.ChipRect.DOKill();
          Destroy(entry.ChipView.gameObject);
        }
      }
      opponentChipsPerSpot[i].Clear();
      UpdateCombinedTotal(i, true);
    }

    foreach (var chip in winningClientChips)
    {
      if (chip != null)
      {
        chip.ChipRect.DOKill();
        Destroy(chip.gameObject);
      }
    }
    winningClientChips.Clear();

    foreach (var entry in winningOpponentChips)
    {
      if (entry?.ChipView != null)
      {
        entry.ChipView.ChipRect.DOKill();
        Destroy(entry.ChipView.gameObject);
      }
    }
    winningOpponentChips.Clear();
  }

  // ── Cashout Animation ──────────────────────────────────────────────

  private IEnumerator RunCashoutAnimation()
  {
    int winnerSpotIndex = currentWinner - 8;
    if (winnerSpotIndex < 0 || winnerSpotIndex >= betSpots.Count)
      yield break;

    yield return new WaitForSeconds(roundEndDelay);

    // Fade overlays + fade out losing chips
    yield return StartCoroutine(FadeOverlaysAndLosingChips(winnerSpotIndex));
    audioController.PlayPlayerWinSFX(currentWinner);

    yield return new WaitForSeconds(preWinningChipDelay);

    // Spawn winning chips
    yield return StartCoroutine(SpawnWinningChips(winnerSpotIndex));

    // Bonus winner: play bonus bg animation and pop text again
    if (currentBonusPlayer == currentWinner && IsValidSpotIndex(winnerSpotIndex))
    {
      var bSpot = betSpots[winnerSpotIndex];
      if (bSpot.bonusBgAnim != null)
      {
        if (bSpot.winningSpotAnimBg != null &&
            bSpot.winningSpotAnimBg.currentAnimationState == ImageAnimation.ImageState.PLAYING)
        {
          bool bgAnimDone = false;
          bSpot.winningSpotAnimBg.SetOnCompleteCallback(() => bgAnimDone = true);
          yield return new WaitUntil(() => bgAnimDone);
          bSpot.winningSpotAnimBg.StopAnimation();
          bSpot.winningSpotAnimBg.gameObject.SetActive(false);
        }

        bSpot.bonusBgAnim.gameObject.SetActive(true);
        bSpot.bonusBgAnim.StopAnimation();
        bSpot.bonusBgAnim.StartAnimation();
      }
      StartCoroutine(BonusWinnerTextPop(bSpot));
    }

    yield return new WaitForSeconds(winningChipHoldDuration);

    // Move all chips away from winning spot
    yield return StartCoroutine(CleanupWinningSpotChips(winnerSpotIndex));

    // Update balance from cashout payout after chips reach the undo target
    if (pendingCashoutData?.payouts != null)
    {
      string username = socketManager.initData.player.username;
      foreach (var payout in pendingCashoutData.payouts)
      {
        if (payout.username == username)
        {
          uiManager.SetBalanceText(payout.balance);
          break;
        }
      }
    }

    // Fire round result text animation (don't wait)
    if (roundTotalBet > 0 && pendingCashoutData?.payouts != null)
    {
      double winAmount = 0;
      string playerName = socketManager?.initData?.player?.username;
      if (!string.IsNullOrEmpty(playerName))
      {
        foreach (var payout in pendingCashoutData.payouts)
        {
          if (payout.username == playerName)
          {
            winAmount = payout.win;
            break;
          }
        }
      }
      double net = winAmount - roundTotalBet;
      if (roundResultRoutine != null) StopCoroutine(roundResultRoutine);
      roundResultRoutine = StartCoroutine(RunRoundNetResultTextAnimation(net));
    }
    Debug.Log("Before wait");
    yield return new WaitForSecondsRealtime(overlayStayDuration);
    Debug.Log("After wait");
    ResetOverlays();
    currentWinner = -1;
    pendingCashoutData = null;
    cashoutAnimationRoutine = null;
  }

  private IEnumerator FadeOverlaysAndLosingChips(int winnerSpotIndex)
  {
    for (int i = 0; i < betSpots.Count; i++)
    {
      var spot = betSpots[i];
      if (spot == null) continue;

      if (i == winnerSpotIndex)
      {
        if (spot.lightGlow != null)
        {
          spot.lightGlow.alpha = 0f;
          spot.lightGlow.gameObject.SetActive(true);
          spot.lightGlow.DOFade(1f, overlayFadeDuration);
        }
        if (chipsPerSpot[winnerSpotIndex].Count > 0)
        {
          if (spot.winningSpotAnimBg != null)
          {
            spot.winningSpotAnimBg.gameObject.SetActive(true);
            spot.winningSpotAnimBg.StopAnimation();
            spot.winningSpotAnimBg.StartAnimation();
          }
          if (spot.winningSpotAnimFg != null)
          {
            spot.winningSpotAnimFg.gameObject.SetActive(true);
            spot.winningSpotAnimFg.StopAnimation();
            spot.winningSpotAnimFg.StartAnimation();
          }
        }
      }
      else
      {
        if (spot.darkGlow != null)
        {
          spot.darkGlow.alpha = 0f;
          spot.darkGlow.gameObject.SetActive(true);
          spot.darkGlow.DOFade(1f, overlayFadeDuration);
        }
      }
    }

    // At 50% of overlay fade, start fading losing chips
    yield return new WaitForSeconds(overlayFadeDuration * 0.5f);

    int bonusSpot = currentBonusPlayer - 8;
    for (int i = 0; i < betSpots.Count; i++)
    {
      if (i == winnerSpotIndex) continue;

      foreach (var chip in chipsPerSpot[i])
      {
        if (chip != null && chip.ChipCanvasGroup != null)
          chip.ChipCanvasGroup.DOFade(0f, losingChipsFadeOutDuration);
      }

      foreach (var entry in opponentChipsPerSpot[i])
      {
        if (entry?.ChipView != null && entry.ChipView.ChipCanvasGroup != null)
          entry.ChipView.ChipCanvasGroup.DOFade(0f, losingChipsFadeOutDuration);
      }

      if (i == bonusSpot)
      {
        var bspot = betSpots[i];
        if (bspot.bonusMultText != null && bspot.bonusMultText.gameObject.activeSelf)
        {
          DOTween.To(
            () => bspot.bonusMultText.alpha,
            x => bspot.bonusMultText.alpha = x,
            0f, bonusMultFadeOutDuration
          ).OnComplete(() => { if (bspot.bonusMultText != null) bspot.bonusMultText.gameObject.SetActive(false); });
        }
        if (bspot.bonusElectricEffect != null)
          bspot.bonusElectricEffect.SetActive(false);
      }
    }

    float remainingOverlay = overlayFadeDuration * 0.5f;
    yield return new WaitForSeconds(Mathf.Max(remainingOverlay, losingChipsFadeOutDuration));

    // Destroy faded chips on losing spots
    for (int i = 0; i < betSpots.Count; i++)
    {
      if (i == winnerSpotIndex) continue;

      foreach (var chip in chipsPerSpot[i])
      {
        if (chip != null)
        {
          chip.ChipRect.DOKill();
          Destroy(chip.gameObject);
        }
      }
      chipsPerSpot[i].Clear();

      foreach (var entry in opponentChipsPerSpot[i])
      {
        if (entry?.ChipView != null)
        {
          entry.ChipView.ChipRect.DOKill();
          Destroy(entry.ChipView.gameObject);
        }
      }
      opponentChipsPerSpot[i].Clear();

      UpdateSpotTotal(i);
      UpdateCombinedTotal(i);
    }
  }

  private IEnumerator SpawnWinningChips(int winnerSpotIndex)
  {
    if (!IsValidSpotIndex(winnerSpotIndex))
      yield break;

    var spot = betSpots[winnerSpotIndex];
    if (spot == null || spot.chipParent == null || spot.chipSpawnArea == null)
      yield break;

    if (pendingCashoutData?.payouts == null)
    {
      Debug.LogWarning("[SpawnWinningChips] pendingCashoutData or payouts is null — skipping winning chip spawn.");
      yield break;
    }

    string currentLevel = uiManager != null ? uiManager.CurrentLevel : "";
    List<double> levelBets = GetLevelBets(currentLevel);

    // Client chips
    double clientTotal = 0.0;
    foreach (var chip in chipsPerSpot[winnerSpotIndex])
    {
      if (chip != null) clientTotal += chip.ChipValue;
    }

    string clientUsername = socketManager?.initData?.player?.username;
    if (string.IsNullOrEmpty(clientUsername))
      Debug.LogWarning("[SpawnWinningChips] Client username is null — skipping client winning chips.");
    else
    {
      double clientPayoutWin = GetPayoutWin(clientUsername);
      if (clientPayoutWin <= 0)
        Debug.LogWarning($"[SpawnWinningChips] No payout.win found for client '{clientUsername}'.");
      else
      {
        // Text lerp: from bet total to full payout.win
        if (clientTotal > 0.0)
          AnimateSpotTotalToWin(winnerSpotIndex, (float)clientTotal, (float)clientPayoutWin);

        // Winning chips = profit only (payout.win already includes bet returned)
        double clientWinChipsAmount = clientPayoutWin - clientTotal;
        if (clientWinChipsAmount > 0.0)
        {
          List<double> clientChipValues = DecomposeIntoChips(clientWinChipsAmount, levelBets);
          foreach (double chipValue in clientChipValues)
          {
            Sprite sprite = GetSpriteForDenomination(chipValue, levelBets);
            SpawnWinningChipOnSpot(winnerSpotIndex, chipValue, sprite, false, null);
            yield return new WaitForSecondsRealtime(winningChipSpawnInterval);
          }
        }
      }
    }

    // Opponent chips
    for (int i = 0; i < opponentChipsPerSpot[winnerSpotIndex].Count; i++)
    {
      var entry = opponentChipsPerSpot[winnerSpotIndex][i];
      if (entry?.ChipView == null) continue;

      if (string.IsNullOrEmpty(entry.Username))
      {
        Debug.LogWarning("[SpawnWinningChips] Opponent entry has null username — skipping.");
        continue;
      }

      double opponentPayoutWin = GetPayoutWin(entry.Username);
      if (opponentPayoutWin <= 0)
      {
        Debug.LogWarning($"[SpawnWinningChips] No payout.win found for opponent '{entry.Username}'.");
        continue;
      }

      double opponentBetTotal = entry.ChipView.ChipValue;
      double opponentWinChipsAmount = opponentPayoutWin - opponentBetTotal;
      if (opponentWinChipsAmount <= 0) continue;

      List<double> opponentChipValues = DecomposeIntoChips(opponentWinChipsAmount, levelBets);
      foreach (double chipValue in opponentChipValues)
      {
        SpawnWinningChipOnSpot(winnerSpotIndex, chipValue, null, true, entry.Username);
        yield return new WaitForSecondsRealtime(winningChipSpawnInterval);
      }
    }
  }

  private double GetPayoutWin(string username)
  {
    if (pendingCashoutData?.payouts == null || string.IsNullOrEmpty(username)) return 0;
    foreach (var p in pendingCashoutData.payouts)
    {
      if (p.username == username) return p.win;
    }
    return 0;
  }

  private List<double> DecomposeIntoChips(double totalAmount, List<double> denominations)
  {
    var result = new List<double>();
    if (denominations == null || denominations.Count == 0)
    {
      result.Add(totalAmount);
      return result;
    }

    var sorted = new List<double>(denominations);
    sorted.Sort((a, b) => b.CompareTo(a));

    double remaining = totalAmount;
    const double epsilon = 0.0001;

    foreach (double denom in sorted)
    {
      if (denom <= 0) continue;
      int count = (int)(remaining / denom);
      for (int i = 0; i < count; i++) result.Add(denom);
      remaining -= denom * count;
      remaining = Math.Round(remaining, 8);
      if (remaining < epsilon) break;
    }

    if (remaining > epsilon)
      result.Add(Math.Round(remaining, 2));

    return result;
  }

  private Sprite GetSpriteForDenomination(double denomination, List<double> orderedBets)
  {
    string currentLevel = uiManager != null ? uiManager.CurrentLevel : "";
    LevelChipSprites levelConfig = levelChipSpriteConfigs?.Find(c => c.levelName == currentLevel);
    if (levelConfig == null) return mainChip?.chipImage?.sprite;

    if (orderedBets != null)
    {
      for (int i = 0; i < orderedBets.Count; i++)
      {
        if (Math.Abs(orderedBets[i] - denomination) < 0.0001)
        {
          if (i == 0) return levelConfig.mainChipSprite;
          if (levelConfig.chipOptionSprites != null && i - 1 < levelConfig.chipOptionSprites.Count)
            return levelConfig.chipOptionSprites[i - 1];
          break;
        }
      }
    }

    // Remainder chip or no denomination match — fall back to main chip sprite
    return levelConfig.mainChipSprite;
  }

  private void SpawnWinningChipOnSpot(int spotIndex, double amount, Sprite chipSprite, bool isOpponent, string username)
  {
    var spot = betSpots[spotIndex];
    if (spot == null || spot.chipParent == null || spot.chipSpawnArea == null)
      return;

    BetChipView prefab = isOpponent ? opponentChipPrefab : betChipPrefab;
    if (prefab == null) return;

    Transform movingParent = winningChipMovingParent != null ? winningChipMovingParent : spot.chipParent;
    BetChipView spawnedChip = Instantiate(prefab, movingParent);
    if (spawnedChip == null || spawnedChip.ChipRect == null || spawnedChip.ChipCanvasGroup == null)
      return;

    spawnedChip.ChipCanvasGroup.alpha = 1f;
    spawnedChip.ChipRect.localScale = isOpponent ? Vector3.one * opponentChipMoveScale : Vector3.one;
    spawnedChip.ChipRect.localRotation = Quaternion.identity;

    if (isOpponent)
      spawnedChip.SetChipValueText(GameUtility.FormatCurrency(amount));
    else
      spawnedChip.SetChipVisuals(chipSprite, GameUtility.FormatCurrency(amount));

    string rippleUsername = isOpponent ? username : (socketManager != null && socketManager.initData != null ? socketManager.initData.player.username : null);
    var rippleSprites = GetRippleSpritesForUser(rippleUsername);
    if (rippleSprites != null) spawnedChip.ShowRipple(rippleSprites);

    // Get final anchored position by temporarily parenting to chipParent
    Vector2 finalAnchoredPos = GetRandomAnchoredPosition(spawnedChip.ChipRect, spot.chipSpawnArea);
    spawnedChip.ChipRect.SetParent(spot.chipParent);
    spawnedChip.ChipRect.anchoredPosition = finalAnchoredPos;
    Vector3 endWorldPos = spawnedChip.ChipRect.position;
    spawnedChip.ChipRect.SetParent(movingParent);

    if (winningChipStartRef != null)
      spawnedChip.ChipRect.position = winningChipStartRef.position;

    Sequence seq = DOTween.Sequence();
    seq.Join(spawnedChip.ChipRect.DOMove(endWorldPos, winningChipMoveDuration).SetEase(Ease.OutQuad));
    if (isOpponent)
      seq.Append(spawnedChip.ChipRect.DOScale(Vector3.one, opponentChipScaleUpDuration).SetEase(Ease.OutQuad));

    Vector2 capturedPos = finalAnchoredPos;
    seq.OnComplete(() =>
    {
      if (spawnedChip != null && spawnedChip.ChipRect != null && spot.chipParent != null)
      {
        spawnedChip.ChipRect.SetParent(spot.chipParent);
        spawnedChip.ChipRect.anchoredPosition = capturedPos;
      }
    });

    if (isOpponent)
      winningOpponentChips.Add(new OpponentChipEntry { ChipView = spawnedChip, Username = username });
    else
      winningClientChips.Add(spawnedChip);
  }

  private IEnumerator CleanupWinningSpotChips(int winnerSpotIndex)
  {
    // Client chips (original + winning) → undo target → destroy
    var clientChips = new List<BetChipView>();
    clientChips.AddRange(chipsPerSpot[winnerSpotIndex]);
    clientChips.AddRange(winningClientChips);

    if(clientChips.Count > 0)
      audioController.PlaySFX(SoundEffect.ChipsWon);
      
    foreach (var chip in clientChips)
    {
      if (chip == null || chip.ChipRect == null) continue;
      chip.ChipRect.DOKill();
      if (chipUndoDestroyTarget != null)
      {
        var c = chip;
        chip.ChipRect.DOMove(chipUndoDestroyTarget.position, chipReturnDuration)
          .SetEase(Ease.InBack)
          .OnComplete(() => { if (c != null) Destroy(c.gameObject); });
      }
      else
      {
        Destroy(chip.gameObject);
      }
    }

    // Opponent chips (original + winning) → leaderboard or start ref
    var opponentEntries = new List<OpponentChipEntry>();
    opponentEntries.AddRange(opponentChipsPerSpot[winnerSpotIndex]);
    opponentEntries.AddRange(winningOpponentChips);

    foreach (var entry in opponentEntries)
    {
      if (entry?.ChipView == null || entry.ChipView.ChipRect == null) continue;

      Vector3 targetPos;
      Transform moveParent = null;
      bool onLeaderboard = false;

      if (leaderboardController != null && !string.IsNullOrEmpty(entry.Username))
      {
        RectTransform lbRect = leaderboardController.GetPlayerPosition(entry.Username, false);
        if (lbRect == null)
          lbRect = leaderboardController.GetPlayerPosition(entry.Username, true);

        if (lbRect != null)
        {
          targetPos = lbRect.position;
          moveParent = leaderboardChipMovingParent;
          onLeaderboard = true;
        }
        else
        {
          targetPos = opponentChipStartRef != null ? opponentChipStartRef.position : entry.ChipView.ChipRect.position;
        }
      }
      else
      {
        targetPos = opponentChipStartRef != null ? opponentChipStartRef.position : entry.ChipView.ChipRect.position;
      }

      if (moveParent != null && onLeaderboard)
        entry.ChipView.ChipRect.SetParent(moveParent);

      entry.ChipView.ChipRect.DOKill();
      var chipView = entry.ChipView;
      entry.ChipView.ChipRect.DOMove(targetPos, chipReturnDuration)
        .SetEase(Ease.InBack)
        .OnComplete(() => { if (chipView != null) Destroy(chipView.gameObject); });
    }

    chipsPerSpot[winnerSpotIndex].Clear();
    opponentChipsPerSpot[winnerSpotIndex].Clear();
    winningClientChips.Clear();
    winningOpponentChips.Clear();

    UpdateSpotTotal(winnerSpotIndex);

    yield return new WaitForSeconds(chipReturnDuration + 0.1f);

    if (combinedTotalClearDelay > 0f)
      yield return new WaitForSeconds(combinedTotalClearDelay);

    UpdateCombinedTotal(winnerSpotIndex);
  }

  private void ResetOverlays()
  {
    if (bonusLightningAnimRoutine != null)
    {
      StopCoroutine(bonusLightningAnimRoutine);
      bonusLightningAnimRoutine = null;
    }

    for (int i = 0; i < betSpots.Count; i++)
    {
      var spot = betSpots[i];
      if (spot == null) continue;

      if (spot.lightGlow != null)
      {
        spot.lightGlow.DOKill();
        spot.lightGlow.alpha = 0f;
        spot.lightGlow.gameObject.SetActive(false);
      }

      if (spot.darkGlow != null)
      {
        spot.darkGlow.DOKill();
        spot.darkGlow.alpha = 0f;
        spot.darkGlow.gameObject.SetActive(false);
      }

      if (spot.winningSpotAnimBg != null)
      {
        spot.winningSpotAnimBg.StopAnimation();
        spot.winningSpotAnimBg.gameObject.SetActive(false);
      }
      if (spot.winningSpotAnimFg != null)
      {
        spot.winningSpotAnimFg.StopAnimation();
        spot.winningSpotAnimFg.gameObject.SetActive(false);
      }

      if (spot.bonusBgAnim != null)
      {
        spot.bonusBgAnim.StopAnimation();
        spot.bonusBgAnim.gameObject.SetActive(false);
      }

      if (spot.bonusLightningAnim != null)
      {
        spot.bonusLightningAnim.StopAnimation();
        spot.bonusLightningAnim.gameObject.SetActive(false);
      }

      if (spot.bonusMultText != null)
      {
        spot.bonusMultText.DOKill();
        spot.bonusMultText.alpha = 1f;
        spot.bonusMultText.transform.localScale = Vector3.one;
        spot.bonusMultText.gameObject.SetActive(false);
      }
      if (spot.bonusElectricEffect != null)
        spot.bonusElectricEffect.SetActive(false);
    }

    currentBonusPlayer = -1;
  }

  private void AnimateSpotTotalToWin(int spotIndex, float fromValue, float toValue)
  {
    var spot = betSpots[spotIndex];
    if (spot?.totalBetText == null) return;

    float current = fromValue;
    DOTween.To(() => current, x =>
    {
      current = x;
      spot.totalBetText.text = GameUtility.FormatCurrency(x);
    }, toValue, winTotalLerpDuration).SetEase(Ease.OutQuad);
  }

  private IEnumerator PlayLightningAnimStaggered()
  {
    for (int i = 0; i < betSpots.Count; i++)
    {
      if (i > 0)
        yield return new WaitForSecondsRealtime(0.5f);
      var spot = betSpots[i];
      if (spot?.bonusLightningAnim == null) continue;
      spot.bonusLightningAnim.gameObject.SetActive(true);
      spot.bonusLightningAnim.StopAnimation();
      spot.bonusLightningAnim.StartAnimation();
    }
    bonusLightningAnimRoutine = null;
  }

  private IEnumerator ShowBonusMultAnim(int spotIndex)
  {
    yield return new WaitForSeconds(bonusMultShowDelay);
    var spot = betSpots[spotIndex];
    if (spot == null) yield break;

    if (spot.bonusMultText != null)
    {
      spot.bonusMultText.gameObject.SetActive(true);
      spot.bonusMultText.transform.localScale = Vector3.one;
    }
    if (spot.bonusElectricEffect != null)
      spot.bonusElectricEffect.SetActive(true);

    if (spot.bonusMultText != null)
    {
      spot.bonusMultText.transform
        .DOScale(Vector3.one * bonusMultScaleFactor, bonusMultScaleUpDuration)
        .SetEase(Ease.OutBack)
        .OnComplete(() =>
        {
          if (spot.bonusMultText != null)
            spot.bonusMultText.transform
              .DOScale(Vector3.one, bonusMultScaleDownDuration)
              .SetEase(Ease.InQuad);
        });
    }
  }

  private IEnumerator BonusWinnerTextPop(BetSpotView spot)
  {
    yield return new WaitForSeconds(bonusWinnerScaleDelay);

    if (spot?.bonusMultText == null) yield break;

    spot.bonusMultText.transform.DOKill();
    spot.bonusMultText.transform.localScale = Vector3.one;
    spot.bonusMultText.transform
      .DOScale(Vector3.one * bonusMultScaleFactor, bonusMultScaleUpDuration)
      .SetEase(Ease.OutBack)
      .OnComplete(() =>
      {
        if (spot?.bonusMultText != null)
          spot.bonusMultText.transform
            .DOScale(Vector3.one, bonusMultScaleDownDuration)
            .SetEase(Ease.InQuad);
      });

    yield return new WaitForSeconds(bonusMultScaleUpDuration + bonusMultScaleDownDuration + bonusMultHideDelay);

    if (spot.bonusMultText != null) spot.bonusMultText.gameObject.SetActive(false);
    if (spot.bonusElectricEffect != null) spot.bonusElectricEffect.SetActive(false);
  }

  private double GetBonusMultiplier(int playerNumber)
  {
    var m = socketManager?.initData?.gameData?.bonusMultipliers;
    if (m == null) return 1;
    switch (playerNumber)
    {
      case 8:  return m.player_8;
      case 9:  return m.player_9;
      case 10: return m.player_10;
      case 11: return m.player_11;
      default: return 1;
    }
  }

  private double GetPayoutMultiplier(int playerNumber)
  {
    if (socketManager == null || socketManager.initData == null ||
        socketManager.initData.gameData == null || socketManager.initData.gameData.wagers == null ||
        socketManager.initData.gameData.wagers.main_bets == null)
      return 1;

    var mainBets = socketManager.initData.gameData.wagers.main_bets;
    switch (playerNumber)
    {
      case 8: return mainBets.player_8 != null && mainBets.player_8.payout != null && mainBets.player_8.payout.Count > 1 ? mainBets.player_8.payout[1] : 1;
      case 9: return mainBets.player_9 != null && mainBets.player_9.payout != null && mainBets.player_9.payout.Count > 1 ? mainBets.player_9.payout[1] : 1;
      case 10: return mainBets.player_10 != null && mainBets.player_10.payout != null && mainBets.player_10.payout.Count > 1 ? mainBets.player_10.payout[1] : 1;
      case 11: return mainBets.player_11 != null && mainBets.player_11.payout != null && mainBets.player_11.payout.Count > 1 ? mainBets.player_11.payout[1] : 1;
      default: return 1;
    }
  }

  private void StopCashoutAnimation()
  {
    if (cashoutAnimationRoutine != null)
    {
      StopCoroutine(cashoutAnimationRoutine);
      cashoutAnimationRoutine = null;
    }
    currentWinner = -1;
    pendingCashoutData = null;
  }

  // ── UI Helpers ─────────────────────────────────────────────────────

  private void CollapseBetActionButtons()
  {
    areBetActionsExpanded = false;

    if (betActionsPanel != null)
    {
      betActionsPanel.DOSizeDelta(new Vector2(0f, betActionsPanel.rect.height), betActionsAnimDuration)
        .SetEase(Ease.InBack);
    }

    if (undoBetButton != null)
      undoBetButton.transform.DOLocalMoveX(0f, betActionsAnimDuration).SetEase(Ease.InBack)
        .OnComplete(() => { if (undoBetButton != null) undoBetButton.gameObject.SetActive(false); });

    if (cancelBetButton != null)
      cancelBetButton.transform.DOLocalMoveX(0f, betActionsAnimDuration).SetEase(Ease.InBack)
        .OnComplete(() => { if (cancelBetButton != null) cancelBetButton.gameObject.SetActive(false); });

    if (doubleBetButton != null)
      doubleBetButton.transform.DOLocalMoveX(0f, betActionsAnimDuration).SetEase(Ease.InBack)
        .OnComplete(() => { if (doubleBetButton != null) doubleBetButton.gameObject.SetActive(false); });
  }

  private void ShowErrorPopup(string message)
  {
    if (errorPopupRoot == null || errorPopupCanvasGroup == null)
      return;

    // Kill any running popup animation
    if (errorPopupSequence != null && errorPopupSequence.IsActive())
      errorPopupSequence.Kill();

    // Set text
    if (errorPopupText != null)
      errorPopupText.text = message;

    // Reset to off-screen left (init position)
    errorPopupCanvasGroup.alpha = 0f;
    errorPopupRoot.localPosition = errorPopupInitLocalPos;

    audioController.PlaySFX(SoundEffect.Error);
    errorPopupSequence = DOTween.Sequence();

    // Slide in to center (x = 0) + fade in
    errorPopupSequence.Append(errorPopupRoot.DOLocalMoveX(0f, popupFadeInDuration).SetEase(Ease.OutCubic));
    errorPopupSequence.Join(errorPopupCanvasGroup.DOFade(1f, popupFadeInDuration).SetEase(Ease.OutCubic));

    // Stay
    errorPopupSequence.AppendInterval(popupStayDuration);

    // Slide out to right (x = +|initX|) + fade out
    errorPopupSequence.Append(errorPopupRoot.DOLocalMoveX(-errorPopupInitLocalPos.x, popupFadeOutDuration).SetEase(Ease.InCubic));
    errorPopupSequence.Join(errorPopupCanvasGroup.DOFade(0f, popupFadeOutDuration).SetEase(Ease.InCubic));

    // Reset to init position when done
    errorPopupSequence.OnComplete(() =>
    {
      if (errorPopupRoot != null)
        errorPopupRoot.localPosition = errorPopupInitLocalPos;
    });
  }

  private IEnumerator ExpandBetActionButtons()
  {
    CollapseRebetButton(true);
    areBetActionsExpanded = true;

    if (betActionsPanel != null && betActionsPanel.rect.width != 0f)
    {
      yield return betActionsPanel
        .DOSizeDelta(new Vector2(0f, betActionsPanel.rect.height), betActionsAnimDuration)
        .SetEase(Ease.OutBack)
        .WaitForCompletion();
    }

    if (betActionsPanel != null && betActionsPanel.rect.width != betActionsExpandedWidth)
    {
      betActionsPanel.DOSizeDelta(new Vector2(betActionsExpandedWidth, betActionsPanel.rect.height), betActionsAnimDuration)
        .SetEase(Ease.OutBack);
    }

    if (undoBetButton != null)
    {
      if (!undoBetButton.gameObject.activeInHierarchy)
        undoBetButton.gameObject.SetActive(true);
      if (!Mathf.Approximately(undoBetButton.transform.localPosition.x, undoButtonExpandedX))
        undoBetButton.transform.DOLocalMoveX(undoButtonExpandedX, betActionsAnimDuration).SetEase(Ease.OutBack);
    }

    if (cancelBetButton != null)
    {
      if (!cancelBetButton.gameObject.activeInHierarchy)
        cancelBetButton.gameObject.SetActive(true);
      if (!Mathf.Approximately(cancelBetButton.transform.localPosition.x, cancelButtonExpandedX))
        cancelBetButton.transform.DOLocalMoveX(cancelButtonExpandedX, betActionsAnimDuration).SetEase(Ease.OutBack);
    }

    if (doubleBetButton != null)
    {
      if (!doubleBetButton.gameObject.activeInHierarchy)
        doubleBetButton.gameObject.SetActive(true);
      if (!Mathf.Approximately(doubleBetButton.transform.localPosition.x, doubleButtonExpandedX))
        doubleBetButton.transform.DOLocalMoveX(doubleButtonExpandedX, betActionsAnimDuration).SetEase(Ease.OutBack);
    }
  }

  private void ExpandBetActionButtonsImmediate()
  {
    areBetActionsExpanded = true;

    if (betActionsPanel != null)
    {
      betActionsPanel.DOKill();
      betActionsPanel.sizeDelta = new Vector2(betActionsExpandedWidth, betActionsPanel.rect.height);
    }

    if (undoBetButton != null)
    {
      undoBetButton.transform.DOKill();
      undoBetButton.gameObject.SetActive(true);
      Vector3 lp = undoBetButton.transform.localPosition;
      undoBetButton.transform.localPosition = new Vector3(undoButtonExpandedX, lp.y, lp.z);
    }

    if (cancelBetButton != null)
    {
      cancelBetButton.transform.DOKill();
      cancelBetButton.gameObject.SetActive(true);
      Vector3 lp = cancelBetButton.transform.localPosition;
      cancelBetButton.transform.localPosition = new Vector3(cancelButtonExpandedX, lp.y, lp.z);
    }

    if (doubleBetButton != null)
    {
      doubleBetButton.transform.DOKill();
      doubleBetButton.gameObject.SetActive(true);
      Vector3 lp = doubleBetButton.transform.localPosition;
      doubleBetButton.transform.localPosition = new Vector3(doubleButtonExpandedX, lp.y, lp.z);
    }
  }

  private bool IsValidSpotIndex(int spotIndex)
  {
    return spotIndex >= 0 && spotIndex < betSpots.Count;
  }

  private List<Sprite> GetRippleSpritesForUser(string username)
  {
    if (leaderboardController == null || string.IsNullOrEmpty(username)) return null;
    string richest = leaderboardController.GetRank1RichestUsername();
    if (richest == username) return goldenRippleSprites;
    string winner = leaderboardController.GetRank1WinnerUsername();
    if (winner == username) return blueRippleSprites;
    return null;
  }

  private void CacheChipSprites()
  {
    cachedMainChipSprite = mainChip != null && mainChip.chipImage != null ? mainChip.chipImage.sprite : null;

    cachedChipOptionSprites.Clear();
    for (int i = 0; i < chipOptions.Count; i++)
    {
      ChipButtonView option = chipOptions[i];
      cachedChipOptionSprites.Add(option != null && option.chipImage != null ? option.chipImage.sprite : null);
    }
  }

  // ── Round Announcer ────────────────────────────────────────────────

  private void InitializeRoundAnnouncerState()
  {
    timerTextBaseScale = timerTextRoot != null ? timerTextRoot.localScale : Vector3.one;
    announcerParentBaseScale = announcerParent != null ? announcerParent.localScale : Vector3.one;

    SetAnnouncerVisible(lightGreenAnnouncer, false, true);
    SetAnnouncerVisible(yellowAnnouncer, false, true);
    SetAnnouncerVisible(pinkAnnouncer, false, true);
    SetAnnouncerVisible(darkGreenAnnouncer, false, true);
    SetTimerVisible(false, true);
  }

  private IEnumerator RunBettingCountdown(RoundStartEvent roundData)
  {
    int startValue = GetBettingStartValue(roundData);
    bool switchedToYellow = startValue <= 5;
    bool firstTick = true;

    FadeTimer(true);
    FadeToAnnouncer(switchedToYellow ? yellowAnnouncer : lightGreenAnnouncer, true);

    for (int value = startValue; value >= 1; value--)
    {
      if (value == 5 && !switchedToYellow)
      {
        FadeToAnnouncer(yellowAnnouncer, true);
        switchedToYellow = true;
      }

      SetTimerValue(value, bettingTimerColor);

      if (value == startValue && firstTick)
      {
        PlaySynchronizedScale(lightGreenAnnouncer);
        firstTick = false;
      }
      else if (value == 5)
      {
        audioController.PlaySFX(SoundEffect.TimeIsRunningOut);
        PlaySynchronizedScale(yellowAnnouncer);
      }
      else if (value <= 4)
      {
        audioController.PlaySFX(SoundEffect.CountDownTimer);
        PlayTimerScaleOnly();
      }

      yield return new WaitForSecondsRealtime(1f);
    }

    roundCountdownRoutine = null;
  }

  private IEnumerator RunNextRoundCountdown(int timeRemainingMs = -1)
  {
    int startValue;
    if (timeRemainingMs < 0)
    {
      yield return new WaitForSecondsRealtime(postRoundAnimationDelay);
      startValue = 4;
    }
    else
    {
      // Wait out everything before the 5-tick countdown in exact ms to avoid rounding drift
      int waitMs = Mathf.Max(0, timeRemainingMs - 5000);
      if (waitMs > 0)
        yield return new WaitForSecondsRealtime(waitMs / 1000f);

      int countdownMs = timeRemainingMs - waitMs;
      startValue = Mathf.Clamp(Mathf.CeilToInt(countdownMs / 1000f) - 1, 0, 4);
    }

    FadeTimer(true);
    FadeToAnnouncer(darkGreenAnnouncer, true);

    for (int value = startValue; value >= 1; value--)
    {
      SetTimerValue(value, finalCountdownTimerColor);
      yield return new WaitForSecondsRealtime(1f);
    }

    nextRoundRoutine = null;
  }

  private int GetBettingStartValue(RoundStartEvent roundData)
  {
    long timeRemainingMs = roundData.bettingEndTime - roundData.serverTime;
    int startValue = Mathf.CeilToInt(timeRemainingMs / 1000f) - 1;
    return Mathf.Clamp(startValue, 1, 14);
  }

  private void StartBorderGlowLoop()
  {
    StopBorderGlowLoop();

    if (betSpots == null) return;

    foreach (var spot in betSpots)
    {
      if (spot?.borderGlow == null) continue;
      spot.borderGlow.DOKill();
      var c = spot.borderGlow.color;
      c.a = 0f;
      spot.borderGlow.color = c;
    }

    bool firstAppended = false;
    borderGlowSequence = DOTween.Sequence();

    foreach (var spot in betSpots)
    {
      if (spot?.borderGlow == null) continue;
      var fadeIn = spot.borderGlow.DOFade(1f, borderGlowFadeDuration);
      if (!firstAppended) { borderGlowSequence.Append(fadeIn); firstAppended = true; }
      else borderGlowSequence.Join(fadeIn);
    }

    borderGlowSequence.AppendInterval(BorderGlowHoldDuration);

    bool firstFadeOut = false;
    foreach (var spot in betSpots)
    {
      if (spot?.borderGlow == null) continue;
      var fadeOut = spot.borderGlow.DOFade(0f, borderGlowFadeDuration);
      if (!firstFadeOut) { borderGlowSequence.Append(fadeOut); firstFadeOut = true; }
      else borderGlowSequence.Join(fadeOut);
    }

    borderGlowSequence.AppendInterval(BorderGlowHoldDuration);
    borderGlowSequence.SetLoops(-1, LoopType.Restart);
  }

  private void StopBorderGlowLoop()
  {
    if (borderGlowSequence != null)
    {
      borderGlowSequence.Kill();
      borderGlowSequence = null;
    }

    if (betSpots == null) return;
    foreach (var spot in betSpots)
    {
      if (spot?.borderGlow == null) continue;
      spot.borderGlow.DOKill();
      var c = spot.borderGlow.color;
      c.a = 0f;
      spot.borderGlow.color = c;
    }
  }

  private void StopRoundRoutines()
  {
    if (roundCountdownRoutine != null)
    {
      StopCoroutine(roundCountdownRoutine);
      roundCountdownRoutine = null;
    }

    if (nextRoundRoutine != null)
    {
      StopCoroutine(nextRoundRoutine);
      nextRoundRoutine = null;
    }
  }

  private void SetTimerValue(int value, Color color)
  {
    if (timerText != null)
    {
      timerText.text = value.ToString();
      timerText.color = color;
    }
  }

  private void FadeTimer(bool visible)
  {
    if (timerTextCanvasGroup == null)
      return;

    timerTextCanvasGroup.DOKill();
    timerTextCanvasGroup.DOFade(visible ? 1f : 0f, timerFadeDuration).SetEase(Ease.Linear);
  }

  private void SetTimerVisible(bool visible, bool immediate)
  {
    if (timerTextCanvasGroup == null)
      return;

    timerTextCanvasGroup.DOKill();
    timerTextCanvasGroup.alpha = visible ? 1f : 0f;
    if (timerTextRoot != null)
      timerTextRoot.localScale = timerTextBaseScale;
  }

  private void FadeToAnnouncer(AnnouncerView target, bool visible)
  {
    SetAnnouncerVisible(lightGreenAnnouncer, target == lightGreenAnnouncer && visible, false);
    SetAnnouncerVisible(yellowAnnouncer, target == yellowAnnouncer && visible, false);
    SetAnnouncerVisible(pinkAnnouncer, target == pinkAnnouncer && visible, false);
    SetAnnouncerVisible(darkGreenAnnouncer, target == darkGreenAnnouncer && visible, false);
  }

  private void HideAllAnnouncers()
  {
    SetAnnouncerVisible(lightGreenAnnouncer, false, false);
    SetAnnouncerVisible(yellowAnnouncer, false, false);
    SetAnnouncerVisible(pinkAnnouncer, false, false);
    SetAnnouncerVisible(darkGreenAnnouncer, false, false);
  }

  private void SetAnnouncerVisible(AnnouncerView announcer, bool visible, bool immediate)
  {
    if (announcer == null || announcer.canvasGroup == null)
      return;

    announcer.canvasGroup.DOKill();
    if (immediate)
    {
      announcer.canvasGroup.alpha = visible ? 1f : 0f;
      if (announcerParent != null)
        announcerParent.localScale = announcerParentBaseScale;
      return;
    }

    announcer.canvasGroup.DOFade(visible ? 1f : 0f, announcerFadeDuration).SetEase(Ease.Linear);
  }

  private void PlaySynchronizedScale(AnnouncerView announcer)
  {
    PlayTimerScaleOnly();
    PlayAnnouncerScale(announcer);
  }

  private void PlayTimerScaleOnly()
  {
    if (timerTextRoot == null)
      return;

    timerTextRoot.DOKill();
    timerTextRoot.localScale = timerTextBaseScale;
    timerTextRoot
      .DOScale(timerTextBaseScale * timerScalePunch, scaleUpDuration)
      .SetEase(Ease.OutQuad)
      .OnComplete(() =>
      {
        if (timerTextRoot != null)
          timerTextRoot.DOScale(timerTextBaseScale, scaleDownDuration).SetEase(Ease.InQuad);
      });
  }

  private void PlayAnnouncerScale(AnnouncerView announcer)
  {
    if (announcer == null || announcerParent == null)
      return;

    announcerParent.DOKill();
    announcerParent.localScale = announcerParentBaseScale;
    announcerParent
      .DOScale(announcerParentBaseScale * announcerScalePunch, scaleUpDuration)
      .SetEase(Ease.OutQuad)
      .OnComplete(() =>
      {
        if (announcerParent != null)
          announcerParent.DOScale(announcerParentBaseScale, scaleDownDuration).SetEase(Ease.InQuad);
      });
  }

  private void KillAnnouncerTweens(AnnouncerView announcer)
  {
    if (announcer == null)
      return;

    if (announcer.canvasGroup != null)
      announcer.canvasGroup.DOKill();
  }

  // ── Betting Controls Parent ─────────────────────────────────────────

  private void AnimateBettingControlsY(float targetY, bool immediate)
  {
    if (bettingControlsParent == null) return;

    bool isHiding = !Mathf.Approximately(targetY, bettingControlsBaseY);

    bettingControlsParent.DOKill();

    if (immediate)
    {
      if (isHiding)
      {
        if (areBetActionsExpanded)
        {
          areBetActionsExpanded = false;
          if (betActionsPanel != null) { betActionsPanel.DOKill(); betActionsPanel.sizeDelta = new Vector2(0f, betActionsPanel.rect.height); }
          if (undoBetButton != null) { undoBetButton.transform.DOKill(); undoBetButton.gameObject.SetActive(false); }
          if (cancelBetButton != null) { cancelBetButton.transform.DOKill(); cancelBetButton.gameObject.SetActive(false); }
          if (doubleBetButton != null) { doubleBetButton.transform.DOKill(); doubleBetButton.gameObject.SetActive(false); }
        }

        CollapseRebetButton(true);

        if (areChipOptionsExpanded)
        {
          areChipOptionsExpanded = false;
          if (chipOptionsBGCloseButton != null) chipOptionsBGCloseButton.gameObject.SetActive(false);
          if (mainChip != null && mainChip.button != null)
          {
            float baseY = mainChip.button.transform.localPosition.y;
            foreach (var option in chipOptions)
            {
              if (option == null || option.button == null) continue;
              option.button.transform.DOKill();
              option.button.transform.localPosition = new Vector3(
                option.button.transform.localPosition.x, baseY, option.button.transform.localPosition.z);
              option.button.gameObject.SetActive(false);
            }
          }
        }
      }

      Vector2 pos = bettingControlsParent.anchoredPosition;
      bettingControlsParent.anchoredPosition = new Vector2(pos.x, targetY);
    }
    else if (isHiding)
    {
      float delay = 0f;

      if (areBetActionsExpanded)
      {
        CollapseBetActionButtons();
        delay = Mathf.Max(delay, betActionsAnimDuration);
      }

      if (isRebetExpanded)
      {
        CollapseRebetButton(false);
        delay = Mathf.Max(delay, rebetAnimDuration);
      }

      if (areChipOptionsExpanded)
      {
        RetractChipOptions();
        delay = Mathf.Max(delay, chipOptionsAnimDuration);
      }

      if (delay > 0f)
        DOVirtual.DelayedCall(delay, () =>
        {
          if (bettingControlsParent != null)
            bettingControlsParent.DOAnchorPosY(targetY, bettingControlsAnimDuration).SetEase(Ease.OutBack);
        });
      else
        bettingControlsParent.DOAnchorPosY(targetY, bettingControlsAnimDuration).SetEase(Ease.OutBack);
    }
    else
    {
      bettingControlsParent.DOAnchorPosY(targetY, bettingControlsAnimDuration).SetEase(Ease.OutBack);
    }
  }

  internal void HideBettingControlsImmediate()
  {
    playerPlacedBetThisRound = false;
    hasPreviousBetData = false;
    CollapseRebetButton(true);
    CollapseBetActionButtons();
    AnimateBettingControlsY(bettingControlsBaseY + bettingControlsHideOffsetY, true);
  }

  // ── Total Stake Display ─────────────────────────────────────────────

  private float GetTotalClientBet()
  {
    float total = 0f;
    for (int i = 0; i < chipsPerSpot.Count; i++)
      for (int j = 0; j < chipsPerSpot[i].Count; j++)
        if (chipsPerSpot[i][j] != null)
          total += chipsPerSpot[i][j].ChipValue;
    return total;
  }

  private void ShowTotalStake()
  {
    if (TotalStakeParent == null || TotalStakeValueText == null) return;

    if (totalStakeRoutine != null)
      StopCoroutine(totalStakeRoutine);
    totalStakeRoutine = StartCoroutine(TotalStakeSequence());
  }

  private IEnumerator TotalStakeSequence()
  {
    float totalBet = GetTotalClientBet();
    TotalStakeValueText.text = GameUtility.FormatCurrency(totalBet);

    yield return new WaitForSecondsRealtime(totalStakePreDelay);

    TotalStakeParent.DOKill();
    TotalStakeParent.DOAnchorPosY(totalStakeBaseY, totalStakeAnimDuration).SetEase(Ease.OutBack);
    audioController?.PlaySFX(SoundEffect.NoMoreBets);
    yield return new WaitForSecondsRealtime(totalStakeHoldDuration);

    TotalStakeParent.DOKill();
    TotalStakeParent.DOAnchorPosY(totalStakeBaseY + totalStakeHideOffsetY, totalStakeAnimDuration).SetEase(Ease.OutBack);

    totalStakeRoutine = null;
  }

  private void HideTotalStakeImmediate()
  {
    if (TotalStakeParent == null) return;
    if (totalStakeRoutine != null)
    {
      StopCoroutine(totalStakeRoutine);
      totalStakeRoutine = null;
    }
    TotalStakeParent.DOKill();
    Vector2 pos = TotalStakeParent.anchoredPosition;
    TotalStakeParent.anchoredPosition = new Vector2(pos.x, totalStakeBaseY + totalStakeHideOffsetY);
  }

  // ── Rebet Button ───────────────────────────────────────────────────

  private void ExpandRebetButton()
  {
    isRebetExpanded = true;

    if (betActionsPanel != null)
    {
      betActionsPanel.DOKill();
      betActionsPanel.DOSizeDelta(new Vector2(rebetPanelExpandedWidth, betActionsPanel.rect.height), rebetAnimDuration)
        .SetEase(Ease.OutBack);
    }

    if (repeatBetButton != null)
    {
      if (!repeatBetButton.gameObject.activeInHierarchy)
        repeatBetButton.gameObject.SetActive(true);
      repeatBetButton.transform.DOKill();
      repeatBetButton.transform.DOLocalMoveX(rebetButtonExpandedX, rebetAnimDuration).SetEase(Ease.OutBack);
    }
  }

  private void CollapseRebetButton(bool immediate)
  {
    if (!isRebetExpanded) return;
    isRebetExpanded = false;

    if (betActionsPanel != null)
    {
      betActionsPanel.DOKill();
      if (immediate)
        betActionsPanel.sizeDelta = new Vector2(0f, betActionsPanel.rect.height);
      else
        betActionsPanel.DOSizeDelta(new Vector2(0f, betActionsPanel.rect.height), rebetAnimDuration).SetEase(Ease.InBack);
    }

    if (repeatBetButton != null)
    {
      repeatBetButton.transform.DOKill();
      if (immediate)
      {
        Vector3 lp = repeatBetButton.transform.localPosition;
        repeatBetButton.transform.localPosition = new Vector3(0f, lp.y, lp.z);
        repeatBetButton.gameObject.SetActive(false);
      }
      else
      {
        repeatBetButton.transform.DOLocalMoveX(0f, rebetAnimDuration).SetEase(Ease.InBack)
          .OnComplete(() => { if (repeatBetButton != null) repeatBetButton.gameObject.SetActive(false); });
      }
    }
  }

  // ── Cancel All Chips Animation ─────────────────────────────────────

  private void AnimateCancelChips(Action onComplete)
  {
    betUndoStack.Clear();

    var allChips = new List<BetChipView>();
    for (int i = 0; i < chipsPerSpot.Count; i++)
      allChips.AddRange(chipsPerSpot[i]);

    // Clear spots immediately so HasAnyClientChips() returns false during the fly-out animation
    for (int i = 0; i < chipsPerSpot.Count; i++) { chipsPerSpot[i].Clear(); UpdateSpotTotal(i); UpdateCombinedTotal(i); }

    int total = 0;
    foreach (var chip in allChips)
      if (chip != null && chip.ChipRect != null) total++;

    if (total == 0)
    {
      onComplete?.Invoke();
      return;
    }

    int[] remaining = { total };
    foreach (var chip in allChips)
    {
      if (chip == null || chip.ChipRect == null) continue;

      chip.ChipRect.DOKill();
      var captured = chip;

      if (chipUndoDestroyTarget != null)
      {
        chip.ChipRect.DOMove(chipUndoDestroyTarget.position, chipUndoDuration)
          .SetEase(Ease.InBack)
          .OnComplete(() =>
          {
            if (captured != null) Destroy(captured.gameObject);
            remaining[0]--;
            if (remaining[0] <= 0)
              onComplete?.Invoke();
          });
      }
      else
      {
        Destroy(chip.gameObject);
        remaining[0]--;
        if (remaining[0] <= 0)
          onComplete?.Invoke();
      }
    }
  }

  private bool HasAnyClientChips()
  {
    for (int i = 0; i < chipsPerSpot.Count; i++)
      if (chipsPerSpot[i].Count > 0) return true;
    return false;
  }

  // ── Round Result Text Animation ─────────────────────────────────────

  private IEnumerator RunRoundNetResultTextAnimation(double net)
  {
    if (roundResultParent == null || roundResultCanvasGroup == null || roundResultText == null)
      yield break;

    string prefix = net >= 0 ? "+" : "-";
    string formatted = GameUtility.FormatCurrency(Math.Abs(net));
    roundResultText.text = prefix + formatted;

    float hideY = roundResultBaseY + roundResultHideOffsetY;
    float visibleY = roundResultBaseY;
    float aboveY = visibleY + 100f;
    float exitY = aboveY - roundResultHideOffsetY;

    roundResultParent.DOKill();
    roundResultCanvasGroup.DOKill();
    roundResultCanvasGroup.alpha = 0f;
    roundResultParent.anchoredPosition = new Vector2(roundResultParent.anchoredPosition.x, hideY);

    yield return new WaitForSeconds(1f);

    // Phase 1: Move to visible + fade in
    roundResultParent.DOAnchorPosY(visibleY, roundResultFadeInDuration).SetEase(Ease.OutQuad);
    roundResultCanvasGroup.DOFade(1f, roundResultFadeInDuration).SetEase(Ease.Linear);
    yield return new WaitForSeconds(roundResultFadeInDuration);

    // Phase 2: Slow drift upward
    roundResultParent.DOAnchorPosY(aboveY, roundResultSlowMoveDuration).SetEase(Ease.Linear);
    yield return new WaitForSeconds(roundResultSlowMoveDuration);

    // Phase 3: Continue up + fade out
    roundResultParent.DOAnchorPosY(exitY, roundResultFadeOutDuration).SetEase(Ease.InQuad);
    roundResultCanvasGroup.DOFade(0f, roundResultFadeOutDuration).SetEase(Ease.Linear);
    yield return new WaitForSeconds(roundResultFadeOutDuration);

    roundResultParent.anchoredPosition = new Vector2(roundResultParent.anchoredPosition.x, hideY);
    roundResultRoutine = null;
  }

  private void ResetRoundResultImmediate()
  {
    if (roundResultRoutine != null)
    {
      StopCoroutine(roundResultRoutine);
      roundResultRoutine = null;
    }
    roundTotalBet = 0;
    if (roundResultParent != null)
    {
      roundResultParent.DOKill();
      roundResultParent.anchoredPosition = new Vector2(
        roundResultParent.anchoredPosition.x,
        roundResultBaseY + roundResultHideOffsetY);
    }
    if (roundResultCanvasGroup != null)
    {
      roundResultCanvasGroup.DOKill();
      roundResultCanvasGroup.alpha = 0f;
    }
  }
}
