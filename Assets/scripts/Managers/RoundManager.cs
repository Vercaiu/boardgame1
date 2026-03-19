// RoundManager.cs
using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance;

    [Header("Round State")]
    private NetworkVariable<int> currentRound = new NetworkVariable<int>(0);
    private NetworkVariable<RoundPhase> currentPhase = new NetworkVariable<RoundPhase>(RoundPhase.WaitingToStart);

    private Dictionary<ulong, (int cardId, int pickOrder)> roundPicks = new Dictionary<ulong, (int, int)>();
    private int pickCounter = 0;

    private NetworkVariable<bool> currentPlayerHasTakenCard = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> currentPlayerHasPlacedCard = new NetworkVariable<bool>(false);

    public event System.Action<RoundPhase> OnPhaseChanged;
    public event System.Action<int> OnRoundStarted;

    public Button EndTurnButtonController;

    private bool gameEndingTriggered = false;
    private int finalRoundNumber = -1;
    private bool _gameInitialized = false;

    [Header("Starting Field Cards")]
    public CardData[] startingFieldCards;

    public enum RoundPhase
    {
        WaitingToStart,
        SignAbilities,
        TakingCard,
        PlacingCard,
        RoundEnd
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        currentPhase.OnValueChanged += OnPhaseChangedHandler;
        currentPlayerHasTakenCard.OnValueChanged += OnTakenCardChanged;
        currentPlayerHasPlacedCard.OnValueChanged += OnPlacedCardChanged;

        if (GameStartManager.Instance != null)
            GameStartManager.Instance.OnTurnOrderSet += OnGameStarted;
    }

    // ============================================================
    //  GAME START
    // ============================================================

    void OnGameStarted(List<ulong> order)
    {
        if (!IsServer) return;
        if (_gameInitialized) return;

        int expectedPlayers = NetworkManager.Singleton.ConnectedClients.Count;
        if (order.Count < expectedPlayers) return;

        _gameInitialized = true;
        StartCoroutine(DealStartingCardsAndBegin(order));
    }

    IEnumerator DealStartingCardsAndBegin(List<ulong> order)
    {
        yield return new WaitUntil(() =>
        {
            foreach (ulong id in order)
                if (PlayerStateNet.GetPlayer(id) == null) return false;
            return true;
        });

        for (int i = 0; i < order.Count; i++)
        {
            if (i >= startingFieldCards.Length)
            {
                Debug.LogWarning($"RoundManager: No starting field card for slot {i}!");
                continue;
            }
            PlayerStateNet player = PlayerStateNet.GetPlayer(order[i]);
            if (player == null) continue;
            player.Field.Add(startingFieldCards[i]);
            PlayerTokenManager.Instance?.PlaceStartingToken(order[i], startingFieldCards[i].spriteId, i);
            yield return null; // wait a frame so UI can process the destroy/rebuild
        }

        DeckManager.Instance?.DealStartingHands(2);
        yield return null;
        StartCoroutine(BeginRound());
    }

    // Called by EndGameManager on rematch to re-trigger the game start flow
    public void OnRematch()
    {
        if (!IsServer) return;
        _gameInitialized = true; // set true BEFORE dealing to block OnGameStarted from firing
        gameEndingTriggered = false;
        finalRoundNumber = -1;
        currentRound.Value = 0;

        foreach (var p in FindObjectsByType<PlayerStateNet>(FindObjectsSortMode.None))
            p.ResetForRematch();

        List<ulong> order = GameStartManager.Instance.GetTurnOrder();
        if (order.Count > 0)
            StartCoroutine(DealStartingCardsAndBegin(order));
    }

    IEnumerator BeginRound()
    {
        if (!IsServer) yield break;

        currentRound.Value++;
        roundPicks.Clear();
        pickCounter = 0;

        int revealCount = 2 + TurnManager.Instance.GetPlayerCount();
        NotifyRoundStartedClientRpc(currentRound.Value);
        FlipAndRevealClientRpc(revealCount);

        yield return new WaitForSeconds(1f);
        yield return null;

        SetPhase(RoundPhase.SignAbilities);
        OnSignAbilitiesPhase();
    }

    public void OnSignPhaseComplete()
    {
        if (!IsServer) return;
        StartCoroutine(BeginTurns());
    }

    void OnSignAbilitiesPhase()
    {
        if (!IsServer) return;
        SignAbilityManager.Instance.BeginSignPhaseForPlayer(TurnManager.Instance.GetCurrentTurnPlayer());
    }

    IEnumerator BeginTurns()
    {
        if (!IsServer) yield break;
        SetPhase(RoundPhase.TakingCard);
        currentPlayerHasTakenCard.Value = false;
        currentPlayerHasPlacedCard.Value = false;
        yield break;
    }

    public void NotifyCardTaken(ulong clientId, int cardId)
    {
        if (!IsServer) return;
        if (currentPhase.Value != RoundPhase.TakingCard) return;

        roundPicks[clientId] = (cardId, pickCounter++);
        PlayerTokenManager.Instance?.MoveTokenToSpot(clientId, cardId);
        currentPlayerHasTakenCard.Value = true;
        SetPhase(RoundPhase.PlacingCard);
    }

    public void NotifyCardPlaced(ulong clientId)
    {
        if (!IsServer) return;

        PlayerStateNet player = PlayerStateNet.GetPlayer(clientId);
        if (player == null) return;

        if (player.Hand.Count > 2)
        {
            currentPlayerHasPlacedCard.Value = false;
            ChatManager.Instance.SendSystemMessage($"Client {clientId} must place another card (hand size: {player.Hand.Count})");
        }
        else
        {
            currentPlayerHasPlacedCard.Value = true;
        }

        UpdateEndTurnButtonState();
    }

    public void OnPlayerEndedTurn(ulong clientId)
    {
        if (!IsServer) return;

        if (!currentPlayerHasTakenCard.Value || !currentPlayerHasPlacedCard.Value)
        {
            ChatManager.Instance.SendSystemMessage("You must take a card and place it before ending your turn!");
            return;
        }

        currentPlayerHasTakenCard.Value = false;
        currentPlayerHasPlacedCard.Value = false;

        PlayerStateNet player = PlayerStateNet.GetPlayer(clientId);
        if (!gameEndingTriggered && player.Field.Count >= 11)
        {
            gameEndingTriggered = true;
            finalRoundNumber = currentRound.Value + 1;
            ChatManager.Instance.SendSystemMessage($"Player {clientId} triggered the final round!");
        }

        List<ulong> order = TurnManager.Instance.GetTurnOrder();
        if (roundPicks.Count >= order.Count)
        {
            StartCoroutine(EndRound());
        }
        else
        {
            TurnManager.Instance.NextTurn();
            OnSignAbilitiesPhase();
            SetPhase(RoundPhase.SignAbilities);
        }
    }

    IEnumerator EndRound()
    {
        if (!IsServer) yield break;

        SetPhase(RoundPhase.RoundEnd);
        yield return new WaitForSeconds(0.5f);

        List<ulong> newOrder = CalculateNewTurnOrder();
        ApplyNewTurnOrder(newOrder);

        yield return new WaitForSeconds(0.5f);

        if (gameEndingTriggered && currentRound.Value >= finalRoundNumber)
            StartCoroutine(EndGame());
        else
            StartCoroutine(BeginRound());
    }

    IEnumerator EndGame()
    {
        ChatManager.Instance.SendSystemMessage("=== FINAL ROUND COMPLETE ===");
        yield return new WaitForSeconds(1f);

        foreach (var kvp in ScoringSystem.Instance.GetAllScores().OrderByDescending(p => p.Value))
            ChatManager.Instance.SendSystemMessage($"Player {kvp.Key} Score: {kvp.Value}");

        ChatManager.Instance.SendSystemMessage("=== GAME OVER ===");
        SetPhase(RoundPhase.WaitingToStart);

        EndGameManager.Instance?.ShowEndGameUI();
    }

    // Called by EndGameManager when all players vote rematch
    public void RestartGame()
    {
        if (!IsServer) return;

        gameEndingTriggered = false;
        finalRoundNumber = -1;
        _gameInitialized = false;
        currentRound.Value = 0;

        List<ulong> order = TurnManager.Instance.GetTurnOrder();
        StartCoroutine(DealStartingCardsAndBegin(order));
    }

    // ============================================================
    //  TURN ORDER
    // ============================================================

    List<ulong> CalculateNewTurnOrder()
    {
        return roundPicks
            .OrderBy(p => p.Value.cardId)
            .ThenByDescending(p => p.Value.pickOrder)
            .Select(p => p.Key)
            .ToList();
    }

    void ApplyNewTurnOrder(List<ulong> newOrder)
    {
        NetworkList<ulong> turnOrderList = GameStartManager.Instance.TurnOrder;
        turnOrderList.Clear();
        foreach (ulong id in newOrder)
            turnOrderList.Add(id);

        ChatManager.Instance.SendSystemMessage($"New turn order: {string.Join(" -> ", newOrder)}");
    }

    // ============================================================
    //  CLIENT RPCs
    // ============================================================

    [ClientRpc]
    void NotifyRoundStartedClientRpc(int round)
    {
        OnRoundStarted?.Invoke(round);
        ChatManager.Instance?.SendSystemMessage($"--- Round {round} begins! ---");
    }

    [ClientRpc]
    void FlipAndRevealClientRpc(int revealCount)
    {
        StartCoroutine(FlipThenReveal(revealCount));
    }

    IEnumerator FlipThenReveal(int revealCount)
    {
        PanelFlipper.Instance?.TogglePanels();
        yield return new WaitForSeconds(PanelFlipper.Instance != null
            ? PanelFlipper.Instance.moveDuration * PanelFlipper.Instance.cardSlideStartPoint
            : 0.3f);

        if (IsServer)
            DeckManager.Instance?.RevealCardsServerRpc(revealCount);

        yield return new WaitForSeconds(0.1f);
        RevealedCardsUI.Instance?.Redraw(true);
    }

    // ============================================================
    //  HELPERS
    // ============================================================

    void SetPhase(RoundPhase phase) => currentPhase.Value = phase;

    void OnPhaseChangedHandler(RoundPhase old, RoundPhase next)
    {
        OnPhaseChanged?.Invoke(next);
        UpdateUIForPhase(next);
    }

    void OnTakenCardChanged(bool old, bool next) => UpdateEndTurnButtonState();
    void OnPlacedCardChanged(bool old, bool next) => UpdateEndTurnButtonState();

    void UpdateEndTurnButtonState()
    {
        bool canEnd = currentPlayerHasTakenCard.Value && currentPlayerHasPlacedCard.Value
                      && TurnManager.Instance.IsMyTurn();
        EndTurnButtonController.interactable = canEnd;
    }

    void UpdateUIForPhase(RoundPhase phase) { }

    // ============================================================
    //  PUBLIC API
    // ============================================================

    public RoundPhase GetCurrentPhase() => currentPhase.Value;
    public int GetCurrentRound() => currentRound.Value;
    public bool CanTakeCard() => currentPhase.Value == RoundPhase.TakingCard && TurnManager.Instance.IsMyTurn();
    public bool CanPlaceCard() => currentPhase.Value == RoundPhase.PlacingCard && TurnManager.Instance.IsMyTurn();
    public bool CanEndTurn() => currentPlayerHasTakenCard.Value && currentPlayerHasPlacedCard.Value && TurnManager.Instance.IsMyTurn();
}