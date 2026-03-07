using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsGridItemView : MonoBehaviour
{
  [Header("References")]
  [SerializeField] private Image selectedBackgroundImage;
  [SerializeField] private TMP_Text valueText;

  [Header("Animation")]
  [SerializeField] private float popDuration = 0.2f;
  [SerializeField] private Ease popEase = Ease.OutBack;

  private Vector3 initialImageScale = Vector3.one;
  private Vector3 initialTextScale = Vector3.one;
  private Tween imageScaleTween;
  private Tween textScaleTween;

  private void OnValidate()
  {
    CacheChildReferences();
    CacheInitialScales();
  }

  private void Awake()
  {
    CacheChildReferences();
    CacheInitialScales();
  }

  private void OnDisable()
  {
    KillTweens();
  }

  internal void SetEmpty(bool collapse)
  {
    KillTweens();

    if (valueText != null)
    {
      valueText.transform.localScale = collapse ? Vector3.zero : initialTextScale;
      Color color = valueText.color;
      color.a = 0f;
      valueText.color = color;
      valueText.text = string.Empty;
    }

    if (selectedBackgroundImage != null)
    {
      selectedBackgroundImage.gameObject.SetActive(true);
      selectedBackgroundImage.transform.localScale = collapse ? Vector3.zero : initialImageScale;
    }
  }

  internal void SetValue(int value, bool isSelected, Color selectedBackgroundColor, Color unselectedBackgroundColor, Color unselectedTextColor, Color selectedTextColor)
  {
    KillTweens();
    SetChildScales(initialImageScale, initialTextScale);
    ApplyVisual(value, isSelected, selectedBackgroundColor, unselectedBackgroundColor, unselectedTextColor, selectedTextColor);
  }

  internal void AnimateNewValue(int value, Color selectedBackgroundColor, Color unselectedBackgroundColor, Color unselectedTextColor, Color selectedTextColor)
  {
    KillTweens();

    SetChildScales(Vector3.zero, Vector3.zero);
    ApplyVisual(value, true, selectedBackgroundColor, unselectedBackgroundColor, unselectedTextColor, selectedTextColor);

    if (selectedBackgroundImage != null)
      imageScaleTween = selectedBackgroundImage.transform.DOScale(initialImageScale, popDuration).SetEase(popEase);

    if (valueText != null)
      textScaleTween = valueText.transform.DOScale(initialTextScale, popDuration).SetEase(popEase);
  }

  private void ApplyVisual(int value, bool isSelected, Color selectedBackgroundColor, Color unselectedBackgroundColor, Color unselectedTextColor, Color selectedTextColor)
  {
    if (valueText != null)
    {
      valueText.text = value.ToString();
      valueText.color = isSelected ? selectedTextColor : unselectedTextColor;

      Color color = valueText.color;
      color.a = 1f;
      valueText.color = color;
    }

    if (selectedBackgroundImage == null)
      return;

    if (isSelected)
    {
      selectedBackgroundImage.gameObject.SetActive(true);
      selectedBackgroundImage.color = selectedBackgroundColor;
    }
    else
    {
      selectedBackgroundImage.color = unselectedBackgroundColor;
      selectedBackgroundImage.gameObject.SetActive(false);
    }
  }

  private void KillTweens()
  {
    if (imageScaleTween != null && imageScaleTween.IsActive())
      imageScaleTween.Kill();
    if (textScaleTween != null && textScaleTween.IsActive())
      textScaleTween.Kill();
    imageScaleTween = null;
    textScaleTween = null;
  }

  private void CacheChildReferences()
  {
    if (selectedBackgroundImage == null && transform.childCount > 0)
      selectedBackgroundImage = transform.GetChild(0).GetComponent<Image>();

    if (valueText == null && transform.childCount > 1)
      valueText = transform.GetChild(1).GetComponent<TMP_Text>();
  }

  private void CacheInitialScales()
  {
    if (selectedBackgroundImage != null)
      initialImageScale = selectedBackgroundImage.transform.localScale;

    if (valueText != null)
      initialTextScale = valueText.transform.localScale;
  }

  private void SetChildScales(Vector3 imageScale, Vector3 textScale)
  {
    if (selectedBackgroundImage != null)
      selectedBackgroundImage.transform.localScale = imageScale;

    if (valueText != null)
      valueText.transform.localScale = textScale;
  }
}
