using System.Collections.Generic;
using System.Text;

internal static class GameUtility
{
  private static readonly Dictionary<double, string> _currencyCache = new Dictionary<double, string>();
  private static readonly Dictionary<double, string> _currencySkipKCache = new Dictionary<double, string>();
  private static readonly Dictionary<double, string> _currencyK1000Cache = new Dictionary<double, string>();
  private static readonly StringBuilder _sb = new StringBuilder(16);

  internal static string FormatCurrency(double amount, bool skipKFormat = false, bool kFrom1000 = false)
  {
    var cache = skipKFormat ? _currencySkipKCache : (kFrom1000 ? _currencyK1000Cache : _currencyCache);
    if (cache.TryGetValue(amount, out string cached)) return cached;

    string result;
    double kThreshold = kFrom1000 ? 1000 : 10000;
    if (amount >= kThreshold && !skipKFormat)
    {
      _sb.Clear();
      _sb.Append((amount / 1000).ToString("F2").TrimEnd('0').TrimEnd('.'));
      _sb.Append("K");
      result = _sb.ToString();
    }
    else if (amount == 0) result = result = amount.ToString("F0");
    else if (amount < 1) result = amount.ToString("F2");
    else if (amount % 1 != 0) result = amount.ToString("F2");
    else result = amount.ToString("F0");

    if (cache.Count < 300) cache[amount] = result;
    return result;
  }

  internal static float ParseFormattedCurrency(string text)
  {
    if (string.IsNullOrEmpty(text))
      return 0f;

    text = text.Trim().Replace(",", "");

    bool isK = text.EndsWith("K") || text.EndsWith("k");
    if (isK)
      text = text.Substring(0, text.Length - 1);

    if (float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float value))
      return isK ? value * 1000f : value;

    return 0f;
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
