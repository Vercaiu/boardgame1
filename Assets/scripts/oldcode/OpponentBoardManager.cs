using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class OpponentBoardManager : MonoBehaviour
{
    public static OpponentBoardManager Instance;
    public Transform opponentPanel;
    public GameObject opponentCardPrefab;
    public Sprite cardBackSprite;
    public Sprite[] cardSprites;

    private Dictionary<ulong, PlayerStateNet> allPlayers = new();
    private PlayerStateNet currentViewedPlayer;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Wait for network to be ready
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            RefreshPlayerList();
        }

        // Check periodically for new players
        InvokeRepeating(nameof(RefreshPlayerList), 1f, 2f);
    }

    void RefreshPlayerList()
    {
        if (NetworkManager.Singleton == null) return;
        if (!NetworkManager.Singleton.IsConnectedClient) return;

        // Find all PlayerStateNet objects in the scene
        PlayerStateNet[] allPlayerStates = FindObjectsByType<PlayerStateNet>(FindObjectsSortMode.None);

        Debug.Log($"OpponentBoardManager: Found {allPlayerStates.Length} player states");

        foreach (var player in allPlayerStates)
        {
            // Make sure the NetworkObject is spawned
            if (!player.IsSpawned)
            {
                Debug.Log($"OpponentBoardManager: Player not yet spawned, skipping");
                continue;
            }

            ulong clientId = player.ClientId.Value;

            // Already registered?
            if (allPlayers.ContainsKey(clientId))
            {
                continue;
            }

            // Register new player
            allPlayers[clientId] = player;

            // Capture the player variable for the lambda
            PlayerStateNet capturedPlayer = player;

            // Listen to their field changes
            player.Field.OnListChanged += (change) =>
            {
                Debug.Log($"OpponentBoardManager: Field changed for player {capturedPlayer.ClientId.Value}");
                if (currentViewedPlayer == capturedPlayer)
                {
                    RedrawOpponentBoard();
                }
            };

            // Listen to hand count changes
            player.HandCount.OnValueChanged += (oldValue, newValue) =>
            {
                Debug.Log($"OpponentBoardManager: Hand count changed for player {capturedPlayer.ClientId.Value}: {oldValue} -> {newValue}");
                if (currentViewedPlayer == capturedPlayer)
                {
                    RedrawOpponentBoard();
                }
            };

            Debug.Log($"OpponentBoardManager: Registered player {clientId} with {player.Hand.Count} cards in hand and {player.Field.Count} cards on field");
        }
    }

    // Call this when player clicks "View Player X's Board" button
    public void ViewPlayerBoard(ulong clientId)
    {
        if (!allPlayers.TryGetValue(clientId, out PlayerStateNet player))
        {
            Debug.LogWarning($"OpponentBoardManager: Player {clientId} not found!");
            return;
        }

        currentViewedPlayer = player;
        RedrawOpponentBoard();
    }

    void RedrawOpponentBoard()
    {
        // Clear existing cards
        foreach (Transform child in opponentPanel)
        {
            Destroy(child.gameObject);
        }

        if (currentViewedPlayer == null) return;

        Debug.Log($"OpponentBoardManager: Redrawing board for player {currentViewedPlayer.ClientId.Value}. Hand: {currentViewedPlayer.HandCount.Value}, Field: {currentViewedPlayer.Field.Count}");

        // Show hand as card backs
        for (int i = 0; i < currentViewedPlayer.HandCount.Value; i++)
        {
            GameObject obj = Instantiate(opponentCardPrefab, opponentPanel);
            var view = obj.GetComponent<OpponentCardView>();
            view.ShowCardBack(cardBackSprite);
        }

        // Show field cards (actual cards)
        foreach (var card in currentViewedPlayer.Field)
        {
            GameObject obj = Instantiate(opponentCardPrefab, opponentPanel);
            var view = obj.GetComponent<OpponentCardView>();
            view.ShowCard(card, cardSprites[card.spriteId]);
        }
    }

    // Helper: Get list of all opponent client IDs (excluding local player)
    public List<ulong> GetOpponentClientIds()
    {
        List<ulong> opponents = new();
        ulong localClientId = NetworkManager.Singleton.LocalClientId;

        foreach (var clientId in allPlayers.Keys)
        {
            if (clientId != localClientId)
            {
                opponents.Add(clientId);
            }
        }

        return opponents;
    }
}