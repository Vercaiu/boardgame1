using UnityEngine;
using Unity.Netcode;

public class CardSwapManager : MonoBehaviour
{
    private static CardSwapManager instance;
    public static CardSwapManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<CardSwapManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("CardSwapManager");
                    instance = go.AddComponent<CardSwapManager>();
                }
            }
            return instance;
        }
    }

    private CardView selectedHandCard;
    private Material originalMaterial;
    private Vector3 originalScale;
    private Color highlightColor = Color.yellow;

    void Awake()
    {

    }

    public void SelectHandCard(CardView cardView)
    {
        // If clicking the same card, deselect it
        if (selectedHandCard == cardView)
        {
            DeselectHandCard();
            return;
        }

        // Deselect previous card if any
        if (selectedHandCard != null)
        {
            DeselectHandCard();
        }

        // Select the new card
        selectedHandCard = cardView;

        // Store original values
        originalScale = cardView.transform.localScale;

        // Get the renderer (adjust based on your card structure)
        Renderer renderer = cardView.GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = cardView.GetComponentInChildren<Renderer>();
        }

        if (renderer != null)
        {
            originalMaterial = renderer.material;

            // Create a new material instance to avoid affecting other cards
            Material highlightMaterial = new Material(originalMaterial);
            highlightMaterial.color = highlightColor;
            renderer.material = highlightMaterial;
        }

        // Scale up the card
        cardView.transform.localScale = originalScale * 1.1f;
    }

    public void DeselectHandCard()
    {
        if (selectedHandCard == null) return;

        // Restore original scale
        selectedHandCard.transform.localScale = originalScale;

        // Restore original material
        Renderer renderer = selectedHandCard.GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = selectedHandCard.GetComponentInChildren<Renderer>();
        }

        if (renderer != null && originalMaterial != null)
        {
            renderer.material = originalMaterial;
        }

        selectedHandCard = null;
        originalMaterial = null;
    }

    public bool HasSelectedHandCard()
    {
        return selectedHandCard != null;
    }

    public CardView GetSelectedHandCard()
    {
        return selectedHandCard;
    }

    public void SwapHandAndFieldCard(CardView fieldCard)
    {
        if (selectedHandCard == null)
        {
            Debug.LogWarning("No hand card selected for swap");
            return;
        }

        int handCardId = selectedHandCard.GetData().spriteId;
        int fieldCardId = fieldCard.GetData().spriteId;

        // Find the DeckManager to call the ServerRpc
        DeckManager deck = FindObjectOfType<DeckManager>();
        if (deck != null)
        {
            deck.SwapHandAndFieldCardServerRpc(handCardId, fieldCardId);
        }

        // Deselect after initiating swap
        DeselectHandCard();
    }

    public void PlaceCardInField(CardView handCard)
    {
        if (handCard == null) return;

        int spriteID = handCard.GetData().spriteId;

        DeckManager deck = FindObjectOfType<DeckManager>();
        if (deck != null)
        {
            deck.PlaceCardInFieldServerRpc(spriteID);
        }

        // Deselect if the card was selected
        if (selectedHandCard == handCard)
        {
            DeselectHandCard();
        }
    }
}