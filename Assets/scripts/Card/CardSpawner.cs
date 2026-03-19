using UnityEngine;

public class CardSpawner : MonoBehaviour
{
    [Header("Card Settings")]
    public GameObject cardPrefab;       // assign your card prefab here
    public int numberOfCards = 5;       // how many cards to spawn
    public Transform handPanel;         // where cards should appear
    public Transform slopePanel;        // scene slope panel
    public Transform hoverCanvas;       // hover canvas

    public void SpawnCards()
    {
        if (cardPrefab == null || handPanel == null || slopePanel == null)
        {
            Debug.LogError("CardPrefab, HandPanel, or SlopePanel not assigned!");
            return;
        }

        for (int i = 0; i < numberOfCards; i++)
        {
            // Instantiate the card as child of the hand panel
            GameObject newCard = Instantiate(cardPrefab, handPanel);

            // Reset scale & rotation (optional)
            newCard.transform.localScale = Vector3.one;
            newCard.transform.localRotation = Quaternion.identity;

            // Set slope panel and hover canvas so hover disables correctly
            CardHoverOverlay hoverScript = newCard.GetComponent<CardHoverOverlay>();
            if (hoverScript != null)
            {
           //     hoverScript.slopepanel = slopePanel;
            }
        }
    }
}
