using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameStatsUIController : MonoBehaviour
{
  [Serializable]
  private class MatchSidePayload
  {
    public string matchSide;
  }

  [Serializable]
  private class SideColorConfig
  {
    [SerializeField] internal int sideValue;
    [SerializeField] internal Color selectedBackgroundColor = Color.white;
    [SerializeField] internal Color unselectedTextColor = Color.white;

    internal SideColorConfig() { }

    internal SideColorConfig(int sideValue, Color selectedBackgroundColor, Color unselectedTextColor)
    {
      this.sideValue = sideValue;
      this.selectedBackgroundColor = selectedBackgroundColor;
      this.unselectedTextColor = unselectedTextColor;
    }

    internal int SideValue => sideValue;
    internal Color SelectedBackgroundColor => selectedBackgroundColor;
    internal Color UnselectedTextColor => unselectedTextColor;
  }

  [Serializable]
  private class RightStatView
  {
    [SerializeField] private int sideValue;
    [SerializeField] private TMP_Text percentageText;
    [SerializeField] private Image fillImage;

    internal int SideValue => sideValue;
    internal TMP_Text PercentageText => percentageText;
    internal Image FillImage => fillImage;
  }

  private const int GridSize = 55;
  private const int ShiftCount = 5;
  private const int PercentWindow = 100;

  [Header("Bottom Left - 55 Grid Views")]
  [SerializeField] private List<StatsGridItemView> leftGridViews = new List<StatsGridItemView>();
  [SerializeField] private Transform leftGridRoot;

  [Header("Bottom Left - Colors")]
  [SerializeField] private List<SideColorConfig> sideColors = new List<SideColorConfig>
  {
    new SideColorConfig(8, new Color(0.96f, 0.37f, 0.26f), new Color(0.96f, 0.37f, 0.26f)),
    new SideColorConfig(9, new Color(0.19f, 0.75f, 0.42f), new Color(0.19f, 0.75f, 0.42f)),
    new SideColorConfig(10, new Color(0.24f, 0.55f, 0.98f), new Color(0.24f, 0.55f, 0.98f)),
    new SideColorConfig(11, new Color(1.00f, 0.72f, 0.18f), new Color(1.00f, 0.72f, 0.18f))
  };
  [SerializeField] private Color unselectedBackgroundColor = new Color(1f, 1f, 1f, 0f);
  [SerializeField] private Color selectedTextColor = Color.white;

  [Header("Bottom Right - 4 Percentage Views")]
  [SerializeField] private List<RightStatView> rightStatViews = new List<RightStatView>();

  private readonly List<int> statsHistory = new List<int>();
  private readonly List<int> leftGridValues = new List<int>();
  private readonly Dictionary<int, SideColorConfig> sideColorMap = new Dictionary<int, SideColorConfig>();
  private bool hasLoggedMatchSideParseWarning;
  private bool hasLoggedRoundJsonParseWarning;

  private static readonly Regex SideRegex = new Regex(@"player_(8|9|10|11)", RegexOptions.Compiled);

  private void Awake()
  {
    AutoBindLeftGridViews();
    BuildColorMap();
  }

  internal void InitializeFromJoinStats(List<string> rawStats)
  {
    statsHistory.Clear();

    if (rawStats != null)
    {
      for (int i = 0; i < rawStats.Count; i++)
      {
        if (TryParseSideValue(rawStats[i], out int side))
          statsHistory.Add(side);
      }
    }

    TrimHistoryToWindow();

    leftGridValues.Clear();
    int startIndex = Mathf.Max(0, statsHistory.Count - GridSize);
    for (int i = startIndex; i < statsHistory.Count; i++)
      leftGridValues.Add(statsHistory[i]);

    RenderLeftGrid();
    UpdateRightStats();
  }

  internal void OnNewRoundResult(int side)
  {
    if (!IsSupportedSide(side))
      return;

    statsHistory.Add(side);
    TrimHistoryToWindow();

    if (leftGridValues.Count >= GridSize)
    {
      int removeCount = Mathf.Min(ShiftCount, leftGridValues.Count);
      leftGridValues.RemoveRange(0, removeCount);
      RenderLeftGrid(true);
    }

    if (leftGridValues.Count < GridSize)
    {
      leftGridValues.Add(side);
      int latestIndex = leftGridValues.Count - 1;
      RefreshSelectionForFilledCells(latestIndex, true);
      AnimateLatestCell(latestIndex, side);
    }

    UpdateRightStats();
  }

  private void RenderLeftGrid(bool collapseEmptySlots = false)
  {
    int filledCount = leftGridValues.Count;
    int selectedIndex = filledCount - 1;

    for (int i = 0; i < leftGridViews.Count; i++)
    {
      StatsGridItemView view = leftGridViews[i];
      if (view == null)
        continue;

      if (i < filledCount)
      {
        int side = leftGridValues[i];
        SideColorConfig colors = GetSideColor(side);
        bool isSelected = i == selectedIndex;

        view.SetValue(side, isSelected, colors.SelectedBackgroundColor, unselectedBackgroundColor, colors.UnselectedTextColor, selectedTextColor);
      }
      else
      {
        view.SetEmpty(collapseEmptySlots);
      }
    }
  }

  private void RefreshSelectionForFilledCells(int selectedIndex, bool skipSelectedCell)
  {
    for (int i = 0; i < leftGridValues.Count && i < leftGridViews.Count; i++)
    {
      if (skipSelectedCell && i == selectedIndex)
        continue;

      StatsGridItemView view = leftGridViews[i];
      if (view == null)
        continue;

      int side = leftGridValues[i];
      SideColorConfig colors = GetSideColor(side);
      bool isSelected = i == selectedIndex;

      view.SetValue(side, isSelected, colors.SelectedBackgroundColor, unselectedBackgroundColor, colors.UnselectedTextColor, selectedTextColor);
    }
  }

  private void AnimateLatestCell(int index, int side)
  {
    if (index < 0 || index >= leftGridViews.Count)
      return;

    StatsGridItemView view = leftGridViews[index];
    if (view == null)
      return;

    SideColorConfig colors = GetSideColor(side);
    view.AnimateNewValue(side, colors.SelectedBackgroundColor, unselectedBackgroundColor, colors.UnselectedTextColor, selectedTextColor);
  }

  private void UpdateRightStats()
  {
    int count = statsHistory.Count;

    for (int i = 0; i < rightStatViews.Count; i++)
    {
      RightStatView rightView = rightStatViews[i];
      if (rightView == null || !IsSupportedSide(rightView.SideValue))
        continue;

      int sideCount = CountSideInWindow(rightView.SideValue);
      float pct = count <= 0 ? 0f : (sideCount * 100f) / count;
      float fill = count <= 0 ? 0f : sideCount / (float)count;

      if (rightView.PercentageText != null)
        rightView.PercentageText.text = Mathf.RoundToInt(pct) + "%";

      if (rightView.FillImage != null)
        rightView.FillImage.fillAmount = Mathf.Clamp01(fill);
    }
  }

  private int CountSideInWindow(int side)
  {
    int sideCount = 0;
    for (int i = 0; i < statsHistory.Count; i++)
    {
      if (statsHistory[i] == side)
        sideCount++;
    }

    return sideCount;
  }

  private void TrimHistoryToWindow()
  {
    int overflow = statsHistory.Count - PercentWindow;
    if (overflow > 0)
      statsHistory.RemoveRange(0, overflow);
  }

  private void AutoBindLeftGridViews()
  {
    if (leftGridViews != null && leftGridViews.Count > 0)
      return;

    Transform root = leftGridRoot != null ? leftGridRoot : transform;
    StatsGridItemView[] found = root.GetComponentsInChildren<StatsGridItemView>(true);
    leftGridViews = new List<StatsGridItemView>(found);
  }

  private void BuildColorMap()
  {
    sideColorMap.Clear();
    for (int i = 0; i < sideColors.Count; i++)
    {
      SideColorConfig config = sideColors[i];
      if (config == null || !IsSupportedSide(config.SideValue))
        continue;

      sideColorMap[config.SideValue] = config;
    }
  }

  private SideColorConfig GetSideColor(int side)
  {
    if (sideColorMap.TryGetValue(side, out SideColorConfig config))
      return config;

    return new SideColorConfig
    {
      sideValue = side,
      selectedBackgroundColor = Color.white,
      unselectedTextColor = Color.white
    };
  }

  private static bool IsSupportedSide(int side)
  {
    return side == 8 || side == 9 || side == 10 || side == 11;
  }

  private bool TryParseSideValue(string rawValue, out int side)
  {
    side = 0;
    if (string.IsNullOrWhiteSpace(rawValue))
      return false;

    // Most common case from JOIN_LEVEL stats: "{\"matchSide\":\"player_9\"}"
    try
    {
      MatchSidePayload parsed = JsonConvert.DeserializeObject<MatchSidePayload>(rawValue);
      if (parsed != null && TryParseSideFromMatchSide(parsed.matchSide, out side))
        return true;
    }
    catch (Exception ex)
    {
      if (!hasLoggedMatchSideParseWarning)
      {
        Debug.LogWarning("Stats matchSide parse fallback enabled: " + ex.Message);
        hasLoggedMatchSideParseWarning = true;
      }
    }

    // If this is a full JSON payload (round_end), probe common structures.
    try
    {
      JToken token = JToken.Parse(rawValue);
      JToken matchSideToken = token.SelectToken("matchSide");
      
      if (matchSideToken != null && TryParseSideFromMatchSide(matchSideToken.ToString(), out side))
        return true;
    }
    catch (Exception ex)
    {
      if (!hasLoggedRoundJsonParseWarning)
      {
        Debug.LogWarning("Stats JSON parse fallback enabled: " + ex.Message);
        hasLoggedRoundJsonParseWarning = true;
      }
    }

    // Last fallback: regex search in raw string.
    Match regexMatch = SideRegex.Match(rawValue);
    if (regexMatch.Success && int.TryParse(regexMatch.Groups[1].Value, out side) && IsSupportedSide(side))
      return true;

    return false;
  }

  private static bool TryParseSideFromMatchSide(string matchSide, out int side)
  {
    side = 0;
    if (string.IsNullOrWhiteSpace(matchSide))
      return false;

    Match match = SideRegex.Match(matchSide);
    if (!match.Success)
      return false;

    if (!int.TryParse(match.Groups[1].Value, out side))
      return false;

    return IsSupportedSide(side);
  }
}
