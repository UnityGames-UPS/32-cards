using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardController : MonoBehaviour
{
  [System.Serializable]
  private class CardSpriteEntry
  {
    public string Suit;
    public string Rank;
    public Sprite Sprite;
  }

  private class SpawnedCardPair
  {
    public GameObject DealerCard;
    public GameObject TopDownCard;
  }

  [Header("Flip Settings")]
  [SerializeField] private float CardFlipDurationSeconds = 0.5f;
  [SerializeField] private float WaitForFlipSeconds = 0.5f;
  [SerializeField, Range(0f, 1f)] private float RevealAtFlipPercent = 0.5f;
  [SerializeField, Range(0f, 1f)] private float TopDownRevealAtFlipPercent = 0.5f;
  [SerializeField, Range(-1f, 1f)] private float FlipDirection = 1f;
  [Header("Card Faces")]
  [SerializeField] private List<CardSpriteEntry> CardSprites = new List<CardSpriteEntry>();
  [SerializeField] private Transform SpawnPoint_Transform;
  [SerializeField] private Transform Card8Pos_Transform;
  [SerializeField] private Transform Card9Pos_Transform;
  [SerializeField] private Transform Card10Pos_Transform;
  [SerializeField] private Transform Card11Pos_Transform;
  [SerializeField] private GameObject Card8_Object;
  [SerializeField] private GameObject Card9_Object;
  [SerializeField] private GameObject Card10_Object;
  [SerializeField] private GameObject Card11_Object;
  [Header("Top Down Cards")]
  [SerializeField] private GameObject TopDownCardPrefab;
  [SerializeField] private Transform TopDownCard1_Transform;
  [SerializeField] private Transform TopDownCard2_Transform;
  [SerializeField] private Transform TopDownCard3_Transform;
  [SerializeField] private Transform TopDownCard4_Transform;
  private readonly Dictionary<int, List<Card>> dealtCardsByPlayer = new Dictionary<int, List<Card>>();
  private readonly Dictionary<int, List<SpawnedCardPair>> spawnedCardsByPlayer = new Dictionary<int, List<SpawnedCardPair>>();
  private Coroutine cashoutResetRoutine;

  private void Awake()
  {
    InitializePlayerCollections();
  }

  internal void SpawnCard(int type, Card cardData)
  {
    InitializePlayerCollections();

    Sprite cardSprite = ResolveCardSprite(cardData);
    GameObject topDownCard = SpawnTopDownCard(type, cardSprite);
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
    StartCoroutine(FlipAndReveal(cardref, cardSprite));
    TrackSpawnedCards(type, cardref, topDownCard);
  }

  internal void SyncDealtCards(CardDealtEvent cardDealtEvent)
  {
    if (cardDealtEvent == null)
    {
      return;
    }

    InitializePlayerCollections();
    ReplacePlayerCards(8, cardDealtEvent.player8Cards);
    ReplacePlayerCards(9, cardDealtEvent.player9Cards);
    ReplacePlayerCards(10, cardDealtEvent.player10Cards);
    ReplacePlayerCards(11, cardDealtEvent.player11Cards);
  }

  internal void BeginCashoutReset(float startDelay, float middleGroupDelay, float disableToDestroyDelay)
  {
    if (cashoutResetRoutine != null)
    {
      StopCoroutine(cashoutResetRoutine);
    }

    cashoutResetRoutine = StartCoroutine(ResetCardsForCashout(startDelay, middleGroupDelay, disableToDestroyDelay));
  }

  internal void ClearAllCardsImmediate()
  {
    if (cashoutResetRoutine != null)
    {
      StopCoroutine(cashoutResetRoutine);
      cashoutResetRoutine = null;
    }

    foreach (List<SpawnedCardPair> spawnedList in spawnedCardsByPlayer.Values)
    {
      for (int i = 0; i < spawnedList.Count; i++)
      {
        DestroyPair(spawnedList[i]);
      }
      spawnedList.Clear();
    }

    foreach (List<Card> dealtCards in dealtCardsByPlayer.Values)
    {
      dealtCards.Clear();
    }
  }

  internal GameObject SpawnTopDownCard(int spotIndex, Sprite cardSprite)
  {
    if (TopDownCardPrefab == null) return null;

    Transform parent = null;
    switch (spotIndex)
    {
      case 8: parent = TopDownCard1_Transform; break;
      case 9: parent = TopDownCard2_Transform; break;
      case 10: parent = TopDownCard3_Transform; break;
      case 11: parent = TopDownCard4_Transform; break;
    }
    if (parent == null) return null;

    GameObject cardref = GameObject.Instantiate(TopDownCardPrefab, parent, false);
    cardref.transform.localPosition = Vector3.zero;
    cardref.transform.localRotation = Quaternion.identity;
    cardref.transform.localScale = Vector3.one;
    cardref.transform.localEulerAngles = new Vector3(0f, 0f, 180f);
    StartCoroutine(FlipTopDownCard(cardref, cardSprite));
    return cardref;
  }

  private IEnumerator FlipAndReveal(GameObject cardref, Sprite cardSprite)
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
      if (!revealed && t >= revealPercent) { SetCardSprite(cardImage, trapezoid, cardSprite); revealed = true; }
      yield return null;
    }
    if (!revealed) SetCardSprite(cardImage, trapezoid, cardSprite);
  }

  private void SetCardSprite(Image cardImage, UITrapezoidTopNarrow trapezoid, Sprite cardSprite)
  {
    if (cardImage == null)
    {
      return;
    }
    if (cardSprite == null)
    {
      return;
    }
    cardImage.sprite = cardSprite;
  }

  private IEnumerator FlipTopDownCard(GameObject cardref, Sprite cardSprite)
  {
    if (cardref == null) yield break;

    Image cardImage = cardref.GetComponent<Image>();
    if (cardImage == null) cardImage = cardref.GetComponentInChildren<Image>();

    float wait = Mathf.Max(0f, WaitForFlipSeconds);
    if (wait > 0f) yield return new WaitForSeconds(wait);

    bool revealed = false;
    float duration = Mathf.Max(0.01f, CardFlipDurationSeconds);
    float elapsed = 0f;
    float revealPercent = Mathf.Clamp01(TopDownRevealAtFlipPercent);
    while (elapsed < duration)
    {
      elapsed += Time.deltaTime;
      float t = Mathf.Clamp01(elapsed / duration);
      float angle = Mathf.Lerp(0f, 180f, t);
      if (angle < 90f)
      {
        cardref.transform.localEulerAngles = new Vector3(angle, 0f, 180f);
      }
      else
      {
        cardref.transform.localEulerAngles = new Vector3(180f - angle, 0f, 0f);
      }
      if (!revealed && t >= revealPercent) { SetCardSprite(cardImage, null, cardSprite); revealed = true; }
      yield return null;
    }
    if (!revealed) SetCardSprite(cardImage, null, cardSprite);
    cardref.transform.localEulerAngles = Vector3.zero;
  }

  private void InitializePlayerCollections()
  {
    EnsurePlayerCollection(8);
    EnsurePlayerCollection(9);
    EnsurePlayerCollection(10);
    EnsurePlayerCollection(11);
  }

  private void EnsurePlayerCollection(int player)
  {
    if (!dealtCardsByPlayer.ContainsKey(player))
    {
      dealtCardsByPlayer[player] = new List<Card>();
    }
    if (!spawnedCardsByPlayer.ContainsKey(player))
    {
      spawnedCardsByPlayer[player] = new List<SpawnedCardPair>();
    }
  }

  private void ReplacePlayerCards(int player, List<Card> cards)
  {
    if (!dealtCardsByPlayer.ContainsKey(player))
    {
      return;
    }

    dealtCardsByPlayer[player].Clear();
    if (cards == null)
    {
      return;
    }

    for (int i = 0; i < cards.Count; i++)
    {
      if (cards[i] != null)
      {
        dealtCardsByPlayer[player].Add(cards[i]);
      }
    }
  }

  private void TrackSpawnedCards(int player, GameObject dealerCard, GameObject topDownCard)
  {
    if (!spawnedCardsByPlayer.ContainsKey(player))
    {
      return;
    }

    spawnedCardsByPlayer[player].Add(new SpawnedCardPair
    {
      DealerCard = dealerCard,
      TopDownCard = topDownCard
    });
  }

  private Sprite ResolveCardSprite(Card cardData)
  {
    if (cardData == null)
    {
      return null;
    }

    for (int i = 0; i < CardSprites.Count; i++)
    {
      CardSpriteEntry entry = CardSprites[i];
      if (entry == null || entry.Sprite == null)
      {
        continue;
      }

      if (string.Equals(entry.Suit, cardData.suit, System.StringComparison.OrdinalIgnoreCase)
        && string.Equals(entry.Rank, cardData.rank, System.StringComparison.OrdinalIgnoreCase))
      {
        return entry.Sprite;
      }
    }

    Debug.LogWarning($"CardController could not resolve sprite for suit '{cardData.suit}' and rank '{cardData.rank}'.");
    return null;
  }

  private IEnumerator ResetCardsForCashout(float startDelay, float middleGroupDelay, float disableToDestroyDelay)
  {
    if (startDelay > 0f)
    {
      yield return new WaitForSeconds(startDelay);
    }

    DisablePlayerCards(8);
    DisablePlayerCards(11);

    if (disableToDestroyDelay > 0f)
    {
      yield return new WaitForSeconds(disableToDestroyDelay);
    }

    DestroyPlayerCards(8);
    DestroyPlayerCards(11);

    if (middleGroupDelay > 0f)
    {
      yield return new WaitForSeconds(middleGroupDelay);
    }

    DisablePlayerCards(9);
    DisablePlayerCards(10);

    if (disableToDestroyDelay > 0f)
    {
      yield return new WaitForSeconds(disableToDestroyDelay);
    }

    DestroyPlayerCards(9);
    DestroyPlayerCards(10);
    cashoutResetRoutine = null;
  }

  private void DisablePlayerCards(int player)
  {
    if (!spawnedCardsByPlayer.ContainsKey(player))
    {
      return;
    }

    List<SpawnedCardPair> spawnedCards = spawnedCardsByPlayer[player];
    for (int i = 0; i < spawnedCards.Count; i++)
    {
      SetPairActive(spawnedCards[i], false);
    }
  }

  private void DestroyPlayerCards(int player)
  {
    if (!spawnedCardsByPlayer.ContainsKey(player))
    {
      return;
    }

    List<SpawnedCardPair> spawnedCards = spawnedCardsByPlayer[player];
    for (int i = 0; i < spawnedCards.Count; i++)
    {
      DestroyPair(spawnedCards[i]);
    }
    spawnedCards.Clear();

    if (dealtCardsByPlayer.ContainsKey(player))
    {
      dealtCardsByPlayer[player].Clear();
    }
  }

  private void SetPairActive(SpawnedCardPair spawnedCardPair, bool isActive)
  {
    if (spawnedCardPair == null)
    {
      return;
    }

    if (spawnedCardPair.DealerCard != null)
    {
      spawnedCardPair.DealerCard.SetActive(isActive);
    }
    if (spawnedCardPair.TopDownCard != null)
    {
      spawnedCardPair.TopDownCard.SetActive(isActive);
    }
  }

  private void DestroyPair(SpawnedCardPair spawnedCardPair)
  {
    if (spawnedCardPair == null)
    {
      return;
    }

    if (spawnedCardPair.DealerCard != null)
    {
      Destroy(spawnedCardPair.DealerCard);
    }
    if (spawnedCardPair.TopDownCard != null)
    {
      Destroy(spawnedCardPair.TopDownCard);
    }
  }
}
