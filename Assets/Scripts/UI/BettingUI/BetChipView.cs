using System.Collections.Generic;
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
  [SerializeField] private ImageAnimation rippleAnimation;

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

  internal void ShowRipple(List<Sprite> rippleSprites)
  {
    if (rippleAnimation == null || rippleSprites == null || rippleSprites.Count == 0) return;
    rippleAnimation.textureArray = rippleSprites;
    rippleAnimation.doLoopAnimation = true;
    rippleAnimation.gameObject.SetActive(true);
    rippleAnimation.StartAnimation();
  }

  internal void HideRipple()
  {
    if (rippleAnimation == null) return;
    rippleAnimation.StopAnimation();
    rippleAnimation.gameObject.SetActive(false);
  }
}
