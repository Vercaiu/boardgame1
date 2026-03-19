using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;
    private Dictionary<ulong, Transform> playerHands = new Dictionary<ulong, Transform>();

    void Awake() { Instance = this; }

    public void RegisterHand(ulong clientId, Transform handPanel)
    {
        playerHands[clientId] = handPanel;
    }

    public Transform GetHandPanel(ulong clientId)
    {
        return playerHands[clientId];
    }
}
