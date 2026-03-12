using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

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
  [Header("Score Texts")]
  [SerializeField] private TMP_Text ScoreText_P8;
  [SerializeField] private TMP_Text ScoreText_P9;
  [SerializeField] private TMP_Text ScoreText_P10;
  [SerializeField] private TMP_Text ScoreText_P11;
  [SerializeField] private float ScoreFadeInDuration = 0.3f;
  [SerializeField] private float ScoreFadeOutDuration = 0.4f;
  [SerializeField] private float ScoreWinScalePeak = 1.4f;
  [SerializeField] private float ScoreWinScaleDuration = 0.5f;
  private readonly Dictionary<int, List<Card>> dealtCardsByPlayer = new Dictionary<int, List<Card>>();
  private readonly Dictionary<int, List<SpawnedCardPair>> spawnedCardsByPlayer = new Dictionary<int, List<SpawnedCardPair>>();
  private Coroutine cashoutResetRoutine;
  private Tween scoreWinTween;
  private readonly Dictionary<int, Tween> scoreFadeTweens = new Dictionary<int, Tween>();

  private void Awake()
  {
    InitializePlayerCollections();
    SetScoreTextAlpha(ScoreText_P8, 0f);
    SetScoreTextAlpha(ScoreText_P9, 0f);
    SetScoreTextAlpha(ScoreText_P10, 0f);
    SetScoreTextAlpha(ScoreText_P11, 0f);
  }

  internal void SpawnCard(int type, Card cardData, CardDealtScores scores)
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

    int scoreForPlayer = GetScoreForPlayer(type, scores);

    GameObject cardref = GameObject.Instantiate(prefab, parent, false);
    cardref.transform.localPosition = Vector3.zero;
    cardref.transform.localRotation = Quaternion.identity;
    cardref.transform.localScale = Vector3.one;

    cardref.transform.localEulerAngles = new Vector3(0f, 0f, 180f);
    StartCoroutine(FlipAndReveal(cardref, cardSprite, type, scoreForPlayer));
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

  private IEnumerator FlipAndReveal(GameObject cardref, Sprite cardSprite, int player = -1, int score = 0)
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

    if (player >= 8 && player <= 11)
    {
      TMP_Text scoreText = GetScoreText(player);
      if (scoreText != null)
      {
        scoreText.text = score.ToString();
        FadeInScoreText(player, scoreText);
      }
    }
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

    float duration = Mathf.Max(0.01f, CardFlipDurationSeconds);
    float half = duration * 0.5f;
    float revealTime = duration * Mathf.Clamp01(TopDownRevealAtFlipPercent);

    // Phase 1: (0,0,180)→(90,0,180), snap z to 0 at midpoint, Phase 2: (90,0,0)→(0,0,0)
    Sequence seq = DOTween.Sequence();
    seq.Append(cardref.transform.DOLocalRotate(new Vector3(90f, 0f, 180f), half).SetEase(Ease.Linear));
    seq.AppendCallback(() => cardref.transform.localEulerAngles = new Vector3(90f, 0f, 0f));
    seq.Append(cardref.transform.DOLocalRotate(Vector3.zero, half).SetEase(Ease.Linear));
    seq.InsertCallback(revealTime, () => SetCardSprite(cardImage, null, cardSprite));
    yield return seq.WaitForCompletion();
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

  private TMP_Text GetScoreText(int player)
  {
    switch (player)
    {
      case 8: return ScoreText_P8;
      case 9: return ScoreText_P9;
      case 10: return ScoreText_P10;
      case 11: return ScoreText_P11;
      default: return null;
    }
  }

  private int GetScoreForPlayer(int player, CardDealtScores scores)
  {
    if (scores == null) return 0;
    switch (player)
    {
      case 8: return scores.player_8;
      case 9: return scores.player_9;
      case 10: return scores.player_10;
      case 11: return scores.player_11;
      default: return 0;
    }
  }

  private void SetScoreTextAlpha(TMP_Text text, float alpha)
  {
    if (text == null) return;
    Color c = text.color;
    c.a = alpha;
    text.color = c;
  }

  private void FadeInScoreText(int player, TMP_Text scoreText)
  {
    if (scoreFadeTweens.TryGetValue(player, out Tween existing)) existing?.Kill();
    scoreFadeTweens[player] = scoreText.DOFade(1f, ScoreFadeInDuration).SetEase(Ease.Linear);
  }

  internal void FadeOutScoreTexts()
  {
    FadeOutScoreText(8, ScoreText_P8);
    FadeOutScoreText(9, ScoreText_P9);
    FadeOutScoreText(10, ScoreText_P10);
    FadeOutScoreText(11, ScoreText_P11);
  }

  private void FadeOutScoreText(int player, TMP_Text scoreText)
  {
    if (scoreText == null) return;
    if (scoreFadeTweens.TryGetValue(player, out Tween existing)) existing?.Kill();
    scoreFadeTweens[player] = scoreText.DOFade(0f, ScoreFadeOutDuration).SetEase(Ease.Linear);
  }

  internal void OnRoundWin(int winner)
  {
    TMP_Text winText = GetScoreText(winner);
    if (winText == null) return;
    scoreWinTween?.Kill();
    float half = Mathf.Max(0.01f, ScoreWinScaleDuration) * 0.5f;
    scoreWinTween = DOTween.Sequence()
      .Append(winText.transform.DOScale(ScoreWinScalePeak, half).SetEase(Ease.OutElastic))
      .Append(winText.transform.DOScale(1f, half).SetEase(Ease.InQuad));
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
