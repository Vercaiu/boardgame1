using UnityEngine;
using UnityEngine.UI;

public class CardButtonHandler : MonoBehaviour
{
    public enum CardAction
    {
        None,
        ClaimFromBoard,
        PlayFromHand,
        SelectForSwap,
        SwapWithHandCard,
        SignSelectCard,      // used during Trailhead/Backtrack selection
        SignSelectTarget     // used for Backtrack target selection
    }
    private CardAction currentAction = CardAction.ClaimFromBoard;

void HandleSignSelectCard()
{
    Debug.Log($"HandleSignSelectCard called, state: {SignAbilityManager.Instance?.GetState()}");
    var mgr   = SignAbilityManager.Instance;
    var state = mgr.GetState();

    if (state == SignAbilityManager.AbilityState.TrailheadSelecting ||
        state == SignAbilityManager.AbilityState.Done)
    {
        int spriteID = GetSpriteID();
        Debug.Log($"Card ID retrieved: {spriteID}");
        if (spriteID != -1) mgr.TrailheadSelectCardServerRpc(spriteID);
    }
    else if (state == SignAbilityManager.AbilityState.BacktrackSelectField)
    {
        int spriteID = GetSpriteID();
        if (spriteID != -1) mgr.BacktrackSelectFieldCardServerRpc(spriteID);
    }
}

    void HandleSignSelectTarget()
    {
        var mgr = SignAbilityManager.Instance;
        int spriteID = GetSpriteID();
        if (spriteID != -1) mgr.BacktrackSelectTargetServerRpc(spriteID);
    }

    int GetSpriteID()
    {


        var cv = GetComponent<CardView>();
        if (cv.GetData().spriteId != 0 && cv.GetData().snowgouleid != 0)
        {
            Debug.Log($"CardView found — spriteId: {cv.GetData().spriteId}, cardId: {cv.GetData().cardId}");
            return cv.GetData().spriteId;
        }

        var rv = GetComponent<RevealedCardView>();
        if (rv.GetData().spriteId != 0 && rv.GetData().snowgouleid != 0)
        {
            Debug.Log($"RevealedCardView found — spriteId: {rv.GetData().spriteId}, cardId: {rv.GetData().cardId}");
            return rv.GetData().spriteId;
        }
        Debug.LogWarning("No CardView or RevealedCardView found on this GameObject!");
        return -1;
    }

    public void SetAction(CardAction action)
    {
        currentAction = action;
    }
    public void OnClick()
    {
        DeckManager deck = FindObjectOfType<DeckManager>();
        CardSwapManager swapManager = CardSwapManager.Instance;

        Debug.Log(currentAction);

        switch (currentAction)
        {
            case CardAction.ClaimFromBoard:
                deck.ClaimRevealedCardServerRpc(this.GetComponent<RevealedCardView>().GetData().spriteId);
                break;

            case CardAction.PlayFromHand:
                // Old behavior: directly play from hand
                deck.PlaceCardInFieldServerRpc(this.GetComponent<CardView>().GetData().spriteId);
                break;

            case CardAction.SelectForSwap:
                // New behavior for hand cards
                CardView handCard = this.GetComponent<CardView>();

                    // Field has cards, select this card for swapping
                    swapManager.SelectHandCard(handCard);
                break;

            case CardAction.SwapWithHandCard:
                // New behavior for field cards
                CardView fieldCard = this.GetComponent<CardView>();

                // Only swap if a hand card is selected
                if (swapManager.HasSelectedHandCard())
                {
                    swapManager.SwapHandAndFieldCard(fieldCard);
                }
                else
                {
                    Debug.Log("No hand card selected to swap with");
                }
                break;

            case CardAction.SignSelectCard:
                HandleSignSelectCard();
                break;
            case CardAction.SignSelectTarget:
                HandleSignSelectTarget();
                break;

            default:
                Debug.Log("Button clicked with no valid action.");
                break;
        }
    }
}
