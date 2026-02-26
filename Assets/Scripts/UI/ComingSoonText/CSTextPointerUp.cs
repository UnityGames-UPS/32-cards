using UnityEngine;
using UnityEngine.EventSystems;

public class CSTextPointerUp : MonoBehaviour, IPointerUpHandler
{
  [SerializeField] private CSTextPoolManager poolManager;

  public void OnPointerUp(PointerEventData eventData)
  {
    if (poolManager == null) return;
    poolManager.PlayAtScreenPoint(eventData.position);
  }
}
