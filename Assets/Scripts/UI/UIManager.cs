using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using JetBrains.Annotations;

public class UiManager : MonoBehaviour
{
  [SerializeField] private AudioManager audioController;
  [SerializeField] private SocketIOManager socketManager;

  [SerializeField] private Button HistoryClose_button;
  [SerializeField] private Button InfoClose_button;
  [SerializeField] private Button InfoLeft_button;
  [SerializeField] private Button InfoRight_button;
  [SerializeField] private List<GameObject> InfoPages_Objects;
  [SerializeField] private List<GameObject> InfoActive_Objects;
  private int currentInfoPage = 0;
  private bool IsMenuPanelOpen = false;

  [Header("Popus UI")]
  [SerializeField] private GameObject MainPopup_Object;
  [SerializeField] private GameObject PaytablePopup_Object;
  [SerializeField] private GameObject GameQuitPopup;
  [SerializeField] private GameObject HistoryPopup_Object;
  [SerializeField] private GameObject InfoPopup_Object;
  [SerializeField] private GameObject StartupPanel;

  [Space(50)]
  [Header("HomePage")]
  [SerializeField] private Button History;
  [SerializeField] private Button CloseStartupPanelBtn;
  [SerializeField] private Button ReadmoreStartupPanelBtn;
  [SerializeField] private RectTransform ToggleTextObj;
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

  [Space(50)]
  [Header("GamePage")]
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

  [Space(50)]
  [Header("LoadingPage")]
  [SerializeField] private GameObject loadingPage;

  [Space(50)]
  [Header("Animation Settings")]
  [SerializeField] private float MenuButtonsDuration = 0.5f;
  [SerializeField] private float lobbyButtonsCollapsedY = 440f;
  [SerializeField] private float gpButtonsCollapsedY = 440f;

  private List<Button> menuButtons;
  private List<Button> menuButtonsGP;
  private bool isLobbyMenuExpanded = false;
  private bool isGPMenuExpanded = false;
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
  bool isExit;
  bool isMusic;
  bool isSound;
  
  private void Start()
  {
    AssignButtonListeners();
    // homepage toggle text scroll
    scrollTextStartPosi = ToggleTextObj.anchoredPosition;
    StartScroll();

    // Collect lobby menu buttons into a list
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

    // Hook menu toggle
    MenuButton.onClick.RemoveAllListeners();
    MenuButton.onClick.AddListener(ToggleMenu);
    lobbyMenuButtonOriginalParent = MenuButton != null ? MenuButton.transform.parent : null;
    lobbyMenuButtonOriginalSiblingIndex = MenuButton != null ? MenuButton.transform.GetSiblingIndex() : 0;

    if (sidepanelCloseButton) sidepanelCloseButton.onClick.RemoveAllListeners();
    if (sidepanelCloseButton) sidepanelCloseButton.onClick.AddListener(RetractMenu);
    if (sidepanelCloseButton) sidepanelCloseButton.interactable = false;
    
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
  }

  private void AssignButtonListeners()
  {
    isMusic = true;
    isSound = true;

    if (GameRules) GameRules.onClick.RemoveAllListeners();
    if (GameRules) GameRules.onClick.AddListener(delegate { OpenPopup(InfoPopup_Object); });

    if (History) History.onClick.RemoveAllListeners();
    if (History) History.onClick.AddListener(delegate { OpenPopup(HistoryPopup_Object); });

    if (Sound) Sound.onClick.RemoveAllListeners();
    if (Sound) Sound.onClick.AddListener(delegate { ToggleSound(); });

    if (Music) Music.onClick.RemoveAllListeners();
    if (Music) Music.onClick.AddListener(delegate { ToggleMusic(); });

    if (GameRulesGP) GameRulesGP.onClick.RemoveAllListeners();
    if (GameRulesGP) GameRulesGP.onClick.AddListener(delegate { OpenPopup(InfoPopup_Object); });

    if (HistoryGP) HistoryGP.onClick.RemoveAllListeners();
    if (HistoryGP) HistoryGP.onClick.AddListener(delegate { OpenPopup(HistoryPopup_Object); });

    if (SoundGP) SoundGP.onClick.RemoveAllListeners();
    if (SoundGP) SoundGP.onClick.AddListener(delegate { ToggleSound(); });

    if (MusicGP) MusicGP.onClick.RemoveAllListeners();
    if (MusicGP) MusicGP.onClick.AddListener(delegate { ToggleMusic(); });

    if (HomeGP) HomeGP.onClick.RemoveAllListeners();
    if (HomeGP) HomeGP.onClick.AddListener(delegate { OpenPopup(GameQuitPopup); });

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

    if (ReadmoreStartupPanelBtn) ReadmoreStartupPanelBtn.onClick.RemoveAllListeners();
    if (ReadmoreStartupPanelBtn) ReadmoreStartupPanelBtn.onClick.AddListener(delegate { StartupPanel.SetActive(false); OpenPopup(InfoPopup_Object); });
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

  internal void LowBalPopup()
  {
    // OpenPopup();
  }

  internal void DisconnectionPopup()
  {
    if (!isExit)
    {
      // OpenPopup(DisconnectPopup_Object);
    }
  }

  internal void ReconnectionPopup()
  {
    // OpenPopup(ReconnectPopup_Object);
  }

  internal void CheckAndClosePopups()
  {
    // if (ReconnectPopup_Object.activeInHierarchy)
    // {
    //     ClosePopup(ReconnectPopup_Object);
    // }
    // if (DisconnectPopup_Object.activeInHierarchy)
    // {
    //     ClosePopup(DisconnectPopup_Object);
    // }
  }

  private void CallOnExitFunction()
  {
    isExit = true;
    StartCoroutine(socketManager.CloseSocket());
  }

  internal void OpenPopup(GameObject Popup)
  {
    if (audioController) audioController.PlayButtonAudio();
    if (MainPopup_Object) MainPopup_Object.SetActive(true);

    if (Popup)
    {
      Popup.SetActive(true);
      var rect = Popup.transform;

      // Start from small
      rect.localScale = Vector3.zero;

      // Scale up with bounce
      rect.DOScale(Vector3.one, 0.4f)
          .SetEase(Ease.OutBack);
    }
  }

  internal void ClosePopup(GameObject Popup)
  {
    if (audioController) audioController.PlayButtonAudio();

    if (Popup)
    {
      var rect = Popup.transform;

      // Scale down smoothly
      rect.DOScale(Vector3.zero, 0.3f)
          .SetEase(Ease.InBack)
          .OnComplete(() =>
          {
            Popup.SetActive(false);
            if (MainPopup_Object) MainPopup_Object.SetActive(false);
          });
    }
    else
    {
      if (MainPopup_Object) MainPopup_Object.SetActive(false);
    }
  }

  private void ToggleMusic()
  {
    isMusic = !isMusic;
    if (isMusic)
    {
      // Music_button.gameObject.SetActive(true);
      // MusicMute_button.gameObject.SetActive(false);
      audioController.ToggleMute(false, "bg");
    }
    else
    {
      // Music_button.gameObject.SetActive(false);
      // MusicMute_button.gameObject.SetActive(true);
      audioController.ToggleMute(true, "bg");
    }
  }

  private void ToggleSound()
  {
    isSound = !isSound;
    if (isSound)
    {
      // Sound_button.gameObject.SetActive(true);
      // SoundMute_button.gameObject.SetActive(false);
      if (audioController) audioController.ToggleMute(false, "button");
      if (audioController) audioController.ToggleMute(false, "wl");
      if (audioController) audioController.ToggleMute(false, "win");
      if (audioController) audioController.ToggleMute(false, "bet");

    }
    else
    {
      // Sound_button.gameObject.SetActive(false);
      // SoundMute_button.gameObject.SetActive(true);
      if (audioController) audioController.ToggleMute(true, "button");
      if (audioController) audioController.ToggleMute(true, "wl");
      if (audioController) audioController.ToggleMute(true, "win");
      if (audioController) audioController.ToggleMute(true, "bet");
    }
  }

  private void UpdateInfoUI()
  {
    for (int i = 0; i < InfoPages_Objects.Count; i++)
      InfoPages_Objects[i].SetActive(i == currentInfoPage);

    for (int i = 0; i < InfoActive_Objects.Count; i++)
      InfoActive_Objects[i].SetActive(i == currentInfoPage);

  }

  private void GoToPreviousInfoPage()
  {
    if (audioController) audioController.PlayButtonAudio();
    currentInfoPage--;
    if (currentInfoPage < 0)
      currentInfoPage = InfoPages_Objects.Count - 1;

    UpdateInfoUI();
  }

  private void GoToNextInfoPage()
  {
    if (audioController) audioController.PlayButtonAudio();
    currentInfoPage++;
    if (currentInfoPage >= InfoPages_Objects.Count)
      currentInfoPage = 0;

    UpdateInfoUI();
  }

  void StartScroll()
  {
    // Start at "fromX"
    ToggleTextObj.anchoredPosition = new Vector2(1000f, scrollTextStartPosi.y);

    // Tween to "toX"
    ToggleTextObj.DOAnchorPosX(-1000f, 10f)
        .SetEase(Ease.Linear)
        .OnComplete(() =>
        {
          ToggleTextObj.anchoredPosition = new Vector2(1000f, scrollTextStartPosi.y);
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
