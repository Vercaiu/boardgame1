using Unity.Netcode;
using UnityEngine;

public class SpawnManager : NetworkBehaviour
{
    public GameObject playerFieldPrefab;
    private static bool hasSpawnedController = false;
    public GameObject playarea;
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // Spawn the controller once (not per player, just once total for this client)
        if (!hasSpawnedController)
        {
            SpawnController();
            //  Spawndeck();
            hasSpawnedController = true;
        }

        SpawnFieldsForAllPlayers();
    }

    void SpawnFieldsForAllPlayers()
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            GameObject fieldObj = Instantiate(playerFieldPrefab);
            NetworkObject netObj = fieldObj.GetComponent<NetworkObject>();

            PlayerFieldUI fieldUI = fieldObj.GetComponent<PlayerFieldUI>();
      //      fieldUI.OwnerClientId.Value = clientId;

            netObj.Spawn();

            Debug.Log($"Spawned field for client {clientId}");
        }
    }
    
    void SpawnController()
    {
        Debug.Log("Spawning spawncontroller");

        GameObject controllerObj = Instantiate(playarea);

        // The controller will initialize itself via OnNetworkSpawn if it's a NetworkBehaviour
        // OR via Start if it's a regular MonoBehaviour

        Debug.Log("FieldCycleController spawned");
    }
}


