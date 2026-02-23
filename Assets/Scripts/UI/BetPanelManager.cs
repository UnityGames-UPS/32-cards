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
  private class ChipButtonView
  {
    public Button button;
    public Image chipImage;
    public TMP_Text chipValueText;
    public string chipColorId;
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
  [SerializeField] private List<ChipColorSprite> chipSpawnColorSprites;

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

  [Header("Bet Options Popup")]
  [SerializeField] private RectTransform betOptionsPopupRoot;
  [SerializeField] private Button betOptionsOpenButton;
  [SerializeField] private Button betOptionsConfirmButton;
  [SerializeField] private Button betOptionsBGCloseButton;
  [SerializeField] private Image betOptionsBGImage;

  private readonly Stack<BetUndoEntry> betUndoStack = new Stack<BetUndoEntry>();
  private readonly List<List<BetChipView>> chipsPerSpot = new List<List<BetChipView>>();

  private bool areChipOptionsExpanded;
  private bool areBetActionsExpanded;

  private string mainChipColorId;

  private void Awake()
  {
    InitializeBetOptionsPopupState();
  }

  private void Start()
  {
    InitializeSpotState();
    BindButtonListeners();
  }

  private void InitializeBetOptionsPopupState()
  {
    if (betOptionsPopupRoot != null)
      betOptionsPopupRoot.localScale = Vector3.zero;

    if (betOptionsBGImage != null)
      betOptionsBGImage.enabled = false;
  }

  private void InitializeSpotState()
  {
    if (betSpots == null)
      betSpots = new List<BetSpotView>();

    if (chipOptions == null)
      chipOptions = new List<ChipButtonView>();

    if (chipSpawnColorSprites == null)
      chipSpawnColorSprites = new List<ChipColorSprite>();

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
      mainChipColorId = mainChip.chipColorId;

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

    if (betOptionsOpenButton != null)
    {
      betOptionsOpenButton.onClick.RemoveAllListeners();
      betOptionsOpenButton.onClick.AddListener(OpenBetOptionsPopup);
    }

    if (betOptionsBGCloseButton != null)
    {
      betOptionsBGCloseButton.onClick.RemoveAllListeners();
      betOptionsBGCloseButton.onClick.AddListener(CloseBetOptionsPopup);
    }

    if (betOptionsConfirmButton != null)
    {
      betOptionsConfirmButton.onClick.RemoveAllListeners();
      betOptionsConfirmButton.onClick.AddListener(CloseBetOptionsPopup);
    }
  }

  internal void OnCoinOptionButtonPressed(Button selectedCoinButton)
  {
    if (selectedCoinButton == null)
      return;

    for (int i = 0; i < chipOptions.Count; i++)
    {
      var option = chipOptions[i];
      if (option != null && option.button == selectedCoinButton)
      {
        ApplySelectedChipOption(option);
        break;
      }
    }
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

  private void OpenBetOptionsPopup()
  {
    if (betOptionsPopupRoot == null)
      return;

    betOptionsPopupRoot.DOKill();
    betOptionsPopupRoot.localScale = Vector3.zero;
    
    if (betOptionsBGImage != null)
    {
      betOptionsBGImage.enabled = false;
      betOptionsBGImage.gameObject.SetActive(true);
    }

    betOptionsPopupRoot.DOScale(Vector3.one, 0.3f).OnComplete(() =>
    {
      betOptionsBGImage.enabled = true;
    });
  }

  private void CloseBetOptionsPopup()
  {
    if (betOptionsPopupRoot == null)
      return;

    if(betOptionsBGImage != null)
    {
      betOptionsBGImage.enabled = false;
    }

    betOptionsPopupRoot.DOKill();
    betOptionsPopupRoot.DOScale(Vector3.zero, 0.3f).OnComplete(() =>
    {
      if (betOptionsBGImage != null)
      {
        betOptionsBGImage.gameObject.SetActive(false);        
      }
    });
  }

  private void ApplySelectedChipOption(ChipButtonView selectedOption)
  {
    if (mainChip == null || selectedOption == null)
      return;

    if (mainChip.chipImage != null && selectedOption.chipImage != null)
    {
      Sprite selectedSprite = selectedOption.chipImage.sprite;
      selectedOption.chipImage.sprite = mainChip.chipImage.sprite;
      mainChip.chipImage.sprite = selectedSprite;
    }

    if (mainChip.chipValueText != null && selectedOption.chipValueText != null)
    {
      string selectedValue = selectedOption.chipValueText.text;
      selectedOption.chipValueText.text = mainChip.chipValueText.text;
      mainChip.chipValueText.text = selectedValue;
    }

    string selectedColorId = selectedOption.chipColorId;
    selectedOption.chipColorId = mainChipColorId;
    mainChipColorId = selectedColorId;

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
    Sprite chipSprite = GetSpawnChipSprite(mainChipColorId);
    if (chipSprite == null && mainChip.chipImage != null)
      chipSprite = mainChip.chipImage.sprite;
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

  [Serializable]
  private class ChipColorSprite
  {
    public string colorId;
    public Sprite sprite;
  }

  private Sprite GetSpawnChipSprite(string colorId)
  {
    if (string.IsNullOrEmpty(colorId))
      return null;

    for (int i = 0; i < chipSpawnColorSprites.Count; i++)
    {
      var entry = chipSpawnColorSprites[i];
      if (entry != null && entry.sprite != null && entry.colorId == colorId)
        return entry.sprite;
    }

    return null;
  }
}
