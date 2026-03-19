using UnityEngine;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;

public class ScoreBreakdownUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject breakdownPanel;
    public Transform breakdownContainer;
    public GameObject playerBreakdownPrefab;

    [Header("Settings")]
    public bool showOnlyLocalPlayer = false;

    private Dictionary<ulong, GameObject> playerBreakdownUIs = new Dictionary<ulong, GameObject>();
    private bool isInitialized = false;

    void Start()
    {
        if (ScoringSystem.Instance != null)
        {
          //  ScoringSystem.Instance.OnScoresUpdated += OnScoresUpdated;
        }

        if (breakdownPanel != null)
        {
            breakdownPanel.SetActive(true);
        }
    }

    void OnDestroy()
    {
        if (ScoringSystem.Instance != null)
        {
            ScoringSystem.Instance.OnScoresUpdated -= OnScoresUpdated;
        }
    }

    void OnScoresUpdated(Dictionary<ulong, int> scores)
    {
        // Initialize on first score update (ensures players are spawned)
        if (!isInitialized)
        {
            //CreateInitialBreakdownUIs();
            isInitialized = true;
        }

        UpdateBreakdownDisplay();
    }

    void CreateInitialBreakdownUIs()
    {
        if (FieldUIManager.Instance == null)
        {
            Debug.LogWarning("ScoreBreakdownUI: FieldUIManager not ready yet");
            return;
        }

        List<ulong> allClientIds = FieldUIManager.Instance.GetAllClientIds();

        if (allClientIds.Count == 0)
        {
            Debug.LogWarning("ScoreBreakdownUI: No clients found yet");
            return;
        }

        Debug.Log($"ScoreBreakdownUI: Creating UI for {allClientIds.Count} clients");

        foreach (ulong clientId in allClientIds)
        {
            // Skip non-local players if setting is enabled
            if (showOnlyLocalPlayer && clientId != NetworkManager.Singleton.LocalClientId)
                continue;

            // Create UI element once
            GameObject breakdownObj = Instantiate(playerBreakdownPrefab, breakdownContainer);
            playerBreakdownUIs[clientId] = breakdownObj;

            Debug.Log($"ScoreBreakdownUI: Created breakdown UI for client {clientId}");
        }
    }

    void UpdateBreakdownDisplay()
    {
        if (ScoringSystem.Instance == null) return;
        if (playerBreakdownUIs.Count == 0) return;

        // Just update existing UI elements
        foreach (var kvp in playerBreakdownUIs)
        {
            ulong clientId = kvp.Key;
            GameObject uiObj = kvp.Value;

            ScoreBreakdown breakdown = ScoringSystem.Instance.GetScoreBreakdown(clientId);
            if (breakdown == null) continue;

            // Update the display
            PlayerBreakdownDisplay display = uiObj.GetComponent<PlayerBreakdownDisplay>();
            if (display != null)
            {
                display.UpdateDisplay(clientId, breakdown);
            }
        }
    }

    public void TogglePanel()
    {
        if (breakdownPanel != null)
        {
            breakdownPanel.SetActive(!breakdownPanel.activeSelf);
        }
    }
}
