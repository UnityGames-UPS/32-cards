using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DealerController : MonoBehaviour
{
  [Serializable]
  private class DealSegment
  {
    public int StartFrame;
    public int EndFrame;
  }

  [Header("ImageAnimations")]
  [SerializeField]
  private ImageAnimation DealerImageAnim_IA;
  [SerializeField]
  private ImageAnimation BoxAnim_IA;
  [SerializeField]
  private ImageAnimation LeftHandAnim_IA;
  [SerializeField]
  private ImageAnimation RightHandAnim_IA;
  [SerializeField]
  private ImageAnimation BothHandAnim_IA;

  [Header("Sprites")]
  [SerializeField]
  private List<Sprite> BoxOpen_Sprites;
  [SerializeField]
  private List<Sprite> BoxClose_Sprites;
  [SerializeField]
  private List<Sprite> DealerShuffle_Sprites;
  [SerializeField]
  private List<Sprite> DealerReset_Sprites;
  [SerializeField]
  private List<Sprite> DealerDeal_Sprites;
  [SerializeField]
  private List<Sprite> DealerRest_Sprites;
  [SerializeField]
  private List<Sprite> BothhandsShuffle_Sprites;
  [SerializeField]
  private List<Sprite> BothHandsReset_Sprites;

  [Header("Deal Segments")]
  [SerializeField]
  private List<DealSegment> DealSegments = new List<DealSegment>();

  [Header("GameObjects")]
  [SerializeField]
  private GameObject BothHands_Object;
  [SerializeField]
  private GameObject LeftHand_Object;
  [SerializeField]
  private GameObject RightHand_Object;
  [SerializeField]
  private GameObject DealerMoving_Object;
  [SerializeField]
  private GameObject DealerRest_Object;

  [Header("Transforms")]
  [SerializeField]
  private Transform BothHands_Transform;
  [SerializeField]
  private Transform LeftHand_Transform;
  [SerializeField]
  private Transform RightHand_Transform;
  [SerializeField]
  private Transform BettingPanel_Transform;
  [SerializeField]
  private Transform Box_Transform;
  [SerializeField]
  private CardController cardManager;

  private int bothHandsDefaultSiblingIndex;
  private int leftHandDefaultSiblingIndex;
  private int rightHandDefaultSiblingIndex;
  private Coroutine dealSequenceRoutine;
  private bool isDealSegmentPlaying;

  private void Awake()
  {
    CacheHandSiblingIndexes();
  }

  private void OnEnable()
  {
    CacheHandSiblingIndexes();
  }

  private void CacheHandSiblingIndexes()
  {
    if (BothHands_Transform != null)
    {
      bothHandsDefaultSiblingIndex = BothHands_Transform.GetSiblingIndex();
    }
    if (LeftHand_Transform != null)
    {
      leftHandDefaultSiblingIndex = LeftHand_Transform.GetSiblingIndex();
    }
    if (RightHand_Transform != null)
    {
      rightHandDefaultSiblingIndex = RightHand_Transform.GetSiblingIndex();
    }
  }

  private void ShuffleCardsAnimation()
  {
    DealerImageAnim_IA.textureArray.Clear();
    DealerImageAnim_IA.textureArray.TrimExcess();
    DealerImageAnim_IA.textureArray.AddRange(DealerShuffle_Sprites);

    BothHandAnim_IA.textureArray.Clear();
    BothHandAnim_IA.textureArray.TrimExcess();
    BothHandAnim_IA.textureArray.AddRange(BothhandsShuffle_Sprites);

    DealerImageAnim_IA.AnimationSpeed = 90;
    BothHandAnim_IA.AnimationSpeed = 90;

    BothHands_Object.SetActive(true);
    LeftHand_Object.SetActive(false);
    RightHand_Object.SetActive(false);

    DealerImageAnim_IA.cardReset = false;
    DealerImageAnim_IA.cardShuffle = true;

    DealerMoving_Object.SetActive(true);
    DealerRest_Object.SetActive(false);

    DealerImageAnim_IA.StopAnimation();
    BothHandAnim_IA.StopAnimation();
    DealerImageAnim_IA.StartAnimation();
    BothHandAnim_IA.StartAnimation();
  }

  private void resetCardsAnimation()
  {
    DealerImageAnim_IA.textureArray.Clear();
    DealerImageAnim_IA.textureArray.TrimExcess();
    DealerImageAnim_IA.textureArray.AddRange(DealerReset_Sprites);

    BothHandAnim_IA.textureArray.Clear();
    BothHandAnim_IA.textureArray.TrimExcess();
    BothHandAnim_IA.textureArray.AddRange(BothHandsReset_Sprites);

    DealerImageAnim_IA.AnimationSpeed = 15;
    BothHandAnim_IA.AnimationSpeed = 15;

    BothHands_Object.SetActive(true);
    LeftHand_Object.SetActive(false);
    RightHand_Object.SetActive(false);

    DealerImageAnim_IA.cardReset = true;
    DealerImageAnim_IA.cardShuffle = false;

    DealerMoving_Object.SetActive(true);
    DealerRest_Object.SetActive(false);

    DealerImageAnim_IA.StopAnimation();
    BothHandAnim_IA.StopAnimation();
    DealerImageAnim_IA.StartAnimation();
    BothHandAnim_IA.StartAnimation();
  }

  private void PrepareDealAnimationState()
  {
    DealerImageAnim_IA.textureArray.Clear();
    DealerImageAnim_IA.textureArray.TrimExcess();
    DealerImageAnim_IA.textureArray.AddRange(DealerDeal_Sprites);

    DealerImageAnim_IA.AnimationSpeed = 215;

    BothHands_Object.SetActive(false);
    LeftHand_Object.SetActive(true);
    RightHand_Object.SetActive(true);

    DealerImageAnim_IA.cardReset = false;
    DealerImageAnim_IA.cardShuffle = false;

    DealerMoving_Object.SetActive(true);
    DealerRest_Object.SetActive(false);

    DealerImageAnim_IA.StopAnimation();
    LeftHandAnim_IA.StopAnimation();
    RightHandAnim_IA.StopAnimation();
  }

  private bool TryGetDealSegment(int spotIndex, out DealSegment segment)
  {
    segment = null;
    if (DealSegments == null || DealSegments.Count <= spotIndex)
    {
      return false;
    }
    segment = DealSegments[spotIndex];
    return segment != null;
  }

  private void PlayDealSegment(int spotIndex, Action onComplete)
  {
    if (!TryGetDealSegment(spotIndex, out DealSegment segment))
    {
      isDealSegmentPlaying = false;
      onComplete?.Invoke();
      return;
    }

    PrepareDealAnimationState();
    if (DealerImageAnim_IA.textureArray == null || DealerImageAnim_IA.textureArray.Count == 0)
    {
      isDealSegmentPlaying = false;
      onComplete?.Invoke();
      return;
    }
    isDealSegmentPlaying = true;
    SetLayeringForLeftHand(true);
    SetLayeringForRightHand(true);

    LeftHandAnim_IA.PlaySegment(segment.StartFrame, segment.EndFrame, (frame) =>
    {
      // switch (frame)
      // {
      //   case 14:
      //     SetLayeringForLeftHand(true);
      //     break;
      //   case 53:
      //     SetLayeringForLeftHand(false);
      //     break;
      //   case 72:
      //     SetLayeringForLeftHand(true);
      //     break;
      //   case 116:
      //     SetLayeringForLeftHand(false);
      //     break;
      //   case 235:
      //     SetLayeringForLeftHand(false);
      //     break;
      // }
    }, null);
    RightHandAnim_IA.PlaySegment(segment.StartFrame, segment.EndFrame, (frame) =>
    {
      // switch (frame)
      // {
      //   case 8:
      //     SetLayeringForRightHand(true);
      //     break;
      //   case 30:
      //     SetLayeringForRightHand(false);
      //     break;
      //   case 67:
      //     SetLayeringForRightHand(true);
      //     break;
      //   case 89:
      //     SetLayeringForRightHand(false);
      //     break;
      //   case 126:
      //     SetLayeringForRightHand(true);
      //     break;
      //   case 185:
      //     SetLayeringForRightHand(true);
      //     break;
      //   case 235:
      //     SetLayeringForRightHand(false);
      //     break;
      // }
    }, null);

    DealerImageAnim_IA.PlaySegment(segment.StartFrame, segment.EndFrame, (frame) =>
    {
      switch (frame)
      {
        case 29:
          SpawnCard(8);
          break;
        case 89:
          SpawnCard(9);
          break;
        case 149:
          SpawnCard(10);
          break;
        case 209:
          SpawnCard(11);
          break;
      }
    }, () =>
    {
      LeftHandAnim_IA.StopAnimation();
      RightHandAnim_IA.StopAnimation();
      SetLayeringForRightHand(false);
      SetLayeringForLeftHand(false);
      SwitchToRest();
      isDealSegmentPlaying = false;
      onComplete?.Invoke();
    });
  }

  private IEnumerator PlayAllDealSegments()
  {
    for (int i = 0; i < 4; i++)
    {
      PlayDealSegment(i, null);
      yield return new WaitUntil(() => !isDealSegmentPlaying);
    }
  }

  internal void BoxOpenAnimation()
  {
    BoxAnim_IA.textureArray.Clear();
    BoxAnim_IA.textureArray.TrimExcess();
    BoxAnim_IA.textureArray.AddRange(BoxOpen_Sprites);
    BoxAnim_IA.AnimationSpeed = 20;
    BoxAnim_IA.StopAnimation();
    BoxAnim_IA.StartAnimation();
  }
  internal void BoxCloseAnimation()
  {
    BoxAnim_IA.textureArray.Clear();
    BoxAnim_IA.textureArray.TrimExcess();
    BoxAnim_IA.textureArray.AddRange(BoxClose_Sprites);
    BoxAnim_IA.AnimationSpeed = 20;
    BoxAnim_IA.StopAnimation();
    BoxAnim_IA.StartAnimation();
  }

  internal void SetLayeringForBothHands(bool IsAbove)
  {
    if (IsAbove)
    {
      BothHands_Transform.SetSiblingIndex(Box_Transform.GetSiblingIndex());
    }
    else
    {
      BothHands_Transform.SetSiblingIndex(bothHandsDefaultSiblingIndex);
    }
  }
  internal void SetLayeringForLeftHand(bool IsAbove)
  {
    if (IsAbove)
    {
      LeftHand_Transform.SetSiblingIndex(Box_Transform.GetSiblingIndex());
    }
    else
    {
      LeftHand_Transform.SetSiblingIndex(leftHandDefaultSiblingIndex);
    }
  }
  internal void SetLayeringForRightHand(bool IsAbove)
  {
    if (IsAbove)
    {
      RightHand_Transform.SetSiblingIndex(Box_Transform.GetSiblingIndex());
    }
    else
    {
      RightHand_Transform.SetSiblingIndex(rightHandDefaultSiblingIndex);
    }
  }

  internal void SwitchToRest()
  {
    DealerMoving_Object.SetActive(false);
    DealerRest_Object.SetActive(true);

    BothHands_Object.SetActive(false);
    LeftHand_Object.SetActive(false);
    RightHand_Object.SetActive(false);
  }

  void SpawnCard(int type)
  {
    cardManager.SpawnCard(type);
  }

  private void Update()
  {
    if (Input.GetKeyDown(KeyCode.A))
    {
      ShuffleCardsAnimation();
    }
    else if (Input.GetKeyDown(KeyCode.B))
    {
      resetCardsAnimation();
    }
    else if (Input.GetKeyDown(KeyCode.C))
    {
      if (dealSequenceRoutine != null)
      {
        StopCoroutine(dealSequenceRoutine);
      }
      dealSequenceRoutine = StartCoroutine(PlayAllDealSegments());
    }
    else if (Input.GetKeyDown(KeyCode.Alpha1))
    {
      if (dealSequenceRoutine != null)
      {
        StopCoroutine(dealSequenceRoutine);
      }
      PlayDealSegment(0, null);
    }
    else if (Input.GetKeyDown(KeyCode.Alpha2))
    {
      if (dealSequenceRoutine != null)
      {
        StopCoroutine(dealSequenceRoutine);
      }
      PlayDealSegment(1, null);
    }
    else if (Input.GetKeyDown(KeyCode.Alpha3))
    {
      if (dealSequenceRoutine != null)
      {
        StopCoroutine(dealSequenceRoutine);
      }
      PlayDealSegment(2, null);
    }
    else if (Input.GetKeyDown(KeyCode.Alpha4))
    {
      if (dealSequenceRoutine != null)
      {
        StopCoroutine(dealSequenceRoutine);
      }
      PlayDealSegment(3, null);
    }
  }

}
