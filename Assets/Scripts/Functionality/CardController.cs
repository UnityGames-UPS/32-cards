using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardController : MonoBehaviour
{
  [Header("Flip Settings")]
  [SerializeField] private float CardFlipDurationSeconds = 0.5f;
  [SerializeField] private float WaitForFlipSeconds = 0.5f;
  [SerializeField, Range(0f, 1f)] private float RevealAtFlipPercent = 0.5f;
  [SerializeField, Range(-1f, 1f)] private float FlipDirection = 1f;
  [SerializeField] private List<Sprite> RandomCardSprites;
  [SerializeField] private Transform SpawnPoint_Transform;
  [SerializeField] private Transform Card8Pos_Transform;
  [SerializeField] private Transform Card9Pos_Transform;
  [SerializeField] private Transform Card10Pos_Transform;
  [SerializeField] private Transform Card11Pos_Transform;
  [SerializeField] private GameObject Card8_Object;
  [SerializeField] private GameObject Card9_Object;
  [SerializeField] private GameObject Card10_Object;
  [SerializeField] private GameObject Card11_Object;

  internal void SpawnCard(int type)
  {
    GameObject prefab = null;
    Transform parent = null;
    switch (type)
    {
      case 8:
        prefab = Card8_Object;
        parent = Card8Pos_Transform;
        break;
      case 9:
        prefab = Card9_Object;
        parent = Card9Pos_Transform;
        break;
      case 10:
        prefab = Card10_Object;
        parent = Card10Pos_Transform;
        break;
      case 11:
        prefab = Card11_Object;
        parent = Card11Pos_Transform;
        break;
    }

    if (prefab == null || parent == null)
    {
      return;
    }

    GameObject cardref = GameObject.Instantiate(prefab, parent, false);
    cardref.transform.localPosition = Vector3.zero;
    cardref.transform.localRotation = Quaternion.identity;
    cardref.transform.localScale = Vector3.one;

    cardref.transform.localEulerAngles = new Vector3(0f, 0f, 180f);
    StartCoroutine(FlipAndReveal(cardref));
  }

  private IEnumerator FlipAndReveal(GameObject cardref)
  {
    if (cardref == null) yield break;

    Image cardImage = cardref.GetComponent<Image>();
    if (cardImage == null) cardImage = cardref.GetComponentInChildren<Image>();

        UITrapezoidTopNarrow trapezoid = cardref.GetComponent<UITrapezoidTopNarrow>();
        if (trapezoid == null) trapezoid = cardref.GetComponentInChildren<UITrapezoidTopNarrow>();
        float startTopInset = trapezoid != null ? trapezoid.TopInset : 0f;
        float startBottomInset = trapezoid != null ? trapezoid.BottomInset : 0f;
        float endTopInset = startBottomInset;
        float endBottomInset = startTopInset;

    float wait = Mathf.Max(0f, WaitForFlipSeconds);
    if (wait > 0f) yield return new WaitForSeconds(wait);

    bool revealed = false;
    float duration = Mathf.Max(0.01f, CardFlipDurationSeconds);
    float elapsed = 0f;
    float revealPercent = Mathf.Clamp01(RevealAtFlipPercent);
    while (elapsed < duration)
    {
      elapsed += Time.deltaTime;
      float t = Mathf.Clamp01(elapsed / duration);
      float direction = FlipDirection >= 0f ? 1f : -1f;
      float angle = Mathf.Lerp(0f, 180f * direction, t);
            cardref.transform.localEulerAngles = new Vector3(angle, 180f, 180f);
            if (trapezoid != null)
            {
                trapezoid.TopInset = Mathf.Lerp(startTopInset, endTopInset, t);
                trapezoid.BottomInset = Mathf.Lerp(startBottomInset, endBottomInset, t);
                Graphic graphic = trapezoid.GetComponent<Graphic>();
                if (graphic != null) graphic.SetVerticesDirty();
            }
            if (!revealed && t >= revealPercent) { SetRandomCardSprite(cardImage, trapezoid); revealed = true; }
            yield return null;
        }
        if (!revealed) SetRandomCardSprite(cardImage, trapezoid);
    }

    private void SetRandomCardSprite(Image cardImage, UITrapezoidTopNarrow trapezoid)
    {
        if (cardImage == null)
        {
            return;
        }
        if (RandomCardSprites == null || RandomCardSprites.Count == 0)
        {
            return;
        }
        int index = UnityEngine.Random.Range(0, RandomCardSprites.Count);
        cardImage.sprite = RandomCardSprites[index];
    }
}
