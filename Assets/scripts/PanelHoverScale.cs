// PanelHoverScale.cs
// PanelHoverScale.cs
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;

public class PanelHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float hoverScale = 1.1f;
    public float transitionSpeed = 10f;

    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Vector3 targetScale;
    private bool isHovered = false;
    private bool pendingEnter = false;
    private bool pendingExit = false;

    private Dictionary<RectTransform, Vector2> trueOriginalPositions = new Dictionary<RectTransform, Vector2>();

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
        targetScale = originalScale;
    }

    void Update()
    {
        bool wasAnimating = HoverLock.IsAnimating;

        rectTransform.localScale = Vector3.Lerp(
            rectTransform.localScale,
            targetScale,
            Time.deltaTime * transitionSpeed
        );

        bool nowAtTarget = Vector3.Distance(rectTransform.localScale, targetScale) < 0.01f;

        if (nowAtTarget)
        {
            rectTransform.localScale = targetScale;
            if (HoverLock.PanelIsAnimating)
            {
                HoverLock.SetPanelAnimating(false);
                if (!isHovered)
                    StartCoroutine(RestoreChildPositions());
                if (pendingEnter) { pendingEnter = false; DoEnter(); }
                else if (pendingExit) { pendingExit = false; DoExit(); }
            }
        }

        if (!isHovered)
            RefreshSavedPositions();
    }

    void DoEnter()
    {
        isHovered = true;
        targetScale = originalScale * hoverScale;
        HoverLock.SetPanelAnimating(true);
    }

    void DoExit()
    {
        isHovered = false;
        targetScale = originalScale;
        HoverLock.SetPanelAnimating(true);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pendingExit = false;
        if (HoverLock.PanelIsAnimating)
            pendingEnter = true;
        else
            DoEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pendingEnter = false;
        if (HoverLock.PanelIsAnimating)
            pendingExit = true;
        else
            DoExit();
    }

    void RefreshSavedPositions()
    {
        foreach (RectTransform child in rectTransform)
            trueOriginalPositions[child] = child.anchoredPosition;

        List<RectTransform> toRemove = new List<RectTransform>();
        foreach (var key in trueOriginalPositions.Keys)
            if (key == null) toRemove.Add(key);
        foreach (var key in toRemove)
            trueOriginalPositions.Remove(key);
    }

    IEnumerator RestoreChildPositions()
    {
        yield return new WaitUntil(() =>
            Vector3.Distance(rectTransform.localScale, originalScale) < 0.01f);

        foreach (var kvp in trueOriginalPositions)
            if (kvp.Key != null)
                kvp.Key.anchoredPosition = kvp.Value;
    }
}

// HoverLock.cs
public static class HoverLock
{
    // Panel and card locks are independent
    public static bool PanelIsAnimating { get; private set; } = false;
    public static CardHoverOverlay CardAnimating { get; private set; } = null;

    // IsAnimating is true if either is animating — used by PanelHoverScale
    public static bool IsAnimating => PanelIsAnimating || CardAnimating != null;

    public static void SetPanelAnimating(bool value) => PanelIsAnimating = value;
    public static void SetCardAnimating(CardHoverOverlay card) => CardAnimating = card;
}