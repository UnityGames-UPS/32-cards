using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ButtonPressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private float scaleMultiplier = 0.95f;
    [SerializeField] private float duration = 0.1f;
    [SerializeField] private Ease ease = Ease.OutQuad;

    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Tween scaleTween;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        originalScale = rectTransform != null ? rectTransform.localScale : transform.localScale;
    }

    private void OnDisable()
    {
        scaleTween?.Kill();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        AnimateToScale(originalScale * scaleMultiplier);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        AnimateToScale(originalScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        AnimateToScale(originalScale);
    }

    private void AnimateToScale(Vector3 targetScale)
    {
        scaleTween?.Kill();
        scaleTween = transform.DOScale(targetScale, duration).SetEase(ease).SetUpdate(true);
    }
}
