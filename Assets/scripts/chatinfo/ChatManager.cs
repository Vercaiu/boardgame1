// ChatManager.cs
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class ChatManager : NetworkBehaviour
{
    public static ChatManager Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Instance = this;
        Debug.Log("ChatManager spawned on network");
    }

    public event System.Action<ChatMessage> OnMessageReceived;

    // ===============================
    // PLAYER MESSAGE
    // ===============================
    public void SendMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        SendMessageServerRpc(message);
    }

    [ServerRpc(RequireOwnership = false)]
    void SendMessageServerRpc(string message, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        PlayerStateNet player = FindPlayerState(senderId);
        FixedString64Bytes senderName = "Unknown";
        if (player != null)
            senderName = player.PlayerName.Value;
        ChatMessage chatMsg = new ChatMessage
        {
            senderId = senderId,
            senderName = senderName,
            message = message,
            isSystemMessage = false
        };
        BroadcastMessageClientRpc(chatMsg);
    }

    // ===============================
    // SYSTEM MESSAGE
    // ===============================
    public void SendSystemMessage(string message)
    {
        ChatMessage chatMsg = new ChatMessage
        {
            senderId = 0,
            senderName = "SYSTEM",
            message = message,
            isSystemMessage = true
        };
        if (IsServer)
            BroadcastMessageClientRpc(chatMsg);
        else
            SendSystemMessageServerRpc(message);
    }

    [ServerRpc(RequireOwnership = false)]
    void SendSystemMessageServerRpc(string message)
    {
        SendSystemMessage(message);
    }

    // ===============================
    // CLIENT BROADCAST
    // ===============================
    [ClientRpc]
    void BroadcastMessageClientRpc(ChatMessage message)
    {
        OnMessageReceived?.Invoke(message);
    }

    // ===============================
    // Helper
    // ===============================
    PlayerStateNet FindPlayerState(ulong clientId)
    {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            return null;
        return client.PlayerObject?.GetComponent<PlayerStateNet>();
    }
}