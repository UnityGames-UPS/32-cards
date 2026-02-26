using DG.Tweening;
using UnityEngine;

public class CSTextPoolItem : MonoBehaviour
{
  [SerializeField] private RectTransform rectTransform;
  [SerializeField] private CanvasGroup canvasGroup;
  private Sequence activeSequence;

  private void Awake()
  {
    if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
    if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
  }

  internal void Play(Vector2 startAnchoredPos, Vector2 endAnchoredPos, float duration, float fadeTo, CSTextPoolManager owner)
  {
    if (rectTransform == null || canvasGroup == null)
    {
      if (owner != null) owner.ReturnToPool(this);
      return;
    }

    activeSequence?.Kill();
    rectTransform.anchoredPosition = startAnchoredPos;
    canvasGroup.alpha = 1f;

    activeSequence = DOTween.Sequence();
    activeSequence.Join(rectTransform.DOAnchorPos(endAnchoredPos, duration).SetEase(Ease.OutQuad));
    activeSequence.Join(canvasGroup.DOFade(fadeTo, duration).SetEase(Ease.OutQuad));
    activeSequence.OnComplete(() =>
    {
      if (owner != null) owner.ReturnToPool(this);
    });
  }

  private void OnDisable()
  {
    activeSequence?.Kill();
  }
}
