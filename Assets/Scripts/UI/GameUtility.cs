using System.Collections.Generic;
using System.Text;

internal static class GameUtility
{
  private static readonly Dictionary<double, string> _currencyCache = new Dictionary<double, string>();
  private static readonly StringBuilder _sb = new StringBuilder(16);

  internal static string FormatCurrency(double amount)
  {
    if (_currencyCache.TryGetValue(amount, out string cached)) return cached;

    string result;
    if (amount >= 10000)
    {
      _sb.Clear();
      _sb.Append((amount / 1000).ToString("F1"));
      _sb.Append("K");
      result = _sb.ToString();
    }
    else if (amount < 1) result = amount.ToString("F1");
    else if (amount % 1 != 0) result = amount.ToString("F1");
    else result = amount.ToString("F0");

    if (_currencyCache.Count < 300) _currencyCache[amount] = result;
    return result;
  }
}
