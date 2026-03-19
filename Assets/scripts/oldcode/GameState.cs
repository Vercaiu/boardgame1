using Unity.Netcode;

public class GameState : NetworkBehaviour
{
    public FieldStateNet fieldPrefab;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    void OnClientConnected(ulong clientId)
    {
        FieldStateNet field = Instantiate(fieldPrefab);
        field.OwnerClientId.Value = clientId;
        field.NetworkObject.Spawn();
    }
}
