using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BetHistoryManager : MonoBehaviour
{
  [SerializeField] private SocketIOManager socketManager;
  [SerializeField] private List<BetHistoryItemView> historyItems;
  [SerializeField] private GameObject noBetHistoryObject;
  [SerializeField] private TMP_Text pageCountText;
  [SerializeField] private Button prevPageButton;
  [SerializeField] private Button skipPrevButton;
  [SerializeField] private Button nextPageButton;
  [SerializeField] private Button skipNextButton;

  private int currentPage = 1;
  private int totalPages = 1;
  private bool isLoading = false;

  private void Awake()
  {
    if (prevPageButton)
    {
      prevPageButton.onClick.RemoveAllListeners();
      prevPageButton.onClick.AddListener(delegate { RequestPage(Mathf.Max(1, currentPage - 1)); });
    }

    if (skipPrevButton)
    {
      skipPrevButton.onClick.RemoveAllListeners();
      skipPrevButton.onClick.AddListener(delegate { RequestPage(Mathf.Max(1, currentPage - 5)); });
    }

    if (nextPageButton)
    {
      nextPageButton.onClick.RemoveAllListeners();
      nextPageButton.onClick.AddListener(delegate { RequestPage(Mathf.Min(totalPages, currentPage + 1)); });
    }

    if (skipNextButton)
    {
      skipNextButton.onClick.RemoveAllListeners();
      skipNextButton.onClick.AddListener(delegate { RequestPage(Mathf.Min(totalPages, currentPage + 5)); });
    }
  }

  public void OpenPanel()
  {
    currentPage = 1;
    RequestPage(1);
  }

  private void RequestPage(int page)
  {
    if (isLoading) return;
    isLoading = true;
    SetAllPaginationInteractable(false);

    if (noBetHistoryObject != null)
      noBetHistoryObject.SetActive(false);

    if (historyItems != null)
      foreach (var item in historyItems)
        if (item != null) item.gameObject.SetActive(false);

    if (socketManager != null)
      socketManager.EmitBetHistory(page, OnHistoryAck);
  }

  private void OnHistoryAck(BetHistoryResponse response)
  {
    isLoading = false;

    if (response == null || !response.success || response.payload == null)
    {
      ShowNoHistory();
      return;
    }

    currentPage = response.payload.meta.page;
    totalPages = response.payload.meta.pages;

    if (response.payload.history == null || response.payload.history.Count == 0)
    {
      ShowNoHistory();
      return;
    }

    UpdatePageCountText();
    PopulateHistoryItems(response.payload.history);
    UpdatePaginationButtons();
  }

  private void PopulateHistoryItems(List<BetHistoryEntry> history)
  {
    if (noBetHistoryObject != null)
      noBetHistoryObject.SetActive(false);

    for (int i = 0; i < historyItems.Count; i++)
    {
      if (historyItems[i] == null) continue;

      if (i < history.Count)
      {
        historyItems[i].gameObject.SetActive(true);
        int globalNumber = (currentPage - 1) * 10 + i + 1;
        historyItems[i].Populate(globalNumber, history[i]);
      }
      else
      {
        historyItems[i].gameObject.SetActive(false);
      }
    }
  }

  private void ShowNoHistory()
  {
    if (noBetHistoryObject != null)
      noBetHistoryObject.SetActive(true);

    if (historyItems != null)
      foreach (var item in historyItems)
        if (item != null) item.gameObject.SetActive(false);

    if (pageCountText != null)
      pageCountText.text = "0/0";

    SetAllPaginationInteractable(false);
  }

  private void UpdatePageCountText()
  {
    if (pageCountText != null)
      pageCountText.text = $"{currentPage}/{totalPages}";
  }

  private void UpdatePaginationButtons()
  {
    bool onFirstPage = currentPage <= 1;
    bool onLastPage = currentPage >= totalPages;

    if (prevPageButton) prevPageButton.interactable = !onFirstPage;
    if (skipPrevButton) skipPrevButton.interactable = !onFirstPage;
    if (nextPageButton) nextPageButton.interactable = !onLastPage;
    if (skipNextButton) skipNextButton.interactable = !onLastPage;
  }

  private void SetAllPaginationInteractable(bool value)
  {
    if (prevPageButton) prevPageButton.interactable = value;
    if (skipPrevButton) skipPrevButton.interactable = value;
    if (nextPageButton) nextPageButton.interactable = value;
    if (skipNextButton) skipNextButton.interactable = value;
  }
}
