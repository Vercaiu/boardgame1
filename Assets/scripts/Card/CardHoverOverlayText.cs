// CardHoverOverlay.cs
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
    private bool interactable = true;
    private bool isHovered = false;

    private static CardHoverOverlay currentlyHovered = null;
    private static Canvas overlayCanvas = null;

    // The ghost copy sitting on the overlay canvas
    private GameObject ghost = null;
    private RectTransform ghostRect = null;
    private Outline ghostOutline = null;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        outline = GetComponent<Outline>();
        originalScale = rectTransform.localScale;
        EnsureOverlayCanvas();
    }

    void Update()
    {
        if (ghost == null) return;

        // Keep ghost positioned exactly over the original card
        ghostRect.position = rectTransform.position;

        // Animate scale on the ghost
        Vector3 target = originalScale * hoverScale;
        ghostRect.localScale = Vector3.Lerp(ghostRect.localScale, target, Time.deltaTime * transitionSpeed);

        // Animate outline on the ghost
        if (ghostOutline != null)
            ghostOutline.effectColor = Color.Lerp(ghostOutline.effectColor, outlineColor, Time.deltaTime * transitionSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!interactable) return;
        if (HoverLock.PanelIsAnimating) return;

        if (currentlyHovered != null && currentlyHovered != this)
            currentlyHovered.DoExit();

        DoEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isHovered) return;
        DoExit();
    }

    void DoEnter()
    {
        isHovered = true;
        currentlyHovered = this;

        // Duplicate the card onto the overlay canvas
        ghost = Instantiate(gameObject, overlayCanvas.transform);
        ghostRect = ghost.GetComponent<RectTransform>();
        ghostOutline = ghost.GetComponent<Outline>();

        // Remove the hover script from the ghost so it doesn't react to pointer events
        Destroy(ghost.GetComponent<CardHoverOverlay>());

        // Disable all raycasting on the ghost and its children so it never blocks pointer events
        foreach (var graphic in ghost.GetComponentsInChildren<Graphic>())
            graphic.raycastTarget = false;

        // Disable any canvas groups that might block raycasts
        foreach (var cg in ghost.GetComponentsInChildren<CanvasGroup>())
            cg.blocksRaycasts = false;

        // Match position and start at original scale
        ghostRect.position = rectTransform.position;
        ghostRect.sizeDelta = rectTransform.sizeDelta;
        ghostRect.localScale = originalScale;

        // Set outline to original color so it can animate to hover color
        if (ghostOutline != null)
            ghostOutline.effectColor = outline != null ? outline.effectColor : Color.clear;

        HoverLock.SetCardAnimating(this);
    }

    void DoExit()
    {
        isHovered = false;
        if (currentlyHovered == this)
            currentlyHovered = null;

        if (ghost != null)
        {
            Destroy(ghost);
            ghost = null;
            ghostRect = null;
            ghostOutline = null;
        }

        HoverLock.SetCardAnimating(null);
    }

    public void SetInteractable(bool value)
    {
        interactable = value;
        if (!value && isHovered)
            DoExit();
    }

    void OnDestroy()
    {
        if (currentlyHovered == this)
            currentlyHovered = null;

        if (ghost != null)
            Destroy(ghost);
    }

    static void EnsureOverlayCanvas()
    {
        if (overlayCanvas != null) return;

        GameObject obj = new GameObject("HoverOverlayCanvas");
        overlayCanvas = obj.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 9999;
        obj.AddComponent<CanvasScaler>();
        obj.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(obj);
    }
}