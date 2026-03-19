using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class DeckManager : NetworkBehaviour
{
    [Header("Card Database")]
    public CardDatabase cardDatabase;

    private readonly List<CardData> deck = new();
    public NetworkList<CardData> RevealedCards = new();
    public static DeckManager Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        BuildDeck();
        ShuffleDeck();
    }

    void BuildDeck()
    {
        deck.Clear();

        if (cardDatabase == null)
        {
            Debug.LogError("DeckManager: No CardDatabase assigned! Using fallback random cards.");
            BuildFallbackDeck();
            return;
        }

        // Get all cards from the database
        List<CardData> cardsFromDatabase = cardDatabase.GetDeckCardData();

        // Add all cards to deck
        foreach (var card in cardsFromDatabase)
        {
            deck.Add(card);
        }

        Debug.Log($"DeckManager: Built deck with {deck.Count} cards from database");
    }

    void BuildFallbackDeck()
    {
        // Keep the old random generation as fallback
        for (int i = 0; i < 40; i++)
        {
            deck.Add(new CardData
            {
                cardId = i,
                spriteId = i % 3,
                cardName = $"Card {i}",
                trees = Random.Range(1, 5),
                moose = Random.Range(1, 5),
                bats = Random.Range(1, 5),
                fire = Random.Range(1, 5),
            });
        }
    }

    void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            int j = Random.Range(i, deck.Count);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }
    }

    public void DealStartingHands(int cardsPerPlayer)
    {
        if (!IsServer) return;

        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            PlayerStateNet player = client.Value.PlayerObject?.GetComponent<PlayerStateNet>();
            if (player == null) continue;

            for (int i = 0; i < cardsPerPlayer; i++)
            {
                if (deck.Count == 0) break;
                player.Hand.Add(deck[0]);
                deck.RemoveAt(0);
            }
        }
    }



    private readonly List<CardData> discardPile = new();

    public void AddToDiscard(CardData card)
    {
        discardPile.Add(card);
    }

    public void ReplaceRevealedCards(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (deck.Count == 0) break;
            RevealedCards.Add(deck[0]);
            deck.RemoveAt(0);
        }
    }

    public CardData DrawTopCard()
    {
        if (deck.Count == 0) return new CardData { cardId = -1 };
        CardData card = deck[0];
        deck.RemoveAt(0);
        return card;
    }


    // ... rest of your existing methods (RevealCardsServerRpc, etc.) stay the same

    [ServerRpc(RequireOwnership = false)]
    public void RevealCardsServerRpc(int amount)
    {
        ChatManager.Instance.SendSystemMessage(deck.Count + "");
        if (deck.Count < amount) return;

        RevealedCards.Clear();

        for (int i = 0; i < amount; i++)
        {
            RevealedCards.Add(deck[0]);
            deck.RemoveAt(0);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ClaimRevealedCardServerRpc(
        int spriteID,
        ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        // Use sender's ID instead of IsMyTurn()
        if (!TurnManager.Instance.IsClientsTurn(clientId))
        {
            ChatManager.Instance.SendSystemMessage($"Client {clientId} tried to take a card but it's not their turn!");
            return;
        }

        // Check phase — must be in TakingCard phase
        if (RoundManager.Instance.GetCurrentPhase() != RoundManager.RoundPhase.TakingCard)
        {
            ChatManager.Instance.SendSystemMessage($"Client {clientId} tried to take a card outside of TakingCard phase!");
            return;
        }

        int index = -1;
        for (int i = 0; i < RevealedCards.Count; i++)
        {
            if (RevealedCards[i].spriteId == spriteID) { index = i; break; }
        }

        if (index == -1)
        {
            Debug.LogError($"[Server] Card {spriteID} not found in RevealedCards!");
            return;
        }

        CardData chosen = RevealedCards[index];
        RevealedCards.RemoveAt(index);

        PlayerStateNet player = FindPlayerState(clientId);
        if (player == null)
        {
            Debug.LogError($"[Server] PlayerStateNet not found for client {clientId}!");
            return;
        }

        player.Hand.Add(chosen);
        Debug.Log($"[Server] Card added to hand. New hand size: {player.Hand.Count}");

        // Notify RoundManager so it can advance phase to PlacingCard
        RoundManager.Instance?.NotifyCardTaken(clientId, chosen.cardId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlaceCardInFieldServerRpc(
        int spriteID,
        ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (!TurnManager.Instance.IsClientsTurn(clientId)) return;
        if (RoundManager.Instance.GetCurrentPhase() != RoundManager.RoundPhase.PlacingCard) return;

        PlayerStateNet player = FindPlayerState(clientId);
        if (player == null) return;

        // Block if already at 2 or fewer cards — nothing more to place
        if (player.Hand.Count <= 2)
        {
            ChatManager.Instance.SendSystemMessage($"Client {clientId} already has 2 or fewer cards, can't place more!");
            return;
        }

        int index = -1;
        for (int i = 0; i < player.Hand.Count; i++)
        {
            if (player.Hand[i].spriteId == spriteID) { index = i; break; }
        }
        if (index == -1) return;

        CardData played = player.Hand[index];
        player.Hand.RemoveAt(index);
        player.Field.Add(played);

        ChatManager.Instance.SendSystemMessage($"Client {clientId} placed card {spriteID} in field");

        RoundManager.Instance?.NotifyCardPlaced(clientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SwapHandAndFieldCardServerRpc(
        int handCardId,
        int fieldCardId,
        ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        PlayerStateNet player = FindPlayerState(clientId);

        if (player == null) return;

        int handIndex = -1;
        for (int i = 0; i < player.Hand.Count; i++)
        {
            if (player.Hand[i].spriteId == handCardId)
            {
                handIndex = i;
                break;
            }
        }

        int fieldIndex = -1;
        for (int i = 0; i < player.Field.Count; i++)
        {
            if (player.Field[i].spriteId == fieldCardId)
            {
                fieldIndex = i;
                break;
            }
        }

        if (handIndex == -1 || fieldIndex == -1) return;

        CardData handCard = player.Hand[handIndex];
        CardData fieldCard = player.Field[fieldIndex];

        player.Hand.RemoveAt(handIndex);
        player.Hand.Add(fieldCard);

        player.Field[fieldIndex] = handCard;

        Debug.Log($"Swapped hand card {handCardId} with field card {fieldCardId} for client {clientId}");

        // NO ClientRpc needed - NetworkList sync handles it
    }

    PlayerStateNet FindPlayerState(ulong clientId)
    {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            Debug.Log("does it go here");
            return null;
        }
        if (client.PlayerObject == null)
        {
            Debug.Log("or does it go here");
            return null;
        }
        return client.PlayerObject.GetComponent<PlayerStateNet>();
    }
}