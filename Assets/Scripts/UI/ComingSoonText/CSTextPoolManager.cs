using UnityEngine;

public class CSTextPoolManager : GenericObjectPool<CSTextPoolItem>
{
  [Header("Text Animation")]
  [SerializeField] private Canvas rootCanvas;
  [SerializeField] private float startYOffset = 70f;
  [SerializeField] private float endYOffset = 50f;
  [SerializeField] private float moveDuration = 0.5f;
  [SerializeField] private float fadeTo = 0f;

  internal void PlayAtScreenPoint(Vector2 screenPoint)
  {
    RectTransform parentRect = ParentTransform as RectTransform;
    if (parentRect == null) return;

    if (rootCanvas == null) rootCanvas = parentRect.GetComponentInParent<Canvas>();
    Camera uiCamera = rootCanvas != null ? rootCanvas.worldCamera : null;
    if (rootCanvas != null && rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
      uiCamera = null;

    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, uiCamera, out Vector2 localPoint))
      return;

    CSTextPoolItem item = GetFromPool();
    Vector2 startPos = localPoint + new Vector2(0f, startYOffset);
    Vector2 endPos = localPoint + new Vector2(0f, endYOffset);
    item.Play(startPos, endPos, moveDuration, fadeTo, this);
  }
}
