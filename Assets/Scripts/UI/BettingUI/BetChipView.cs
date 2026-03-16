using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BetChipView : MonoBehaviour
{
  [Header("References")]
  [SerializeField] private RectTransform chipRect;
  [SerializeField] private CanvasGroup chipCanvasGroup;
  [SerializeField] private Image chipImage;
  [SerializeField] private TMP_Text chipValueText;

  internal RectTransform ChipRect => chipRect;
  internal CanvasGroup ChipCanvasGroup => chipCanvasGroup;
  internal Sprite ChipSprite => chipImage != null ? chipImage.sprite : null;
  internal float ChipValue
  {
    get
    {
      if (chipValueText == null)
        return 0f;

      return float.TryParse(chipValueText.text, out float value) ? value : 0f;
    }
  }

  internal void SetChipVisuals(Sprite sprite, string valueText)
  {
    if (chipImage != null)
      chipImage.sprite = sprite;

    if (chipValueText != null)
      chipValueText.text = valueText;
  }

  internal void SetChipValueText(string valueText)
  {
    if (chipValueText != null)
      chipValueText.text = valueText;
  }
}
