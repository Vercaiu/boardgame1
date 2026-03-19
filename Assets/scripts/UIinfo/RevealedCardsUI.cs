// RevealedCardsUI.cs
using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class RevealedCardsUI : MonoBehaviour
{
    public static RevealedCardsUI Instance;
    public Transform panel, panelright;
    public Transform leftflat, rightflat;
    public Transform leftcrevasse, rightcrevasse;
    public Transform leftslope1, rightslope1;
    public Transform leftslope2, rightslope2;
    public GameObject cardPrefab;
    public Sprite[] cardSprites;

    private bool _pendingRedraw = false;
    private List<GameObject> _spawnedCards = new List<GameObject>();

    void Awake() => Instance = this;

    void Start()
    {
        StartCoroutine(WaitForDeckManager());
    }

    IEnumerator WaitForDeckManager()
    {
        yield return new WaitUntil(() => DeckManager.Instance != null);
        DeckManager.Instance.RevealedCards.OnListChanged += OnRevealedChanged;
        Redraw(false);
    }

    void OnDestroy()
    {
        if (DeckManager.Instance != null)
            DeckManager.Instance.RevealedCards.OnListChanged -= OnRevealedChanged;
    }

    void OnRevealedChanged(NetworkListEvent<CardData> change)
    {
        if (change.Type == NetworkListEvent<CardData>.EventType.RemoveAt ||
            change.Type == NetworkListEvent<CardData>.EventType.Remove)
        {
            Redraw(false);
            return;
        }

        if (RoundManager.Instance != null &&
            RoundManager.Instance.GetCurrentPhase() == RoundManager.RoundPhase.TakingCard)
            return;

        if (!PanelFlipper.IsAnimating)
            Redraw(true);
        else
            _pendingRedraw = true;
    }

    public void Redraw(bool animate)
    {
        if (DeckManager.Instance == null) return;
        _pendingRedraw = false;

        bool showRight = PanelFlipper.ShowingRight;
        var revealedCards = DeckManager.Instance.RevealedCards;

        // Build a set of cardIds currently in the revealed list
        HashSet<int> revealedIds = new HashSet<int>();
        foreach (var card in revealedCards)
            revealedIds.Add(card.spriteId);

        // Remove spawned cards that are no longer in the revealed list
        for (int i = _spawnedCards.Count - 1; i >= 0; i--)
        {
            GameObject obj = _spawnedCards[i];
            if (obj == null) { _spawnedCards.RemoveAt(i); continue; }

            RevealedCardView view = obj.GetComponent<RevealedCardView>();
            if (view == null || !revealedIds.Contains(view.GetData().spriteId))
            {
                DestroyImmediate(obj);
                _spawnedCards.RemoveAt(i);
            }
        }

        // Build a set of spriteIds already spawned
        HashSet<int> spawnedIds = new HashSet<int>();
        foreach (GameObject obj in _spawnedCards)
        {
            if (obj == null) continue;
            RevealedCardView view = obj.GetComponent<RevealedCardView>();
            if (view != null) spawnedIds.Add(view.GetData().spriteId);
        }

        // Spawn only cards that aren't already present
        int index = _spawnedCards.Count;
        foreach (var card in revealedCards)
        {
            if (spawnedIds.Contains(card.spriteId)) continue; // already exists, skip

            Transform target = GetTargetTransform(card.cardId, showRight);
            if (target == null) continue;

            GameObject obj = Instantiate(cardPrefab, target, false);
            _spawnedCards.Add(obj);

            RevealedCardView view = obj.GetComponent<RevealedCardView>();
            view.Set(card, cardSprites[card.spriteId]);

            // Update button action
            obj.GetComponent<CardButtonHandler>()?.SetAction(CardButtonHandler.CardAction.ClaimFromBoard);

            if (animate)
                view.SlideIn(index, fromRight: showRight);

            index++;
        }
    }

    public void UpdateCardActions(CardButtonHandler.CardAction action)
    {
        foreach (GameObject obj in _spawnedCards)
        {
            if (obj == null) continue;
            obj.GetComponent<CardButtonHandler>()?.SetAction(action);
        }
    }

    public void RedrawIfPending()
    {
        if (_pendingRedraw) Redraw(true);
    }

    Transform GetTargetTransform(int id, bool right)
    {
        if (id == 0 || id == 8) return right ? rightflat : leftflat;
        if (id == 1 || id == 2) return right ? rightcrevasse : leftcrevasse;
        if (id == 3 || id == 5) return right ? rightslope1 : leftslope1;
        if (id == 4 || id == 6) return right ? rightslope2 : leftslope2;
        return null;
    }
}