// FieldCardLayoutManager.cs
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct CardTransitionOffset
{
    public int fromCardId;
    public int toCardId;
    public float verticalOffset;
}

public class FieldCardLayoutManager : MonoBehaviour
{
    [Header("Layout Settings")]
    public float cardWidth = 100f;
    public float cardSpacing = 5f;

    [Header("Panel Settings")]
    public float baseCardHeight = 150f;

    [Header("Transition Offsets")]
    public List<CardTransitionOffset> offsets = new List<CardTransitionOffset>();

    private RectTransform rectTransform;
    private float highestPoint = 0f;
    private float lowestPoint = 0f;
    private float cardShiftAccumulator = 0f;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void RepositionCards()
    {
        // Only consider children that are NOT pending destruction
        List<RectTransform> activeCards = new List<RectTransform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            if (child.name != "__pendingDestroy")
                activeCards.Add(child as RectTransform);
        }

        int childCount = activeCards.Count;
        if (childCount == 0) return;

        float offset = 0f;
        float change = 0f;
        float currentY = 0f;
        float currentX = 0f;
        List<Vector2> positions = new List<Vector2>();

        for (int i = 0; i < childCount; i++)
        {
            if (i > 0)
            {
                CardView prevView = activeCards[i - 1]?.GetComponent<CardView>();
                CardView currView = activeCards[i]?.GetComponent<CardView>();

                if (prevView != null && currView != null)
                {
                    offset = GetOffset(prevView.GetData().cardId, currView.GetData().cardId);
                    currentY += offset;
                }

                currentX += cardWidth + cardSpacing;
            }

            positions.Add(new Vector2(currentX, currentY));
        }

        cardShiftAccumulator += offset;


        if (cardShiftAccumulator > highestPoint)
        {
            change = cardShiftAccumulator - highestPoint;
            highestPoint = cardShiftAccumulator;
        }
        if (cardShiftAccumulator < lowestPoint)
        {
            change = cardShiftAccumulator - lowestPoint;
            lowestPoint = cardShiftAccumulator;
        }

        Debug.Log("change is" + change);
        Debug.Log("cardshifaccumulator is" + cardShiftAccumulator);
        Debug.Log("offset is" + offset);

        for (int i = 0; i < childCount; i++)
        {
            if (activeCards[i] != null)
                activeCards[i].anchoredPosition = positions[i] - new Vector2(0, change);
        }
    }

    float GetOffset(int fromId, int toId)
    {
        foreach (var o in offsets)
            if (o.fromCardId == fromId && o.toCardId == toId)
                return o.verticalOffset;
        return 0f;
    }
}