using System.Collections.Generic;
using UnityEngine;

public class PlayerFieldUI : MonoBehaviour
{
    public ulong ownerClientId;
    public Transform fieldPanel;
    public GameObject cardPrefab;
    public Sprite[] cardSprites;

    private readonly List<GameObject> visuals = new();

    public void Bind(FieldStateNet state)
    {
        ownerClientId = state.OwnerClientId.Value;

        state.Cards.OnListChanged += _ => Refresh(state);
        Refresh(state);
    }

    void Refresh(FieldStateNet state)
    {
        foreach (var v in visuals) Destroy(v);
        visuals.Clear();

        foreach (var card in state.Cards)
        {
            var go = Instantiate(cardPrefab, fieldPanel);
            visuals.Add(go);

            go.GetComponent<CardView>()
              .SetCard(card, cardSprites[card.spriteId]);
        }
    }
}
