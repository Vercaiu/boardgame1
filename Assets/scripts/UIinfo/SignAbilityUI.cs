// SignAbilityUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SignAbilityUI : MonoBehaviour
{
    public static SignAbilityUI Instance;

    [Header("Root Panel")]
    public GameObject rootPanel;

    [Header("Info")]
    public TextMeshProUGUI signNameText;
    public TextMeshProUGUI signDescriptionText;
    public TextMeshProUGUI activePlayerText;

    [Header("Main Buttons")]
    public Button useAbilityButton;
    public Button skipButton;

    [Header("Trailhead Buttons")]
    public GameObject trailheadModePanel;
    public Button trailheadHandButton;
    public Button trailheadRevealedButton;
    public Button trailheadConfirmButton;

    [Header("Backtrack Buttons")]
    public GameObject backtrackModePanel;
    public Button backtrackFieldButton;
    public Button backtrackHandButton;

    private int[] currentSelection = new int[0];

    static readonly string[] SignNames = { "Trailhead", "Backtrack", "Shortcut" };
    static readonly string[] SignDescs =
    {
        "Discard 2 cards from your hand or from the revealed cards. They are replaced from the deck.",
        "Choose a card in your field. Swap it with another field card or a card in your hand.",
        "Draw a card from the deck into your hand. You may place one additional card this round."
    };

    void Awake()
    {
        Instance = this;

        useAbilityButton.onClick.AddListener(() => SignAbilityManager.Instance.UseAbilityServerRpc());
        skipButton.onClick.AddListener(() => SignAbilityManager.Instance.SkipAbilityServerRpc());

        trailheadHandButton.onClick.AddListener(() =>
            SignAbilityManager.Instance.TrailheadChooseModeServerRpc(true));
        trailheadRevealedButton.onClick.AddListener(() =>
            SignAbilityManager.Instance.TrailheadChooseModeServerRpc(false));
        trailheadConfirmButton.onClick.AddListener(() =>
            SignAbilityManager.Instance.TrailheadConfirmServerRpc());

        backtrackFieldButton.onClick.AddListener(() =>
            SignAbilityManager.Instance.BacktrackChooseModeServerRpc(true));
        backtrackHandButton.onClick.AddListener(() =>
            SignAbilityManager.Instance.BacktrackChooseModeServerRpc(false));

    }

    public void Refresh()
    {
        var mgr = SignAbilityManager.Instance;
        var state = mgr.GetState();

        // Only redraw hand when state changes, NOT during highlight updates
        // This is called separately via OnValueChanged on currentState
        LocalHandUI.Instance?.Redraw();
        localfieldUI.Instance?.Redraw();


        // Reset sub-panels every refresh
        trailheadModePanel.SetActive(false);
        backtrackModePanel.SetActive(false);
        trailheadConfirmButton.gameObject.SetActive(false);
        useAbilityButton.gameObject.SetActive(false);
        skipButton.gameObject.SetActive(false);

        int signId = mgr.GetActiveSignId();
        ulong pid = mgr.GetActivePlayerId();
        bool isActive = mgr.IsLocalPlayerActive();

        PlayerStateNet player = PlayerStateNet.GetPlayer(pid);

        if (state == SignAbilityManager.AbilityState.Inactive)
        {
            signNameText.text = "No Sign Active";
            signDescriptionText.text = "";
            activePlayerText.text = "";
            //   instructionText.text = "Waiting for next turn...";
            return;
        }

        signNameText.text = signId >= 0 ? SignNames[signId] : "";
        signDescriptionText.text = signId >= 0 ? SignDescs[signId] : "";
        activePlayerText.text = $"{player.GetPlayerName()}'s ability";

        switch (state)
        {
            case SignAbilityManager.AbilityState.WaitingForChoice:
                useAbilityButton.gameObject.SetActive(isActive);
                skipButton.gameObject.SetActive(isActive);
                break;

            case SignAbilityManager.AbilityState.TrailheadChooseMode:
                trailheadModePanel.SetActive(isActive);
                break;

            case SignAbilityManager.AbilityState.TrailheadSelecting:
                if (SignAbilityManager.Instance.GetTrailheadMode() == SignAbilityManager.TrailheadMode.Revealed)
                    RevealedCardsUI.Instance?.UpdateCardActions(CardButtonHandler.CardAction.SignSelectCard);
                break;

            case SignAbilityManager.AbilityState.Done:
                trailheadConfirmButton.gameObject.SetActive(isActive);
                break;

            case SignAbilityManager.AbilityState.BacktrackSelectField:
                // No buttons needed — player just clicks a field card directly
                // Make sure field cards have SignSelectCard action (handled by localfieldUI.Redraw)
                break;

            case SignAbilityManager.AbilityState.BacktrackChooseMode:
                backtrackModePanel.SetActive(isActive);
                break;

            case SignAbilityManager.AbilityState.BacktrackSelectTarget:
                // No buttons — player clicks a field or hand card directly
                // localfieldUI and LocalHandUI Redraw handle setting correct actions
                break;
        }
        // Highlight the selected backtrack field card if one is chosen
        if (state == SignAbilityManager.AbilityState.BacktrackChooseMode ||
            state == SignAbilityManager.AbilityState.BacktrackSelectTarget)
        {
            int selectedId = mgr.GetBacktrackSelectedCard();
            if (selectedId != -1)
                UpdateSelectionHighlights(new int[] { selectedId });
        }
        else if (state == SignAbilityManager.AbilityState.Inactive ||
                 state == SignAbilityManager.AbilityState.WaitingForChoice)
        {
            // Clear highlights when sign phase resets
            ClearAllHighlights();
        }
        // Always reset revealed card actions when sign phase is inactive
        if (state == SignAbilityManager.AbilityState.Inactive)
            RevealedCardsUI.Instance?.UpdateCardActions(CardButtonHandler.CardAction.ClaimFromBoard);
    }

    // In SignAbilityUI.cs

    private List<GameObject> highlightedCards = new List<GameObject>();
    public void UpdateSelectionHighlights(int[] selected)
    {
        currentSelection = selected;

        foreach (GameObject card in highlightedCards)
        {
            if (card == null) continue;
            Image img = card.GetComponent<Image>();
            if (img != null) img.color = Color.white;
        }
        highlightedCards.Clear();

        foreach (int spriteId in selected)
        {
            GameObject card = FindCardGameObject(spriteId);
            if (card == null) continue;
            Image img = card.GetComponent<Image>();
            if (img != null) img.color = new Color(0.4f, 0.7f, 1f);
            highlightedCards.Add(card);
        }
    }

    GameObject FindCardGameObject(int spriteID)
    {
        // Search hand cards
        if (LocalHandUI.Instance != null)
        {
            foreach (Transform child in LocalHandUI.Instance.handPanel)
            {
                CardView cv = child.GetComponent<CardView>();
                if (cv != null && cv.GetData().spriteId == spriteID)
                    return child.gameObject;
            }
        }

        // Search revealed cards
        if (RevealedCardsUI.Instance != null)
        {
            foreach (Transform panel in new Transform[]{
            RevealedCardsUI.Instance.leftflat,
            RevealedCardsUI.Instance.rightflat,
            RevealedCardsUI.Instance.leftcrevasse,
            RevealedCardsUI.Instance.rightcrevasse,
            RevealedCardsUI.Instance.leftslope1,
            RevealedCardsUI.Instance.rightslope1,
            RevealedCardsUI.Instance.leftslope2,
            RevealedCardsUI.Instance.rightslope2 })
            {
                if (panel == null) continue;
                foreach (Transform child in panel)
                {
                    RevealedCardView rv = child.GetComponent<RevealedCardView>();
                    if (rv != null && rv.GetData().spriteId == spriteID)
                        return child.gameObject;
                }
            }
        }

        return null;
    }

    public void ClearAllHighlights()
    {
        foreach (GameObject card in highlightedCards)
        {
            if (card == null) continue;
            Image img = card.GetComponent<Image>();
            if (img != null) img.color = Color.white;
        }
        highlightedCards.Clear();
        currentSelection = new int[0];
    }
}