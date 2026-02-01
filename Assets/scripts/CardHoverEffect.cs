using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardHoverEffect: MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Hover Settings")]
    public float hoverScale = 1.2f;
    public float transitionSpeed = 10f;
    public Color outlineColor = Color.yellow;

    RectTransform rectTransform;
    Outline outline;

    Vector3 originalScale;
    Color originalOutlineColor;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        outline = GetComponent<Outline>();

        originalScale = rectTransform.localScale;

        if (outline != null)
            originalOutlineColor = outline.effectColor;
    }

    void Update()
    {
        // Smooth scale interpolation
        rectTransform.localScale = Vector3.Lerp(
            rectTransform.localScale,
            originalScale * (isHovered ? hoverScale : 1f),
            Time.deltaTime * transitionSpeed
        );

        if (outline != null)
        {
            outline.effectColor = Color.Lerp(
                outline.effectColor,
                isHovered ? outlineColor : originalOutlineColor,
                Time.deltaTime * transitionSpeed
            );
        }
    }

    bool isHovered;

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        rectTransform.SetAsLastSibling(); // bring to front
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }
}
