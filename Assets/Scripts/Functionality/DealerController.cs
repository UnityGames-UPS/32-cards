using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DealerController : MonoBehaviour
{
  [Serializable]
  private class CardDealRequest
  {
    public int Player;
    public Card CardData;
    public CardDealtScores Scores;
  }

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
  [SerializeField]
  private ImageAnimation TopDownHandsAnim_IA;

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
  private List<Sprite> BothhandsShuffle_Sprites;
  [SerializeField]
  private List<Sprite> BothHandsReset_Sprites;
  [SerializeField]
  private List<Sprite> TopDownHandsReset_Sprites;
  [SerializeField]
  private List<Sprite> TopDownHandsDeal_Sprites;

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
  [SerializeField]
  private GameObject TopDownHandsParent_Object;

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
  [Header("Cashout Reset")]
  [SerializeField]
  private float CardDespawnStartDelay = 0.25f;
  [SerializeField]
  private float CardDespawnMiddleGroupDelay = 0.15f;
  [SerializeField]
  private float CardDisableToDestroyDelay = 0.08f;
  [SerializeField] private float CardResetDelayOnCashout = 9;

  private int bothHandsDefaultSiblingIndex;
  private int leftHandDefaultSiblingIndex;
  private int rightHandDefaultSiblingIndex;
  private Coroutine dealSequenceRoutine;
  private Coroutine queuedDealRoutine;
  private bool isDealSegmentPlaying;
  private readonly Queue<CardDealRequest> pendingDeals = new Queue<CardDealRequest>();

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

    TopDownHandsAnim_IA.textureArray.Clear();
    TopDownHandsAnim_IA.textureArray.TrimExcess();
    TopDownHandsAnim_IA.textureArray.AddRange(TopDownHandsReset_Sprites);

    DealerImageAnim_IA.AnimationSpeed = 15;
    BothHandAnim_IA.AnimationSpeed = 15;
    TopDownHandsAnim_IA.AnimationSpeed = 6.5f;

    BothHands_Object.SetActive(true);
    LeftHand_Object.SetActive(false);
    RightHand_Object.SetActive(false);

    DealerImageAnim_IA.cardReset = true;
    DealerImageAnim_IA.cardShuffle = false;

    DealerMoving_Object.SetActive(true);
    DealerRest_Object.SetActive(false);
    TopDownHandsParent_Object.SetActive(true);
    TopDownHandsAnim_IA.gameObject.SetActive(true);

    DealerImageAnim_IA.StopAnimation();
    BothHandAnim_IA.StopAnimation();
    TopDownHandsAnim_IA.StopAnimation();
    DealerImageAnim_IA.StartAnimation();
    BothHandAnim_IA.StartAnimation();
    TopDownHandsAnim_IA.StartAnimation();

    if (cardManager != null)
    {
      cardManager.FadeOutScoreTexts();
    }
  }

  private void PrepareDealAnimationState()
  {
    DealerImageAnim_IA.textureArray.Clear();
    DealerImageAnim_IA.textureArray.TrimExcess();
    DealerImageAnim_IA.textureArray.AddRange(DealerDeal_Sprites);

    TopDownHandsAnim_IA.textureArray.Clear();
    TopDownHandsAnim_IA.textureArray.TrimExcess();
    TopDownHandsAnim_IA.textureArray.AddRange(TopDownHandsDeal_Sprites);

    DealerImageAnim_IA.AnimationSpeed = 215;
    TopDownHandsAnim_IA.AnimationSpeed = 215;

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
    TopDownHandsAnim_IA.StopAnimation();
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

  private void PlayDealSegment(int player, Card cardData, CardDealtScores scores, Action onComplete)
  {
    if (!TryGetSegmentIndexForPlayer(player, out int spotIndex) || !TryGetDealSegment(spotIndex, out DealSegment segment))
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
    bool hasSpawnedCard = false;
    int targetSpawnFrame = GetSpawnFrameForPlayer(player);
    SetLayeringForLeftHand(true);
    SetLayeringForRightHand(true);

    if (TopDownHandsParent_Object != null)
    {
      TopDownHandsParent_Object.SetActive(true);
      TopDownHandsAnim_IA.gameObject.SetActive(true);
      TopDownHandsAnim_IA.PlaySegment(segment.StartFrame, segment.EndFrame, (frame) =>
      {
      }, null);
    }

    LeftHandAnim_IA.PlaySegment(segment.StartFrame, segment.EndFrame, (frame) =>
    {
    }, null);
    RightHandAnim_IA.PlaySegment(segment.StartFrame, segment.EndFrame, (frame) =>
    {
    }, null);

    DealerImageAnim_IA.PlaySegment(segment.StartFrame, segment.EndFrame, (frame) =>
    {
      if (!hasSpawnedCard && frame == targetSpawnFrame)
      {
        hasSpawnedCard = true;
        SpawnCard(player, cardData, scores);
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
      int player = 8 + i;
      PlayDealSegment(player, null, null, null);
      yield return new WaitUntil(() => !isDealSegmentPlaying);
    }
  }

  internal void ResetImmediate()
  {
    pendingDeals.Clear();
    if (queuedDealRoutine != null)
    {
      StopCoroutine(queuedDealRoutine);
      queuedDealRoutine = null;
    }
    if (dealSequenceRoutine != null)
    {
      StopCoroutine(dealSequenceRoutine);
      dealSequenceRoutine = null;
    }
    isDealSegmentPlaying = false;

    // Explicitly stop all ImageAnimations (they use Invoke, not coroutines)
    // so stopping the coroutines above is not enough to cancel scheduled frames.
    if (DealerImageAnim_IA != null) DealerImageAnim_IA.StopAnimation();
    if (LeftHandAnim_IA != null) LeftHandAnim_IA.StopAnimation();
    if (RightHandAnim_IA != null) RightHandAnim_IA.StopAnimation();
    if (BothHandAnim_IA != null) BothHandAnim_IA.StopAnimation();
    if (TopDownHandsAnim_IA != null)
    {
      TopDownHandsAnim_IA.StopAnimation();
      TopDownHandsAnim_IA.gameObject.SetActive(false);
    }

    SwitchToRest();
    if (cardManager != null)
      cardManager.ClearAllCardsImmediate();
  }

  internal void OnJoinDuringDeal(CardDealtEvent response, int previousCardsDealt)
  {
    if (response == null) return;

    pendingDeals.Clear();
    if (queuedDealRoutine != null) { StopCoroutine(queuedDealRoutine); queuedDealRoutine = null; }
    if (dealSequenceRoutine != null) { StopCoroutine(dealSequenceRoutine); dealSequenceRoutine = null; }

    if (cardManager != null)
    {
      cardManager.SyncDealtCards(response);
      // Spawn all previously dealt cards face-up immediately (skip the current player's last card)
      SpawnPlayerCardsSilent(8, response.player8Cards, response.player == 8 ? 1 : 0, response.scores);
      SpawnPlayerCardsSilent(9, response.player9Cards, response.player == 9 ? 1 : 0, response.scores);
      SpawnPlayerCardsSilent(10, response.player10Cards, response.player == 10 ? 1 : 0, response.scores);
      SpawnPlayerCardsSilent(11, response.player11Cards, response.player == 11 ? 1 : 0, response.scores);
    }

    // Queue the current card as a normal animated deal
    pendingDeals.Enqueue(new CardDealRequest
    {
      Player = response.player,
      CardData = response.card,
      Scores = response.scores
    });

    if (queuedDealRoutine == null)
      queuedDealRoutine = StartCoroutine(ProcessPendingDeals());
  }

  private void SpawnPlayerCardsSilent(int player, List<Card> cards, int skipLast, CardDealtScores scores)
  {
    if (cards == null || cardManager == null) return;
    int count = cards.Count - skipLast;
    for (int i = 0; i < count; i++)
    {
      if (cards[i] != null)
        cardManager.SpawnCardImmediate(player, cards[i], scores);
    }
  }

  internal void OnCardDealt(CardDealtEvent cardDealtEvent)
  {
    if (cardDealtEvent == null || cardDealtEvent.card == null)
    {
      return;
    }

    if (cardManager != null)
    {
      cardManager.SyncDealtCards(cardDealtEvent);
    }

    pendingDeals.Enqueue(new CardDealRequest
    {
      Player = cardDealtEvent.player,
      CardData = cardDealtEvent.card,
      Scores = cardDealtEvent.scores
    });

    if (queuedDealRoutine == null)
    {
      queuedDealRoutine = StartCoroutine(ProcessPendingDeals());
    }
  }

  internal void OnRoundEnd(int winner)
  {
    if (cardManager != null)
    {
      cardManager.OnRoundWin(winner);
    }
  }

  internal IEnumerator OnCashout()
  {
    pendingDeals.Clear();
    if (queuedDealRoutine != null)
    {
      StopCoroutine(queuedDealRoutine);
      queuedDealRoutine = null;
    }
    if (dealSequenceRoutine != null)
    {
      StopCoroutine(dealSequenceRoutine);
      dealSequenceRoutine = null;
    }

    yield return new WaitForSecondsRealtime(CardResetDelayOnCashout); 
    resetCardsAnimation();
    if (cardManager != null)
    {
      cardManager.BeginCashoutReset(CardDespawnStartDelay, CardDespawnMiddleGroupDelay, CardDisableToDestroyDelay);
    }
  }

  private IEnumerator ProcessPendingDeals()
  {
    while (pendingDeals.Count > 0)
    {
      CardDealRequest request = pendingDeals.Dequeue();
      PlayDealSegment(request.Player, request.CardData, request.Scores, null);
      yield return new WaitUntil(() => !isDealSegmentPlaying);
    }

    queuedDealRoutine = null;
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
    if (TopDownHandsParent_Object != null)
    {
      TopDownHandsParent_Object.SetActive(false);
    }
  }

  private bool TryGetSegmentIndexForPlayer(int player, out int spotIndex)
  {
    switch (player)
    {
      case 8:
        spotIndex = 0;
        return true;
      case 9:
        spotIndex = 1;
        return true;
      case 10:
        spotIndex = 2;
        return true;
      case 11:
        spotIndex = 3;
        return true;
      default:
        spotIndex = -1;
        return false;
    }
  }

  private int GetSpawnFrameForPlayer(int player)
  {
    switch (player)
    {
      case 8:
        return 29;
      case 9:
        return 91;
      case 10:
        return 150;
      case 11:
        return 212;
      default:
        return -1;
    }
  }

  void SpawnCard(int type, Card cardData, CardDealtScores scores)
  {
    if (cardManager != null)
    {
      cardManager.SpawnCard(type, cardData, scores);
    }
  }

  private void Update()
  {
    // if (Input.GetKeyDown(KeyCode.A))
    // {
    //   ShuffleCardsAnimation();
    // }
    // else if (Input.GetKeyDown(KeyCode.B))
    // {
    //   resetCardsAnimation();
    // }
    // else if (Input.GetKeyDown(KeyCode.C))
    // {
    //   if (dealSequenceRoutine != null)
    //   {
    //     StopCoroutine(dealSequenceRoutine);
    //   }
    //   dealSequenceRoutine = StartCoroutine(PlayAllDealSegments());
    // }
    // else if (Input.GetKeyDown(KeyCode.Alpha1))
    // {
    //   if (dealSequenceRoutine != null)
    //   {
    //     StopCoroutine(dealSequenceRoutine);
    //   }
    //   PlayDealSegment(0, null);
    // }
    // else if (Input.GetKeyDown(KeyCode.Alpha2))
    // {
    //   if (dealSequenceRoutine != null)
    //   {
    //     StopCoroutine(dealSequenceRoutine);
    //   }
    //   PlayDealSegment(1, null);
    // }
    // else if (Input.GetKeyDown(KeyCode.Alpha3))
    // {
    //   if (dealSequenceRoutine != null)
    //   {
    //     StopCoroutine(dealSequenceRoutine);
    //   }
    //   PlayDealSegment(2, null);
    // }
    // else if (Input.GetKeyDown(KeyCode.Alpha4))
    // {
    //   if (dealSequenceRoutine != null)
    //   {
    //     StopCoroutine(dealSequenceRoutine);
    //   }
    //   PlayDealSegment(3, null);
    // }
  }

}
