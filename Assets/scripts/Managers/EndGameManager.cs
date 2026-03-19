// EndGameManager.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.Collections.Generic;
using TMPro;

public class EndGameManager : NetworkBehaviour
{
    public static EndGameManager Instance;

    [Header("End Game UI")]
    public GameObject endGamePanel;
    public Button rematchButton;
    public Button leaveButton;
    public TextMeshProUGUI rematchStatusText;

    [Header("Cleanup On Rematch")]
    public GameObject[] containersToClear; // assign in Inspector
    public GameObject lobbyCanvas; // assign your lobby canvas in Inspector

    // Tracks which clients have voted for rematch
    private NetworkList<ulong> rematchVotes;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        rematchVotes = new NetworkList<ulong>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
    }

    public override void OnNetworkSpawn()
    {
        rematchVotes.OnListChanged += OnRematchVotesChanged;
        rematchButton.onClick.AddListener(OnRematchClicked);
        leaveButton.onClick.AddListener(OnLeaveClicked);

        // Hide panel at start
        endGamePanel.SetActive(false);

        // Listen for disconnects to reset votes
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    public override void OnNetworkDespawn()
    {
        rematchVotes.OnListChanged -= OnRematchVotesChanged;
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    // Called by RoundManager when the game ends
    public void ShowEndGameUI()
    {
        ShowEndGameUIClientRpc();
    }

    [ClientRpc]
    private void ShowEndGameUIClientRpc()
    {
        endGamePanel.SetActive(true);
        UpdateRematchText();
    }

    // ============================================================
    //  REMATCH
    // ============================================================

    private void OnRematchClicked()
    {
        rematchButton.interactable = false;
        VoteRematchServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void VoteRematchServerRpc(ulong clientId)
    {
        if (rematchVotes.Contains(clientId)) return;
        rematchVotes.Add(clientId);

        int needed = NetworkManager.Singleton.ConnectedClients.Count;
        if (rematchVotes.Count >= needed)
            StartRematch();
    }

    private void StartRematch()
    {
        rematchVotes.Clear();
        HideEndGameUIClientRpc();
        GameStartManager.Instance?.OnStartGameButtonPressed();
        // Re-trigger the game started flow in RoundManager
        RoundManager.Instance?.OnRematch();
    }

    private void OnRematchVotesChanged(NetworkListEvent<ulong> _)
    {
        UpdateRematchText();
    }

    private void UpdateRematchText()
    {
        if (rematchStatusText == null) return;
        int total = NetworkManager.Singleton?.ConnectedClients?.Count ?? 0;
        rematchStatusText.text = $"{rematchVotes.Count}/{total} ready";
    }

    [ClientRpc]
    private void HideEndGameUIClientRpc()
    {
        endGamePanel.SetActive(false);
        rematchButton.interactable = true;
    }

    // ============================================================
    //  LEAVE
    // ============================================================

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        if (rematchVotes.Contains(clientId))
        {
            rematchVotes.Clear();
            NotifyRematchResetClientRpc();
        }

        ReturnToMainMenu();
    }

    private void OnLeaveClicked()
    {
        ReturnToMainMenu();
    }

    [ClientRpc]
    private void NotifyRematchResetClientRpc()
    {
        rematchButton.interactable = true; // re-enable so players can vote again
        UpdateRematchText();
        ChatManager.Instance?.SendSystemMessage("A player left — rematch votes reset.");
    }

    private void ReturnToMainMenu()
    {
        // Destroy all DontDestroyOnLoad objects
        foreach (var obj in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (obj.scene.name == "DontDestroyOnLoad")
                Destroy(obj.transform.root.gameObject);
        }

        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("Main");
    }

}