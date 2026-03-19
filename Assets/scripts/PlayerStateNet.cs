using Unity.Netcode;
using Unity.Collections;
using UnityEngine;

public class PlayerStateNet : NetworkBehaviour
{
    // ===============================
    // Card Containers
    // ===============================

    public NetworkList<CardData> Hand;
    public NetworkList<CardData> Field;

    public NetworkVariable<int> HandCount = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<ulong> ClientId = new NetworkVariable<ulong>();

    // ===============================
    // Player Name
    // ===============================

    public NetworkVariable<FixedString64Bytes> PlayerName =
        new NetworkVariable<FixedString64Bytes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    // ===============================
    // Awake
    // ===============================

    void Awake()
    {
        Hand = new NetworkList<CardData>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        Field = new NetworkList<CardData>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        Hand.OnListChanged += (change) =>
        {
            if (IsServer)
                HandCount.Value = Hand.Count;
        };
    }

    // ===============================
    // Network Spawn
    // ===============================

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            ClientId.Value = OwnerClientId;

        if (IsOwner)
        {
            string savedName = PlayerPrefs.GetString("PlayerName", "");
            if (string.IsNullOrWhiteSpace(savedName))
                savedName = "Player" + Random.Range(100, 999);
            SetPlayerNameServerRpc(savedName);
        }

        PlayerName.OnValueChanged += OnNameChanged;
        ChatManager.Instance.SendSystemMessage($"{GetPlayerName()} has joined");
    }

    private void OnDestroy()
    {
        PlayerName.OnValueChanged -= OnNameChanged;
    }

    // ===============================
    // Reset (called on rematch)
    // ===============================

    public void ResetForRematch()
    {
        if (!IsServer) return;
        Hand.Clear();
        Field.Clear();
        HandCount.Value = 0;
    }

    // ===============================
    // Server RPC for Setting Name
    // ===============================

    [ServerRpc]
    private void SetPlayerNameServerRpc(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            newName = "Player";
        if (newName.Length > 16)
            newName = newName.Substring(0, 16);
        PlayerName.Value = newName;
    }

    // ===============================
    // Name Changed Callback
    // ===============================

    private void OnNameChanged(FixedString64Bytes oldName, FixedString64Bytes newName)
    {
        Debug.Log($"Client {OwnerClientId} changed name to {newName}");
    }

    // ===============================
    // Helper
    // ===============================

    public string GetPlayerName()
    {
        if (PlayerName.Value.Length == 0)
            return $"Player {OwnerClientId}";
        return PlayerName.Value.ToString();
    }

    public static PlayerStateNet GetPlayer(ulong clientId)
    {
        if (NetworkManager.Singleton == null)
            return null;

        if (NetworkManager.Singleton.IsServer)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return null;
            return client.PlayerObject?.GetComponent<PlayerStateNet>();
        }

        foreach (var p in FindObjectsByType<PlayerStateNet>(FindObjectsSortMode.None))
            if (p.ClientId.Value == clientId)
                return p;

        return null;
    }
}