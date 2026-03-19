using UnityEngine;
using TMPro;
using System;
using System.Globalization;

public class BetHistoryItemView : MonoBehaviour
{
  [SerializeField] private TMP_Text numberText;
  [SerializeField] private TMP_Text roundIdTimeText;
  [SerializeField] private TMP_Text stakeText;
  [SerializeField] private TMP_Text winText;
  [SerializeField] private TMP_Text netText;
  [SerializeField] private TMP_Text playerText;
  [SerializeField] private TMP_Text valueText;

  public void Populate(int globalNumber, BetHistoryEntry entry)
  {
    if (numberText != null)
      numberText.text = globalNumber.ToString();

    if (roundIdTimeText != null)
      roundIdTimeText.text = entry.round_id + "\n" + FormatDateTime(entry.created_at);

    if (stakeText != null)
      stakeText.text = GameUtility.FormatCurrency(entry.bet_amount, kFrom1000: true);

    if (winText != null)
      winText.text = GameUtility.FormatCurrency(entry.win_amount, kFrom1000: true);

    if (netText != null)
    {
      double net = entry.win_amount - entry.bet_amount;
      string sign = net >= 0 ? "+" : "-";
      netText.text = sign + GameUtility.FormatCurrency(Math.Abs(net), kFrom1000: true);
    }

    if (playerText != null)
    {
      string playerNum = !string.IsNullOrEmpty(entry.match_side)
          ? entry.match_side.Replace("player_", "")
          : "-";
      playerText.text = playerNum;
    }

    if (valueText != null)
      valueText.text = entry.win_score.ToString();
  }

  private string FormatDateTime(string isoString)
  {
    if (string.IsNullOrEmpty(isoString))
      return "";

    if (DateTime.TryParse(isoString, null, DateTimeStyles.RoundtripKind, out DateTime dt))
    {
      DateTime local = dt.ToLocalTime();
      return local.ToString("dd/MM/yyyy") + "\n" + local.ToString("hh:mm:ss tt");
    }

    return isoString;
  }
}
