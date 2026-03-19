using UnityEngine;
using Unity.Netcode;
public class localfieldUI : MonoBehaviour
{
    public Transform handPanel;
    public GameObject cardPrefab;
    public Sprite[] cardSprites;
    private PlayerStateNet assignedPlayer;
    private ulong assignedClientId;
    public static localfieldUI Instance { get; private set; }
    void Awake()
    {
        Instance = this;
    }
    private FieldCardLayoutManager layoutManager;
    public void Initialize(PlayerStateNet player)
    {
        assignedPlayer = player;
        assignedClientId = player.ClientId.Value;
        assignedPlayer.Field.OnListChanged += OnFieldChanged;
        layoutManager = handPanel.GetComponent<FieldCardLayoutManager>();
        Redraw();
    }
    void OnFieldChanged(NetworkListEvent<CardData> change)
    {
        Redraw();
        if (ScoringSystem.Instance != null)
        {
            ScoringSystem.Instance.RecalculateAllScores();
        }
    }
    void OnDestroy()
    {
        if (assignedPlayer != null)
        {
            assignedPlayer.Field.OnListChanged -= OnFieldChanged;
        }
    }
    public void Redraw()
    {
        if (assignedPlayer == null) return;
        bool isOwn = NetworkManager.Singleton != null &&
                     assignedClientId == NetworkManager.Singleton.LocalClientId;
        var fieldCards = assignedPlayer.Field;
        if (handPanel.childCount != fieldCards.Count)
        {
            for (int i = handPanel.childCount - 1; i >= 0; i--)
            {
                var child = handPanel.GetChild(i).gameObject;
                child.name = "__pendingDestroy";
                Destroy(child);
            }
            for (int i = 0; i < fieldCards.Count; i++)
            {
                GameObject cardObj = Instantiate(cardPrefab, handPanel, false);
                CardView view = cardObj.GetComponent<CardView>();
                view.SetCard(fieldCards[i], cardSprites[fieldCards[i].spriteId]);
                UpdateFieldCardAction(cardObj, fieldCards[i], isOwn);
            }
        }
        else
        {
            for (int i = 0; i < fieldCards.Count; i++)
            {
                GameObject cardObj = handPanel.GetChild(i).gameObject;
                CardView view = cardObj.GetComponent<CardView>();
                view.SetCard(fieldCards[i], cardSprites[fieldCards[i].spriteId]);
                UpdateFieldCardAction(cardObj, fieldCards[i], isOwn);
            }
        }
        layoutManager?.RepositionCards();
    }
    void UpdateFieldCardAction(GameObject cardObj, CardData card, bool isOwn)
    {
        var handler = cardObj.GetComponent<CardButtonHandler>();
        if (handler == null) return;
        var signState = SignAbilityManager.Instance?.GetState();
        var signMgr = SignAbilityManager.Instance;
        if (isOwn && signMgr != null && signMgr.IsLocalPlayerActive())
        {
            if (signState == SignAbilityManager.AbilityState.BacktrackSelectField)
            {
                handler.SetAction(CardButtonHandler.CardAction.SignSelectCard);
                return;
            }
            if (signState == SignAbilityManager.AbilityState.BacktrackChooseMode)
            {
                handler.SetAction(CardButtonHandler.CardAction.SignSelectCard);
                return;
            }
            if (signState == SignAbilityManager.AbilityState.BacktrackSelectTarget &&
                signMgr.GetBacktrackMode() == SignAbilityManager.BacktrackMode.Field)
            {
                handler.SetAction(CardButtonHandler.CardAction.SignSelectTarget);
                return;
            }
        }
        handler.SetAction(CardButtonHandler.CardAction.None);
    }
}