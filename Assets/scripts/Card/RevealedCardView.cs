// RevealedCardView.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RevealedCardView : MonoBehaviour
{
    public Image artwork;
    public TMPro.TextMeshProUGUI nameText;

    private CardData data;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f; // visible by default, SlideIn will handle hiding if needed
    }


    public void Set(CardData card, Sprite sprite)
    {
        data = card;
        artwork.sprite = sprite;
    }


    // fromRight: true = slide from top-right, false = slide from top-left
    public void SlideIn(int index, bool fromRight, float staggerDelay = 0.5f, float slideDuration = 1f, float slideDistance = 1000f)
    {
        canvasGroup.alpha = 0f; // hide now, coroutine will reveal it
        StartCoroutine(SlideInCoroutine(index * staggerDelay, fromRight, slideDuration, slideDistance));
    }

    // RevealedCardView.cs — disable hover during slide, re-enable after
    private IEnumerator SlideInCoroutine(float delay, bool fromRight, float duration, float slideDistance)
    {
        // Disable interaction during slide
        var hover = GetComponent<CardHoverOverlay>();
     //   if (hover != null) hover.(false);

        var button = GetComponent<Button>();
        if (button != null) button.interactable = false;

        yield return new WaitForSeconds(delay);

        canvasGroup.alpha = 1f;

        Vector2 targetPos = rectTransform.anchoredPosition;
        float xOffset = fromRight ? slideDistance : -slideDistance;
        Vector2 startPos = targetPos + new Vector2(xOffset, slideDistance);
        rectTransform.anchoredPosition = startPos;

        float time = 0f;
        while (time < duration)
        {
            float easedT = Mathf.SmoothStep(0f, 1f, time / duration);
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, easedT);
            time += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = targetPos;

        // Re-enable interaction once settled
    //    if (hover != null) hover.SetInteractable(true);
        if (button != null) button.interactable = true;
    }

    public CardData GetData() => data;
}