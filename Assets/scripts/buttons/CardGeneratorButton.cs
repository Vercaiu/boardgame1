using UnityEngine;

public class CardGenerator : MonoBehaviour
{
    public void GenerateCards(int num)
    {
        if (DeckManager.Instance == null)
        {
            Debug.LogError("DeckManager not found!");
            return;
        }

        // Call your deck manager method
        DeckManager.Instance.RevealCardsServerRpc(num);
    }
}