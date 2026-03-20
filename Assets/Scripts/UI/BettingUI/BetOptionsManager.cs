using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BetOptionsManager : MonoBehaviour
{
  private const float PopupBGTargetAlpha = 230f / 255f;

  [Header("Bet Options Popup")]
  [SerializeField] private SocketIOManager socketManager;
  [SerializeField] private AudioManager audioController;
  [SerializeField] private UiManager uiManager;
  [SerializeField] private RectTransform betOptionsPopupRoot;
  [SerializeField] private Button betOptionsOpenButton;
  [SerializeField] private Button betOptionsConfirmButton;
  [SerializeField] private Button betOptionsBGCloseButton;
  [SerializeField] private Image betOptionsBGImage;
  [SerializeField] private float popupAnimDuration = 0.3f;

  [Header("Level Option Buttons (Order: Casual, Novice, Expert, High Roller)")]
  [SerializeField] private List<Button> levelButtons = new List<Button>();
  [SerializeField] private List<Image> levelButtonImages = new List<Image>();
  [SerializeField] private List<TMP_Text> levelButtonRangeTexts = new List<TMP_Text>();
  [SerializeField] private Sprite selectedLevelSprite;
  [SerializeField] private Sprite unselectedLevelSprite;
  [SerializeField] private Color selectedLevelTextColor = Color.white;
  [SerializeField] private Color unselectedLevelTextColor = Color.white;

  [Header("Player Range Texts (Order: Player 8, 9, 10, 11)")]
  [SerializeField] private List<TMP_Text> playerRangeTexts = new List<TMP_Text>();
  [SerializeField] private GameObject leaveCurrentRoomNoticeObject;

  private int selectedLevelIndex = 0;
  private int currentRoomLevelIndex = 0;
  private bool isConfirmInProgress = false;

  private void Awake()
  {
    InitializeBetOptionsPopupState();
  }

  private void Start()
  {
    BindButtonListeners();
  }

  private void InitializeBetOptionsPopupState()
  {
    if (betOptionsPopupRoot != null)
      betOptionsPopupRoot.localScale = Vector3.zero;

    if (betOptionsBGImage != null)
    {
      if(!betOptionsBGImage.gameObject.activeSelf)
        betOptionsBGImage.gameObject.SetActive(true);
      betOptionsBGImage.enabled = true;
      betOptionsBGImage.raycastTarget = false;
      Color bgColor = betOptionsBGImage.color;
      bgColor.a = 0f;
      betOptionsBGImage.color = bgColor;
    }
  }

  private void BindButtonListeners()
  {
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
      betOptionsConfirmButton.onClick.AddListener(OnConfirmButtonClicked);
    }

    for (int i = 0; i < levelButtons.Count; i++)
    {
      int buttonIndex = i;
      Button levelButton = levelButtons[i];
      if (levelButton == null)
        continue;

      levelButton.onClick.RemoveAllListeners();
      levelButton.onClick.AddListener(() => OnLevelButtonClicked(buttonIndex));
    }
  }

  private void OpenBetOptionsPopup()
  {
    audioController?.PlaySFX(SoundEffect.ButtonClick);
    if (betOptionsPopupRoot == null)
      return;

    betOptionsPopupRoot.DOKill();
    if (betOptionsBGImage != null)
      betOptionsBGImage.DOKill();

    betOptionsPopupRoot.localScale = Vector3.zero;

    if (betOptionsBGImage != null)
    {
      betOptionsBGImage.raycastTarget = true;
      Color bgColor = betOptionsBGImage.color;
      bgColor.a = 0f;
      betOptionsBGImage.color = bgColor;
      betOptionsBGImage.DOFade(PopupBGTargetAlpha, popupAnimDuration);
    }

    RefreshBetOptionsData();
    isConfirmInProgress = false;
    SetConfirmButtonInteractable(true);
    betOptionsPopupRoot.DOScale(Vector3.one, popupAnimDuration);
  }

  private void CloseBetOptionsPopup()
  {
    audioController?.PlaySFX(SoundEffect.ButtonClick);
    if (betOptionsPopupRoot == null)
      return;

    if (betOptionsBGImage != null)
      betOptionsBGImage.DOKill();

    betOptionsPopupRoot.DOKill();
    if (betOptionsBGImage != null)
    {
      betOptionsBGImage.DOFade(0f, popupAnimDuration).OnComplete(() =>
      {
        if (betOptionsBGImage != null)
          betOptionsBGImage.raycastTarget = false;
      });
    }

    betOptionsPopupRoot.DOScale(Vector3.zero, popupAnimDuration);
  }

  private void OnConfirmButtonClicked()
  {
    audioController?.PlaySFX(SoundEffect.ButtonClick);
    if (isConfirmInProgress)
      return;

    isConfirmInProgress = true;
    SetConfirmButtonInteractable(false);

    if (selectedLevelIndex == currentRoomLevelIndex)
    {
      CloseBetOptionsPopup();
      isConfirmInProgress = false;
      SetConfirmButtonInteractable(true);
      return;
    }

    string targetLevel = GetLevelNameFromIndex(selectedLevelIndex);
    if (string.IsNullOrEmpty(targetLevel))
    {
      Debug.LogError("Unable to switch level: invalid selected level index.");
      isConfirmInProgress = false;
      SetConfirmButtonInteractable(true);
      return;
    }

    CloseBetOptionsPopup();
    if (uiManager != null)
      StartCoroutine(uiManager.SwitchLevelFromBetOptions(targetLevel));
    else
      Debug.LogError("UiManager reference missing in BetOptionsManager.");
  }

  private void OnLevelButtonClicked(int levelIndex)
  {
    audioController?.PlaySFX(SoundEffect.ButtonClick);
    selectedLevelIndex = Mathf.Clamp(levelIndex, 0, 3);
    RefreshSelectionVisuals();
    RefreshPlayerRangeTextsForSelectedLevel();
    RefreshLeaveRoomNotice();
  }

  private void RefreshBetOptionsData()
  {
    if (socketManager == null || socketManager.initData == null || socketManager.initData.gameData == null)
      return;

    currentRoomLevelIndex = GetLevelIndexFromName(uiManager != null ? uiManager.CurrentLevel : string.Empty);
    selectedLevelIndex = currentRoomLevelIndex;

    RefreshLevelRangeTexts();
    RefreshSelectionVisuals();
    RefreshPlayerRangeTextsForSelectedLevel();
    RefreshLeaveRoomNotice();
  }

  private void RefreshLevelRangeTexts()
  {
    for (int i = 0; i < levelButtonRangeTexts.Count && i < 4; i++)
    {
      TMP_Text label = levelButtonRangeTexts[i];
      if (label == null)
        continue;

      double minBet = GetLevelMinBet(i);
      double levelMaxBet = GetPlayerMaxBetForLevel(3, i); // Player 11 max, same as lobby level card in UiManager.
      label.text = $"{FormatAmount(minBet)} - {FormatAmount(levelMaxBet)}";
    }
  }

  private void RefreshPlayerRangeTextsForSelectedLevel()
  {
    double minBet = GetLevelMinBet(selectedLevelIndex);
    for (int playerIndex = 0; playerIndex < playerRangeTexts.Count && playerIndex < 4; playerIndex++)
    {
      TMP_Text label = playerRangeTexts[playerIndex];
      if (label == null)
        continue;

      double maxBet = GetPlayerMaxBetForLevel(playerIndex, selectedLevelIndex);
      label.text = $"{FormatAmount(minBet)} - {FormatAmount(maxBet)}";
    }
  }

  private void RefreshSelectionVisuals()
  {
    for (int i = 0; i < levelButtons.Count && i < 4; i++)
    {
      bool isSelected = i == selectedLevelIndex;
      if (i < levelButtonImages.Count && levelButtonImages[i] != null)
      {
        if (isSelected && selectedLevelSprite != null)
          levelButtonImages[i].sprite = selectedLevelSprite;
        else if (!isSelected && unselectedLevelSprite != null)
          levelButtonImages[i].sprite = unselectedLevelSprite;
      }

      if (i < levelButtonRangeTexts.Count && levelButtonRangeTexts[i] != null)
        levelButtonRangeTexts[i].color = isSelected ? selectedLevelTextColor : unselectedLevelTextColor;
    }
  }

  private double GetLevelMinBet(int levelIndex)
  {
    if (socketManager == null || socketManager.initData == null || socketManager.initData.gameData == null || socketManager.initData.gameData.bets == null)
      return 0d;

    Bets bets = socketManager.initData.gameData.bets;
    switch (Mathf.Clamp(levelIndex, 0, 3))
    {
      case 0: return bets.casual != null && bets.casual.Count > 0 ? bets.casual[0] : 0d;
      case 1: return bets.novice != null && bets.novice.Count > 0 ? bets.novice[0] : 0d;
      case 2: return bets.expert != null && bets.expert.Count > 0 ? bets.expert[0] : 0d;
      case 3: return bets.high_roller != null && bets.high_roller.Count > 0 ? bets.high_roller[0] : 0d;
    }
    return 0d;
  }

  private double GetPlayerMaxBetForLevel(int playerIndex, int levelIndex)
  {
    if (socketManager == null || socketManager.initData == null || socketManager.initData.gameData == null || socketManager.initData.gameData.wagers == null || socketManager.initData.gameData.wagers.main_bets == null)
      return 0d;

    MaxBetLimit maxBetLimit = null;
    switch (Mathf.Clamp(playerIndex, 0, 3))
    {
      case 0: maxBetLimit = socketManager.initData.gameData.wagers.main_bets.player_8 != null ? socketManager.initData.gameData.wagers.main_bets.player_8.max_bet_limit : null; break;
      case 1: maxBetLimit = socketManager.initData.gameData.wagers.main_bets.player_9 != null ? socketManager.initData.gameData.wagers.main_bets.player_9.max_bet_limit : null; break;
      case 2: maxBetLimit = socketManager.initData.gameData.wagers.main_bets.player_10 != null ? socketManager.initData.gameData.wagers.main_bets.player_10.max_bet_limit : null; break;
      case 3: maxBetLimit = socketManager.initData.gameData.wagers.main_bets.player_11 != null ? socketManager.initData.gameData.wagers.main_bets.player_11.max_bet_limit : null; break;
    }

    if (maxBetLimit == null)
      return 0d;

    switch (Mathf.Clamp(levelIndex, 0, 3))
    {
      case 0: return maxBetLimit.casual;
      case 1: return maxBetLimit.novice;
      case 2: return maxBetLimit.expert;
      case 3: return maxBetLimit.high_roller;
    }
    return 0d;
  }

  private string FormatAmount(double value)
  {
    return GameUtility.FormatCurrency(value);
  }

  private int GetLevelIndexFromName(string levelName)
  {
    switch (levelName)
    {
      case "casual": return 0;
      case "novice": return 1;
      case "expert": return 2;
      case "high_roller": return 3;
      default: return 0;
    }
  }

  private string GetLevelNameFromIndex(int levelIndex)
  {
    switch (Mathf.Clamp(levelIndex, 0, 3))
    {
      case 0: return "casual";
      case 1: return "novice";
      case 2: return "expert";
      case 3: return "high_roller";
    }
    return string.Empty;
  }

  private void SetConfirmButtonInteractable(bool isInteractable)
  {
    if (betOptionsConfirmButton != null)
      betOptionsConfirmButton.interactable = isInteractable;
  }

  private void RefreshLeaveRoomNotice()
  {
    if (leaveCurrentRoomNoticeObject == null)
      return;

    leaveCurrentRoomNoticeObject.SetActive(selectedLevelIndex != currentRoomLevelIndex);
  }
}
