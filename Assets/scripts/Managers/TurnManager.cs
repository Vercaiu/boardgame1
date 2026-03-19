using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class TurnManager : NetworkBehaviour
{
    public static TurnManager Instance;

    [Header("Turn State")]
    private NetworkVariable<int> currentTurnIndex = new NetworkVariable<int>(0);
    private NetworkVariable<bool> turnSystemActive = new NetworkVariable<bool>(false);

    private List<ulong> turnOrder = new List<ulong>();

    public event System.Action<ulong, int> OnTurnChanged; // (clientId, turnIndex)
    public event System.Action<ulong> OnTurnStarted; // Fires when a player's turn begins
    public event System.Action<ulong> OnTurnEnded; // Fires when a player's turn ends

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
        base.OnNetworkSpawn();

        // Listen for turn changes
        currentTurnIndex.OnValueChanged += OnTurnIndexChanged;
        turnSystemActive.OnValueChanged += OnTurnSystemActiveChanged;

        // Get initial turn order from GameStartManager if game already started
        if (GameStartManager.Instance != null)
        {
            GameStartManager.Instance.OnTurnOrderSet += OnTurnOrderReceived;

            // If turn order is already set, grab it
            List<ulong> existingOrder = GameStartManager.Instance.GetTurnOrder();
            if (existingOrder.Count > 0)
            {
                OnTurnOrderReceived(existingOrder);
            }
        }
    }

    void OnDestroy()
    {
        if (GameStartManager.Instance != null)
        {
            GameStartManager.Instance.OnTurnOrderSet -= OnTurnOrderReceived;
        }
    }

    /// <summary>
    /// Called by GameStartManager when turn order is set
    /// </summary>
    /// <summary>
    /// Called by GameStartManager when turn order is set
    /// </summary>
    void OnTurnOrderReceived(List<ulong> order)
    {
        // Check if order is valid
        if (order == null || order.Count == 0)
        {
            Debug.LogWarning("TurnManager: Received empty turn order!");
            return;
        }

        turnOrder = new List<ulong>(order);
        Debug.Log($"TurnManager: Received turn order with {turnOrder.Count} players");

        if (IsServer)
        {
            // Activate turn system
            turnSystemActive.Value = true;
            currentTurnIndex.Value = 0;

            Debug.Log($"TurnManager: Turn system activated. Client {turnOrder[0]}'s turn!");
        }
    }

    void OnTurnIndexChanged(int oldIndex, int newIndex)
    {
        if (turnOrder.Count == 0) return;

        ulong oldPlayer = (oldIndex >= 0 && oldIndex < turnOrder.Count) ? turnOrder[oldIndex] : 0;
        ulong newPlayer = turnOrder[newIndex];

        Debug.Log($"TurnManager: Turn changed from Client {oldPlayer} (index {oldIndex}) to Client {newPlayer} (index {newIndex})");

        // Fire events
        if (oldPlayer != 0)
        {
            OnTurnEnded?.Invoke(oldPlayer);
        }

        OnTurnStarted?.Invoke(newPlayer);
        OnTurnChanged?.Invoke(newPlayer, newIndex);

        // Send chat message
        if (ChatManager.Instance != null)
        {
            ChatManager.Instance.SendSystemMessage($"Client {newPlayer}'s turn!");
        }
    }

    void OnTurnSystemActiveChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"TurnManager: Turn system active = {newValue}");
    }

    // ============= PUBLIC API =============

    /// <summary>
    /// Call this from UI buttons - works for both host and clients
    /// </summary>
    public void EndTurn()
    {
        if (IsServer)
        {
            // Host can call directly
            RoundManager.Instance?.OnPlayerEndedTurn(NetworkManager.Singleton.LocalClientId);
        }
        else
        {
            // Clients request server to advance
            RequestNextTurnServerRpc();
        }
    }

    /// <summary>
    /// Advance to the next player's turn (server only)
    /// </summary>
    public void NextTurn()
    {
        if (!IsServer)
        {
            Debug.LogWarning("TurnManager: Only server can advance turns!");
            return;
        }

        if (!turnSystemActive.Value)
        {
            Debug.LogWarning("TurnManager: Turn system is not active yet!");
            return;
        }

        if (turnOrder.Count == 0)
        {
            Debug.LogWarning("TurnManager: No players in turn order!");
            return;
        }

        int oldIndex = currentTurnIndex.Value;
        currentTurnIndex.Value = (currentTurnIndex.Value + 1) % turnOrder.Count;

        ChatManager.Instance.SendSystemMessage($"TurnManager: Advanced from turn {oldIndex} to {currentTurnIndex.Value}");
    }

    /// <summary>
    /// Request the server to advance to next turn (any client can call)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestNextTurnServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        // Optional: Validate it's the current player's turn
        if (GetCurrentTurnPlayer() != senderId)
        {
            ChatManager.Instance.SendSystemMessage($"TurnManager: Client {senderId} tried to end turn, but it's Client {GetCurrentTurnPlayer()}'s turn!");
            return;
        }

        Debug.Log($"TurnManager: Client {senderId} requested to end their turn");
        RoundManager.Instance?.OnPlayerEndedTurn(senderId);
    }

    /// <summary>
    /// Get the client ID of the current player whose turn it is
    /// </summary>
    public ulong GetCurrentTurnPlayer()
    {
        if (turnOrder.Count == 0 || currentTurnIndex.Value < 0 || currentTurnIndex.Value >= turnOrder.Count)
        {
            return 0;
        }
        return turnOrder[currentTurnIndex.Value];
    }

    /// <summary>
    /// Get the current turn index (0-based)
    /// </summary>
    public int GetCurrentTurnIndex()
    {
        return currentTurnIndex.Value;
    }

    /// <summary>
    /// Check if it's a specific client's turn
    /// </summary>
    public bool IsClientsTurn(ulong clientId)
    {
        return GetCurrentTurnPlayer() == clientId;
    }

    /// <summary>
    /// Check if it's the local player's turn
    /// </summary>
    public bool IsMyTurn()
    {
        if (NetworkManager.Singleton == null) return false;
        return IsClientsTurn(NetworkManager.Singleton.LocalClientId);
    }

    /// <summary>
    /// Get player at specific turn index
    /// </summary>
    public ulong GetPlayerAtTurnIndex(int index)
    {
        if (index < 0 || index >= turnOrder.Count)
        {
            Debug.LogWarning($"TurnManager: Invalid turn index: {index}");
            return 0;
        }
        return turnOrder[index];
    }

    /// <summary>
    /// Get the full turn order
    /// </summary>
    public List<ulong> GetTurnOrder()
    {
        return new List<ulong>(turnOrder);
    }

    /// <summary>
    /// Get total number of players
    /// </summary>
    public int GetPlayerCount()
    {
        return turnOrder.Count;
    }

    /// <summary>
    /// Check if turn system is active (game has started)
    /// </summary>
    public bool IsTurnSystemActive()
    {
        return turnSystemActive.Value;
    }

    /// <summary>
    /// Get the next player in turn order (without advancing)
    /// </summary>
    public ulong GetNextPlayer()
    {
        if (turnOrder.Count == 0) return 0;

        int nextIndex = (currentTurnIndex.Value + 1) % turnOrder.Count;
        return turnOrder[nextIndex];
    }

    /// <summary>
    /// Get the previous player in turn order
    /// </summary>
    public ulong GetPreviousPlayer()
    {
        if (turnOrder.Count == 0) return 0;

        int prevIndex = currentTurnIndex.Value - 1;
        if (prevIndex < 0) prevIndex = turnOrder.Count - 1;

        return turnOrder[prevIndex];
    }
}