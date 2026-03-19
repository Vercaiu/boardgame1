// SignAbilityManager.cs
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class SignAbilityManager : NetworkBehaviour
{
    public static SignAbilityManager Instance;

    private NetworkVariable<int> activeSignId = new NetworkVariable<int>(-1);
    private NetworkVariable<ulong> activePlayerId = new NetworkVariable<ulong>(0);
    private NetworkVariable<AbilityState> currentState = new NetworkVariable<AbilityState>(AbilityState.Inactive);

    // Trailhead state
    private NetworkVariable<TrailheadMode> trailheadMode = new NetworkVariable<TrailheadMode>(TrailheadMode.None);
    private List<int> selectedCardIds = new List<int>(); // server-side selection tracking

    // Backtrack state
    private NetworkVariable<int> backtrackSelectedFieldCardId = new NetworkVariable<int>(-1);
    private NetworkVariable<BacktrackMode> backtrackMode = new NetworkVariable<BacktrackMode>(BacktrackMode.None);
    public enum AbilityState
    {
        Inactive,
        WaitingForChoice,    // showing Use/Skip buttons
        TrailheadChooseMode, // choosing hand or revealed
        TrailheadSelecting,  // clicking 2 cards
        BacktrackSelectField,// clicking a field card
        BacktrackChooseMode, // choosing swap with field or hand
        BacktrackSelectTarget,// clicking the target
        Done
    }

    public enum TrailheadMode { None, Hand, Revealed }
    public enum BacktrackMode { None, Field, Hand }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        activeSignId.OnValueChanged += (o, n) => SignAbilityUI.Instance?.Refresh();
        activePlayerId.OnValueChanged += (o, n) => SignAbilityUI.Instance?.Refresh();
        currentState.OnValueChanged += (o, n) => SignAbilityUI.Instance?.Refresh();
        trailheadMode.OnValueChanged += (o, n) => SignAbilityUI.Instance?.Refresh();
        backtrackSelectedFieldCardId.OnValueChanged += (o, n) => SignAbilityUI.Instance?.Refresh();
        backtrackMode.OnValueChanged += (o, n) => SignAbilityUI.Instance?.Refresh();
    }

    // ============================================================
    //  CALLED BY ROUNDMANAGER AT START OF EACH TURN
    // ============================================================

    public void BeginSignPhaseForPlayer(ulong clientId)
    {

        if (!IsServer) return;

        selectedCardIds.Clear();

        // Find rightmost field card (last child = last added)
        PlayerStateNet player = FindPlayerState(clientId);
        Debug.Log($"BeginSignPhaseForPlayer called for {clientId}, field count: {player?.Field.Count}");
        if (player == null || player.Field.Count == 0)
        {
            // No field cards yet — skip sign phase
            FinishSignPhase();
            return;
        }

        CardData rightmost = player.Field[player.Field.Count - 1];
        int signId = rightmost.signid;

        // Only ids 0, 1, 2 are signs
        if (signId > 2)
        {
            FinishSignPhase();
            return;
        }

        activeSignId.Value = signId;
        activePlayerId.Value = clientId;
        currentState.Value = AbilityState.WaitingForChoice;
    }

    // ============================================================
    //  SERVER RPCs — called by UI buttons
    // ============================================================

    [ServerRpc(RequireOwnership = false)]
    public void SkipAbilityServerRpc(ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != activePlayerId.Value) return;
        FinishSignPhase();
    }

    [ServerRpc(RequireOwnership = false)]
    public void UseAbilityServerRpc(ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != activePlayerId.Value) return;
                            Debug.Log("is the active sign id" + activeSignId);
        switch (activeSignId.Value)
        {

            case 0: // Trailhead — choose mode first
                currentState.Value = AbilityState.TrailheadChooseMode;
                break;
            case 1: // Backtrack — select a field card first
                currentState.Value = AbilityState.BacktrackSelectField;
                break;
            case 2: // Shortcut — instant, no selection needed
                Debug.Log("is it not doing the shorcut");
                ExecuteShortcut();
                break;
        }
    }

    // ---- TRAILHEAD ----

    [ServerRpc(RequireOwnership = false)]
    public void TrailheadChooseModeServerRpc(bool fromHand, ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != activePlayerId.Value) return;
        if (currentState.Value != AbilityState.TrailheadChooseMode) return;

        trailheadMode.Value = fromHand ? TrailheadMode.Hand : TrailheadMode.Revealed;
        selectedCardIds.Clear();
        currentState.Value = AbilityState.TrailheadSelecting;
    }

    [ServerRpc(RequireOwnership = false)]
    public void TrailheadSelectCardServerRpc(int spriteID, ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != activePlayerId.Value) return;
        if (currentState.Value != AbilityState.TrailheadSelecting &&
            currentState.Value != AbilityState.Done) return; // allow deselection from Done state too

        if (selectedCardIds.Contains(spriteID))
            selectedCardIds.Remove(spriteID);
        else
        {
            if (selectedCardIds.Count >= 2) return;
            selectedCardIds.Add(spriteID);
        }

        // Set state FIRST, then notify clients so Refresh sees the correct state
        currentState.Value = selectedCardIds.Count >= 1
            ? AbilityState.Done
            : AbilityState.TrailheadSelecting;

        // Now notify — Refresh will fire from OnValueChanged on currentState AND from here
        NotifySelectionChangedClientRpc(selectedCardIds.ToArray());
    }


    [ServerRpc(RequireOwnership = false)]
    public void TrailheadConfirmServerRpc(ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != activePlayerId.Value) return;

        PlayerStateNet player = FindPlayerState(activePlayerId.Value);
        if (player == null) return;

        int discardCount = 0;

        foreach (int spriteID in selectedCardIds)
        {
            if (trailheadMode.Value == TrailheadMode.Hand)
            {
                for (int i = 0; i < player.Hand.Count; i++)
                {
                    if (player.Hand[i].spriteId == spriteID)
                    {
                        DeckManager.Instance.AddToDiscard(player.Hand[i]);
                        player.Hand.RemoveAt(i);
                        discardCount++;
                        break;
                    }
                }
            }
            else // Revealed
            {
                for (int i = 0; i < DeckManager.Instance.RevealedCards.Count; i++)
                {
                    if (DeckManager.Instance.RevealedCards[i].spriteId == spriteID)
                    {
                        DeckManager.Instance.AddToDiscard(DeckManager.Instance.RevealedCards[i]);
                        DeckManager.Instance.RevealedCards.RemoveAt(i);
                        discardCount++;
                        break;
                    }
                }
            }
        }

        // Replace AFTER all discards are done, once, with the correct count
        if (trailheadMode.Value == TrailheadMode.Hand)
        {
            // Draw replacement cards into hand
            for (int i = 0; i < discardCount; i++)
            {
                CardData drawn = DeckManager.Instance.DrawTopCard();
                if (drawn.cardId != -1)
                    player.Hand.Add(drawn);
            }
        }
        else
        {
            // Refill revealed cards from deck
            DeckManager.Instance.ReplaceRevealedCards(discardCount);
        }

        ChatManager.Instance.SendSystemMessage($"Client {activePlayerId.Value} used Trailhead! Discarded {discardCount} card(s) and drew replacements.");
        FinishSignPhase();
    }

    // ---- BACKTRACK ----

    [ServerRpc(RequireOwnership = false)]
    public void BacktrackSelectFieldCardServerRpc(int spriteID, ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != activePlayerId.Value) return;
        if (currentState.Value != AbilityState.BacktrackSelectField) return;

        backtrackSelectedFieldCardId.Value = spriteID;
        currentState.Value = AbilityState.BacktrackChooseMode;
    }

    [ServerRpc(RequireOwnership = false)]
    public void BacktrackChooseModeServerRpc(bool swapWithField, ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != activePlayerId.Value) return;
        if (currentState.Value != AbilityState.BacktrackChooseMode) return;

        backtrackMode.Value = swapWithField ? BacktrackMode.Field : BacktrackMode.Hand;
        currentState.Value = AbilityState.BacktrackSelectTarget;
    }

    [ServerRpc(RequireOwnership = false)]
    public void BacktrackSelectTargetServerRpc(int targetCardId, ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != activePlayerId.Value) return;
        if (currentState.Value != AbilityState.BacktrackSelectTarget) return;

        PlayerStateNet player = FindPlayerState(activePlayerId.Value);
        if (player == null) return;

        int sourceId = backtrackSelectedFieldCardId.Value;

        if (backtrackMode.Value == BacktrackMode.Field)
        {
            // Swap two field cards
            int idxA = -1, idxB = -1;
            for (int i = 0; i < player.Field.Count; i++)
            {
                if (player.Field[i].spriteId == sourceId) idxA = i;
                if (player.Field[i].spriteId == targetCardId) idxB = i;
            }
            if (idxA == -1 || idxB == -1) return;

            CardData tmp = player.Field[idxA];
            player.Field[idxA] = player.Field[idxB];
            player.Field[idxB] = tmp;
        }
        else // swap field card with hand card
        {
            DeckManager.Instance.SwapHandAndFieldCardServerRpc(targetCardId, sourceId);
            FinishSignPhase();
            return;
        }

        ChatManager.Instance.SendSystemMessage($"Client {activePlayerId.Value} used Backtrack!");
        FinishSignPhase();
    }

    // ---- SHORTCUT ----

    void ExecuteShortcut()
    {
        PlayerStateNet player = FindPlayerState(activePlayerId.Value);
        if (player == null) return;

        CardData drawn = DeckManager.Instance.DrawTopCard();
        if (drawn.spriteId != -1)
        {
            player.Hand.Add(drawn);
            ChatManager.Instance.SendSystemMessage($"Client {activePlayerId.Value} used Shortcut — drew a card!");
        }

        FinishSignPhase();
    }

    // ============================================================
    //  FINISH
    // ============================================================
    [ClientRpc]
    void ClearHighlightsClientRpc()
    {
        SignAbilityUI.Instance?.ClearAllHighlights();
    }

    void FinishSignPhase()
    {
        if (!IsServer) return;

        selectedCardIds.Clear();
        currentState.Value = AbilityState.Inactive;
        activeSignId.Value = -1;
        backtrackSelectedFieldCardId.Value = -1;
        backtrackMode.Value = BacktrackMode.None;
        trailheadMode.Value = TrailheadMode.None;

        ClearHighlightsClientRpc(); // make sure this is here
        RoundManager.Instance.OnSignPhaseComplete();
    }

    // ============================================================
    //  CLIENT RPCs
    // ============================================================

    [ClientRpc]
    void NotifySelectionChangedClientRpc(int[] selected)
    {
        SignAbilityUI.Instance?.UpdateSelectionHighlights(selected);
    }

    // ============================================================
    //  PUBLIC ACCESSORS for UI
    // ============================================================

    public AbilityState GetState() => currentState.Value;
    public int GetActiveSignId() => activeSignId.Value;
    public ulong GetActivePlayerId() => activePlayerId.Value;
    public TrailheadMode GetTrailheadMode() => trailheadMode.Value;
    public BacktrackMode GetBacktrackMode() => backtrackMode.Value;
    public int GetBacktrackSelectedCard() => backtrackSelectedFieldCardId.Value;
    public bool IsLocalPlayerActive() =>
        NetworkManager.Singleton != null &&
        activePlayerId.Value == NetworkManager.Singleton.LocalClientId;

    PlayerStateNet FindPlayerState(ulong clientId)
    {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return null;
        return client.PlayerObject?.GetComponent<PlayerStateNet>();
    }
}