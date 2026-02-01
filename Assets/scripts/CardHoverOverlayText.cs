using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardHoverOverlay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Settings")]
    public float hoverScale = 1.2f;
    public float transitionSpeed = 10f;
    public Color outlineColor = Color.yellow;

    private RectTransform rectTransform;
    private Outline outline;

    private Vector3 originalScale;
    private Color originalOutlineColor;

    private Transform originalParent;
    private Vector3 originalPosition;

    private bool isHovered = false;

    [Header("Hover Canvas")]
    public Transform hoverCanvas; // Assign the empty HoverCanvas in the inspector

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
        rectTransform.localScale = Vector3.Lerp(
            rectTransform.localScale,
            isHovered ? originalScale * hoverScale : originalScale,
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!hoverCanvas) return;

        isHovered = true;

        // Save original parent and position
        originalParent = transform.parent;
        originalPosition = rectTransform.position;

        // Move card to hover canvas
        transform.SetParent(hoverCanvas, true); // keep world position
        rectTransform.SetAsLastSibling();       // render on top
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;

        if (!originalParent) return;

        // Return card to original parent and position
        transform.SetParent(originalParent, true);
        rectTransform.position = originalPosition;
    }
}
