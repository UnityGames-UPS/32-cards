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

  public RectTransform ChipRect => chipRect;
  public CanvasGroup ChipCanvasGroup => chipCanvasGroup;
  public Sprite ChipSprite => chipImage != null ? chipImage.sprite : null;
  public float ChipValue
  {
    get
    {
      if (chipValueText == null)
        return 0f;

      return float.TryParse(chipValueText.text, out float value) ? value : 0f;
    }
  }

  public void SetChipVisuals(Sprite sprite, string valueText)
  {
    if (chipImage != null)
      chipImage.sprite = sprite;

    if (chipValueText != null)
      chipValueText.text = valueText;
  }

  public void SetChipValueText(string valueText)
  {
    if (chipValueText != null)
      chipValueText.text = valueText;
  }
}
