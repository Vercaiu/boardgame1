using UnityEngine;
using Unity.Netcode;

public class NetworkedCard : NetworkBehaviour
{
    public void DrawCard(ulong clientId)
    {
        if (!IsServer) return;

        // Assign ownership to the drawing player
        NetworkObject.ChangeOwnership(clientId);

        // Move it to the player's hand
        Transform handPanel = PlayerManager.Instance.GetHandPanel(clientId);
        transform.SetParent(handPanel, false);

        // Hide for all other clients
        HideForOtherClientsClientRpc(clientId);
    }

    [ClientRpc]
    private void HideForOtherClientsClientRpc(ulong ownerId)
    {
        if (NetworkManager.Singleton.LocalClientId != ownerId)
        {
            gameObject.SetActive(false); // disappear for everyone else
        }
    }
}
