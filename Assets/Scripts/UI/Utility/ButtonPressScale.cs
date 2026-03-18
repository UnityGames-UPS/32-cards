using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

public class ButtonPressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
  [SerializeField] private float scaleMultiplier = 0.95f;
  [SerializeField] private float duration = 0.1f;
  [SerializeField] private Ease ease = Ease.OutQuad;

  private Button button;
  private RectTransform rectTransform;
  private Vector3 originalScale;
  private Tween scaleTween;

  void OnValidate()
  {
    GetButtonRef();
  }

  private void Awake()
  {
    GetButtonRef();
    rectTransform = transform as RectTransform;
    originalScale = rectTransform != null ? rectTransform.localScale : transform.localScale;
  }

  void GetButtonRef()
  {
    if (button == null)
    {
      button = GetComponent<Button>();
    }
  }

  private void OnDisable()
  {
    scaleTween?.Kill();
  }

  public void OnPointerDown(PointerEventData eventData)
  {
    if(button != null && !button.interactable) return;
    AnimateToScale(originalScale * scaleMultiplier);
  }

  public void OnPointerUp(PointerEventData eventData)
  {
    if(button != null && !button.interactable) return;
    AnimateToScale(originalScale);
  }

  public void OnPointerExit(PointerEventData eventData)
  {
    if(button != null && !button.interactable) return;
    AnimateToScale(originalScale);
  }

  private void AnimateToScale(Vector3 targetScale)
  {
    scaleTween?.Kill();
    scaleTween = transform.DOScale(targetScale, duration).SetEase(ease).SetUpdate(true);
  }
}
