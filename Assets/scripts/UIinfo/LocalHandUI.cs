using UnityEngine;
using Unity.Netcode;

public class LocalHandUI : MonoBehaviour
{
    public Transform handPanel;
    public GameObject cardPrefab;
    public Sprite[] cardSprites;
    public Sprite cardBackSprite;

    private PlayerStateNet assignedPlayer;
    private ulong assignedClientId;


    public static LocalHandUI Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }


    public void Initialize(PlayerStateNet player)
    {
        assignedPlayer = player;
        assignedClientId = player.ClientId.Value;

        // Subscribe to this player's hand changes
        assignedPlayer.Hand.OnListChanged += OnHandChanged;

        // Initial draw
        Redraw();
    }

    void OnDestroy()
    {
        if (assignedPlayer != null)
        {
            assignedPlayer.Hand.OnListChanged -= OnHandChanged;
        }
    }

    void OnHandChanged(NetworkListEvent<CardData> change)
    {
        Redraw();
    }

    // LocalHandUI.cs
    public void Redraw()
    {
        if (assignedPlayer == null) return;
        if (handPanel == null) return;

        bool isOwnHand = NetworkManager.Singleton != null &&
                         assignedClientId == NetworkManager.Singleton.LocalClientId;

        var handCards = assignedPlayer.Hand;

        // Only rebuild if card count changed
        if (handPanel.childCount != handCards.Count)
        {
            // Clear and rebuild
            while (handPanel.childCount > 0)
                DestroyImmediate(handPanel.GetChild(0).gameObject);

            for (int i = 0; i < handCards.Count; i++)
            {
                GameObject cardObj = Instantiate(cardPrefab, handPanel, false);
                CardView view = cardObj.GetComponent<CardView>();
                Sprite sprite = isOwnHand ? cardSprites[handCards[i].spriteId] : cardBackSprite;
                view.SetCard(handCards[i], sprite);
                UpdateCardAction(cardObj, handCards[i], isOwnHand);
            }
        }
        else
        {
            // Just update existing cards in place
            for (int i = 0; i < handCards.Count; i++)
            {
                GameObject cardObj = handPanel.GetChild(i).gameObject;
                CardView view = cardObj.GetComponent<CardView>();
                Sprite sprite = isOwnHand ? cardSprites[handCards[i].spriteId] : cardBackSprite;
                view.SetCard(handCards[i], sprite);
                UpdateCardAction(cardObj, handCards[i], isOwnHand);
            }
        }
    }

    void UpdateCardAction(GameObject cardObj, CardData card, bool isOwnHand)
    {
        var handler = cardObj.GetComponent<CardButtonHandler>();
        if (handler == null) return;

        var signState = SignAbilityManager.Instance?.GetState();
        var signMgr = SignAbilityManager.Instance;

        // Sign ability overrides normal actions
        if (isOwnHand && signMgr != null && signMgr.IsLocalPlayerActive())
        {
            if (signState == SignAbilityManager.AbilityState.TrailheadSelecting &&
                signMgr.GetTrailheadMode() == SignAbilityManager.TrailheadMode.Hand)
            {
                handler.SetAction(CardButtonHandler.CardAction.SignSelectCard);
                return;
            }
            if (signState == SignAbilityManager.AbilityState.TrailheadSelecting &&
                signMgr.GetTrailheadMode() == SignAbilityManager.TrailheadMode.Revealed)
            {
                handler.SetAction(CardButtonHandler.CardAction.SignSelectCard);
                return;
            }
            if (signState == SignAbilityManager.AbilityState.Done &&
                signMgr.GetTrailheadMode() == SignAbilityManager.TrailheadMode.Hand)
            {
                handler.SetAction(CardButtonHandler.CardAction.SignSelectCard);
                return;
            }
            if (signState == SignAbilityManager.AbilityState.BacktrackSelectTarget &&
                signMgr.GetBacktrackMode() == SignAbilityManager.BacktrackMode.Hand)
            {
                handler.SetAction(CardButtonHandler.CardAction.SignSelectTarget);
                return;
            }
        }

        // Normal actions
        if (isOwnHand)
            handler.SetAction(CardButtonHandler.CardAction.PlayFromHand);
        else
            handler.SetAction(CardButtonHandler.CardAction.None);
    }
}