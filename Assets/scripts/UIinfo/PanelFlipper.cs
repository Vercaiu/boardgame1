// PanelFlipper.cs
using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class PanelFlipper : NetworkBehaviour
{
    public RectTransform leftPanel, middlePanel, rightPanel;
    public float moveDistance = 600f;
    public float moveDuration = 0.6f;

    [Range(0f, 1f)]
    public float cardSlideStartPoint = 0.3f;


    public static bool ShowingRight { get; private set; } = false;
    public static bool IsAnimating { get; private set; } = false;
    
    
    public static PanelFlipper Instance;
    void Awake() => Instance = this;


    // Called by the button in the inspector
    public void TogglePanels()
    {
        if (IsAnimating) return;
        TogglePanelsServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void TogglePanelsServerRpc()
    {
        TogglePanelsClientRpc();
    }

    [ClientRpc]
    private void TogglePanelsClientRpc()
    {
        if (IsAnimating) return;
        ShowingRight = !ShowingRight;
        StartCoroutine(AnimatePanels(ShowingRight ? -moveDistance : moveDistance, ShowingRight));
    }

    // For end-of-round server-driven flipping
    public void FlipToRight(bool goRight)
    {
        if (IsServer) FlipToRightClientRpc(goRight);
    }

    [ClientRpc]
    private void FlipToRightClientRpc(bool goRight)
    {
        if (IsAnimating) return;
        ShowingRight = goRight;
        StartCoroutine(AnimatePanels(goRight ? -moveDistance : moveDistance, goRight));
    }

    private IEnumerator AnimatePanels(float distance, bool goingRight)
    {
        IsAnimating = true;

        Vector2 leftStart = leftPanel.anchoredPosition;
        Vector2 midStart = middlePanel.anchoredPosition;
        Vector2 rightStart = rightPanel.anchoredPosition;

        CanvasGroup leftGroup = leftPanel.GetComponent<CanvasGroup>();
        CanvasGroup rightGroup = rightPanel.GetComponent<CanvasGroup>();

        bool cardsSpawned = false;
        float time = 0f;

        while (time < moveDuration)
        {
            float t = time / moveDuration;
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            leftPanel.anchoredPosition = Vector2.Lerp(leftStart, leftStart + new Vector2(distance, 0), easedT);
            middlePanel.anchoredPosition = Vector2.Lerp(midStart, midStart + new Vector2(distance, 0), easedT);
            rightPanel.anchoredPosition = Vector2.Lerp(rightStart, rightStart + new Vector2(distance, 0), easedT);

            leftGroup.alpha = goingRight ? 1f - easedT : easedT;
            rightGroup.alpha = goingRight ? easedT : 1f - easedT;

            if (!cardsSpawned && t >= cardSlideStartPoint)
            {
                cardsSpawned = true;
                RevealedCardsUI.Instance?.Redraw(true);
            }

            time += Time.deltaTime;
            yield return null;
        }

        leftPanel.anchoredPosition = leftStart + new Vector2(distance, 0);
        middlePanel.anchoredPosition = midStart + new Vector2(distance, 0);
        rightPanel.anchoredPosition = rightStart + new Vector2(distance, 0);

        IsAnimating = false;

        RevealedCardsUI.Instance?.RedrawIfPending();
    }
}