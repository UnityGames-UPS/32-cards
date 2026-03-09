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
  private class BetSpotView
  {
    public Button betButton;
    public RectTransform chipParent;
    public RectTransform chipSpawnArea;
    public RectTransform totalBetRoot;
    public TMP_Text totalBetText;
    [NonSerialized] public Vector2 totalBetBaseSize;
  }

  private struct BetUndoEntry
  {
    public int SpotIndex;
    public BetChipView ChipView;
  }

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
  [SerializeField] private float announcerFadeDuration = 0.25f;
  [SerializeField] private float timerFadeDuration = 0.2f;
  [SerializeField] private float timerScalePunch = 1.4f;
  [SerializeField] private float announcerScalePunch = 1.4f;
  [SerializeField] private float scaleUpDuration = 0.16f;
  [SerializeField] private float scaleDownDuration = 0.16f;
  [SerializeField] private float postRoundAnimationDelay = 3f;

  private readonly Stack<BetUndoEntry> betUndoStack = new Stack<BetUndoEntry>();
  private readonly List<List<BetChipView>> chipsPerSpot = new List<List<BetChipView>>();
  private readonly List<Sprite> cachedChipOptionSprites = new List<Sprite>();
  private Sprite cachedMainChipSprite;

  private bool areChipOptionsExpanded;
  private bool areBetActionsExpanded;
  private Coroutine roundCountdownRoutine;
  private Coroutine nextRoundRoutine;
  private bool hasReceivedFirstCardDealt;
  private string activeRoundId;
  private bool roundEndReceived;
  private Vector3 timerTextBaseScale = Vector3.one;
  private Vector3 announcerParentBaseScale = Vector3.one;


  private void Start()
  {
    CacheChipSprites();
    InitializeSpotState();
    InitializeRoundAnnouncerState();
    BindButtonListeners();
  }

  private void OnDisable()
  {
    StopRoundRoutines();
    DOTween.Kill(timerTextRoot);
    KillAnnouncerTweens(lightGreenAnnouncer);
    KillAnnouncerTweens(yellowAnnouncer);
    KillAnnouncerTweens(pinkAnnouncer);
    KillAnnouncerTweens(darkGreenAnnouncer);
    if (announcerParent != null)
      announcerParent.DOKill();
    if (timerTextCanvasGroup != null)
      timerTextCanvasGroup.DOKill();
  }

  private void InitializeSpotState()
  {
    if (betSpots == null)
      betSpots = new List<BetSpotView>();

    if (chipOptions == null)
      chipOptions = new List<ChipButtonView>();

    chipsPerSpot.Clear();
    for (int i = 0; i < betSpots.Count; i++)
    {
      if (betSpots[i] != null && betSpots[i].totalBetRoot != null)
        betSpots[i].totalBetBaseSize = betSpots[i].totalBetRoot.sizeDelta;

      chipsPerSpot.Add(new List<BetChipView>());
      UpdateSpotTotal(i);
    }

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

  internal void OnRoundStart(RoundStartEvent roundData)
  {
    if (roundData == null)
      return;

    activeRoundId = roundData.roundId;
    hasReceivedFirstCardDealt = false;
    roundEndReceived = false;

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

    FadeTimer(false);
    FadeToAnnouncer(pinkAnnouncer, true);
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

  internal void OnRoundEnd()
  {
    roundEndReceived = true;
  }

  internal void OnCashout()
  {
    if (!roundEndReceived)
      return;

    roundEndReceived = false;

    if (nextRoundRoutine != null)
      StopCoroutine(nextRoundRoutine);

    nextRoundRoutine = StartCoroutine(RunNextRoundCountdown());
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

  private void PlaceBetOnSpot(int spotIndex)
  {
    if (!IsValidSpotIndex(spotIndex) || mainChip == null || betChipPrefab == null)
      return;

    var spot = betSpots[spotIndex];
    if (spot == null || spot.chipParent == null || spot.chipSpawnArea == null)
      return;

    RetractChipOptions();

    BetChipView spawnedChip = Instantiate(betChipPrefab, spot.chipParent);
    if (spawnedChip == null || spawnedChip.ChipRect == null || spawnedChip.ChipCanvasGroup == null)
      return;

    spawnedChip.ChipCanvasGroup.alpha = 0f;
    spawnedChip.ChipRect.localScale = Vector3.one;
    spawnedChip.ChipRect.localRotation = Quaternion.identity;

    string chipValueText = mainChip.chipValueText != null ? mainChip.chipValueText.text : "0";
    Sprite chipSprite = mainChip.chipImage != null ? mainChip.chipImage.sprite : null;
    spawnedChip.SetChipVisuals(chipSprite, chipValueText);

    Vector2 finalPos = GetRandomAnchoredPosition(spawnedChip.ChipRect, spot.chipSpawnArea);
    spawnedChip.ChipRect.anchoredPosition = finalPos + new Vector2(0f, chipSpawnYOffset);

    Sequence seq = DOTween.Sequence();
    seq.Join(spawnedChip.ChipRect.DOAnchorPos(finalPos, chipSpawnDuration).SetEase(Ease.OutBack));
    seq.Join(spawnedChip.ChipCanvasGroup.DOFade(1f, chipSpawnDuration * 0.5f).SetEase(Ease.Linear));

    chipsPerSpot[spotIndex].Add(spawnedChip);
    betUndoStack.Push(new BetUndoEntry { SpotIndex = spotIndex, ChipView = spawnedChip });

    UpdateSpotTotal(spotIndex);
    if (!areBetActionsExpanded)
      StartCoroutine(ExpandBetActionButtons());
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
      spot.totalBetText.text = totalBet.ToString("N2");
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
      spot.totalBetText.text = "0.00";
    }
  }

  private void UndoLastBet()
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

    if (chipUndoDestroyTarget != null)
    {
      chipRect.DOMove(chipUndoDestroyTarget.position, chipUndoDuration).SetEase(Ease.InBack)
        .OnComplete(() =>
        {
          if (entry.ChipView != null)
            Destroy(entry.ChipView.gameObject);
          UpdateSpotTotal(entry.SpotIndex);
        });
    }
    else
    {
      Destroy(entry.ChipView.gameObject);
      UpdateSpotTotal(entry.SpotIndex);
    }
  }

  private IEnumerator ExpandBetActionButtons()
  {
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

  private bool IsValidSpotIndex(int spotIndex)
  {
    return spotIndex >= 0 && spotIndex < betSpots.Count;
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
    bool switchedToYellow = false;
    bool firstTick = true;

    FadeTimer(true);
    FadeToAnnouncer(lightGreenAnnouncer, true);

    for (int value = startValue; value >= 0; value--)
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
        PlaySynchronizedScale(yellowAnnouncer);
      }
      else if (value <= 4)
      {
        PlayTimerScaleOnly();
      }

      yield return new WaitForSecondsRealtime(1f);
    }

    roundCountdownRoutine = null;
  }

  private IEnumerator RunNextRoundCountdown()
  {
    yield return new WaitForSecondsRealtime(postRoundAnimationDelay);

    FadeTimer(true);
    FadeToAnnouncer(darkGreenAnnouncer, true);

    for (int value = 4; value >= 0; value--)
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
}
