using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class PlayerFieldSpawner : MonoBehaviour
{
    public GameObject playerFieldPrefab;

    void Start()
    {
        StartCoroutine(WaitAndSpawnField());
    }

    IEnumerator WaitAndSpawnField()
    {
        // Wait for network to be ready
        while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // Extra safety delay
        yield return new WaitForSeconds(0.3f);

        ulong myClientId = NetworkManager.Singleton.LocalClientId;
        Debug.Log($"Spawning field for client {myClientId}");

        GameObject myField = Instantiate(playerFieldPrefab);
        PlayerFieldUI fieldUI = myField.GetComponent<PlayerFieldUI>();
    //   fieldUI.OwnerClientId.Value = myClientId;

        Debug.Log($"Field spawned and assigned to client {myClientId}");
    }
}