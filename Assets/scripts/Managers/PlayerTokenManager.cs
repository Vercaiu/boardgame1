// PlayerTokenManager.cs
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PlayerTokenManager : NetworkBehaviour
{
    public static PlayerTokenManager Instance;

    [Header("Token Settings")]
    public GameObject[] tokenPrefabs; // 4 prefabs, index matches starting field card slot (0-3)
    public float tokenStackOffset = 0.3f;

    [Header("Field Spot Markers (4 each, matching card groups)")]
    public Transform[] leftSpotMarkers;
    public Transform[] rightSpotMarkers;

    private static readonly Dictionary<int, int> cardIdToSpot = new Dictionary<int, int>
    {
        { 74, 0 }, { 75, 1 }, { 76, 2 }, { 77, 3 },
        { 0, 0 },
        { 1, 1 }, { 2, 1 },
        { 3, 2 }, { 5, 2 },
        { 4, 3 }, { 6, 3 },
    };

    // Server-side tracking
    private Dictionary<ulong, GameObject> spawnedTokens = new Dictionary<ulong, GameObject>();
    private Dictionary<ulong, int> playerTokenIndex = new Dictionary<ulong, int>();

    // Client-side tracking
    private Dictionary<ulong, GameObject> clientTokens = new Dictionary<ulong, GameObject>();

    void Awake() => Instance = this;

    public void PlaceStartingToken(ulong clientId, int cardId, int slotIndex)
    {
        if (!IsServer) return;

        playerTokenIndex[clientId] = slotIndex;

        if (!cardIdToSpot.TryGetValue(cardId, out int spotIndex))
        {
            Debug.LogWarning($"[TokenManager] No spot mapping for cardId {cardId}");
            return;
        }

        SpawnTokenAtSpot(clientId, spotIndex);
    }

    public void MoveTokenToSpot(ulong clientId, int cardId)
    {
        if (!IsServer) return;

        if (!cardIdToSpot.TryGetValue(cardId, out int spotIndex))
        {
            Debug.LogWarning($"[TokenManager] No spot mapping for cardId {cardId}");
            return;
        }

        // Destroy server copy
        if (spawnedTokens.TryGetValue(clientId, out GameObject existing) && existing != null)
        {
            Destroy(existing);
            spawnedTokens.Remove(clientId);
        }

        // Tell clients to destroy their copy
        DestroyTokenClientRpc(clientId);

        SpawnTokenAtSpot(clientId, spotIndex);
    }

    private void SpawnTokenAtSpot(ulong clientId, int spotIndex)
    {
        if (!playerTokenIndex.TryGetValue(clientId, out int prefabIndex))
        {
            Debug.LogWarning($"[TokenManager] No token prefab registered for client {clientId}");
            return;
        }

        Transform[] markers = PanelFlipper.ShowingRight ? rightSpotMarkers : leftSpotMarkers;

        if (spotIndex >= markers.Length)
        {
            Debug.LogWarning($"[TokenManager] Spot index {spotIndex} out of range.");
            return;
        }

        Transform marker = markers[spotIndex];
        Vector3 spawnPos = marker.position;

        // Count tokens already at this spot
        int tokensAtSpot = 0;
        foreach (var kvp in spawnedTokens)
            if (kvp.Value != null && kvp.Value.transform.parent == marker)
                tokensAtSpot++;

        if (tokensAtSpot > 0)
            spawnPos += new Vector3(tokenStackOffset * tokensAtSpot, 0f, 0f);

        // Spawn on server
        GameObject serverToken = Instantiate(tokenPrefabs[prefabIndex], marker);
        serverToken.transform.position = spawnPos;
        spawnedTokens[clientId] = serverToken;

        // Tell clients to spawn their copy
        SpawnTokenClientRpc(prefabIndex, PanelFlipper.ShowingRight, spotIndex, spawnPos, clientId);
    }

    [ClientRpc]
    private void SpawnTokenClientRpc(int prefabIndex, bool showingRight, int spotIndex, Vector3 position, ulong clientId)
    {
        if (IsServer) return;

        Transform[] markers = showingRight ? rightSpotMarkers : leftSpotMarkers;
        if (spotIndex >= markers.Length) return;

        GameObject token = Instantiate(tokenPrefabs[prefabIndex], markers[spotIndex]);
        token.transform.position = position;
        clientTokens[clientId] = token;
    }

    [ClientRpc]
    private void DestroyTokenClientRpc(ulong clientId)
    {
        if (IsServer) return;

        if (clientTokens.TryGetValue(clientId, out GameObject token) && token != null)
        {
            Destroy(token);
            clientTokens.Remove(clientId);
        }
    }

    public void ClearAllTokens()
    {
        if (!IsServer) return;

        ClearAllTokensClientRpc();

        foreach (var kvp in spawnedTokens)
            if (kvp.Value != null) Destroy(kvp.Value);

        spawnedTokens.Clear();
    }

    [ClientRpc]
    private void ClearAllTokensClientRpc()
    {
        if (IsServer) return;

        foreach (var kvp in clientTokens)
            if (kvp.Value != null) Destroy(kvp.Value);

        clientTokens.Clear();
    }
}