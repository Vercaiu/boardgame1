using Unity.Netcode;
using UnityEngine;

public class FieldStateNet : NetworkBehaviour
{
    public NetworkVariable<ulong> OwnerClientId = new();
    public NetworkList<CardData> Cards;

    public override void OnNetworkSpawn()
    {
        Cards ??= new NetworkList<CardData>();
    }
}
