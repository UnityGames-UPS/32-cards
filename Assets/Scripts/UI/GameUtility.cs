using System.Collections.Generic;
using System.Text;

internal static class GameUtility
{
  private static readonly Dictionary<double, string> _currencyCache = new Dictionary<double, string>();
  private static readonly Dictionary<double, string> _currencySkipKCache = new Dictionary<double, string>();
  private static readonly StringBuilder _sb = new StringBuilder(16);

  internal static string FormatCurrency(double amount, bool skipKFormat = false)
  {
    var cache = skipKFormat ? _currencySkipKCache : _currencyCache;
    if (cache.TryGetValue(amount, out string cached)) return cached;

    string result;
    if (amount >= 10000 && !skipKFormat)
    {
      _sb.Clear();
      _sb.Append((amount / 1000).ToString("N2"));
      _sb.Append("K");
      result = _sb.ToString();
    }
    else if (amount < 1) result = amount.ToString("N2");
    else if (amount % 1 != 0) result = amount.ToString("N2");
    else result = amount.ToString("F0");

    if (cache.Count < 300) cache[amount] = result;
    return result;
  }

  internal static string FormatUsername(string username)
  {
    if (string.IsNullOrEmpty(username) || username.Length <= 4) return username;

    int firstChars = 1;
    int lastChars = 3;
    int maskedLength = username.Length - firstChars - lastChars;
    if (maskedLength <= 0) return username;

    return username.Substring(0, firstChars)
         + new string('*', 3)
         + username.Substring(username.Length - lastChars);
  }
}
