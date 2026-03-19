// GameStartManager.cs
using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class GameStartManager : NetworkBehaviour
{
    public static GameStartManager Instance;

    [Header("UI References")]
    public GameObject menuUI;
    public GameObject playAreaUI;
    public Button startGameButton;

    private NetworkList<ulong> _turnOrder;
    private NetworkVariable<bool> gameStarted = new NetworkVariable<bool>(false);

    public NetworkList<ulong> TurnOrder { get => _turnOrder; set => _turnOrder = value; }
    public event System.Action<List<ulong>> OnTurnOrderSet;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _turnOrder = new NetworkList<ulong>();
    }

    void Start()
    {
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameButtonPressed);
            UpdateStartButtonVisibility();
        }
        if (playAreaUI != null)
            playAreaUI.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _turnOrder.OnListChanged += OnTurnOrderChanged;
        gameStarted.OnValueChanged += OnGameStartedChanged;
        UpdateStartButtonVisibility();
    }

    void UpdateStartButtonVisibility()
    {
        if (startGameButton != null)
            startGameButton.gameObject.SetActive(IsHost && !gameStarted.Value);
    }

    public void OnStartGameButtonPressed()
    {
        if (!IsHost) return;
        StartGameServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    void StartGameServerRpc()
    {
        ChatManager.Instance?.SendSystemMessage("Starting game...");

        List<ulong> allPlayers = FieldUIManager.Instance.GetAllClientIds();
        if (allPlayers.Count == 0)
        {
            Debug.LogError("GameStartManager: No players found!");
            return;
        }

        // Fisher-Yates shuffle
        List<ulong> randomized = new List<ulong>(allPlayers);
        for (int i = randomized.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            ulong tmp = randomized[i];
            randomized[i] = randomized[j];
            randomized[j] = tmp;
        }

        _turnOrder.Clear();
        foreach (ulong id in randomized)
            _turnOrder.Add(id);

        gameStarted.Value = true;

        ChatManager.Instance?.SendSystemMessage($"Turn order: {string.Join(" -> ", randomized)}");

        StartGameClientRpc();
    }

    [ClientRpc]
    void StartGameClientRpc()
    {
        // Clear any existing content from previous game
        foreach (var obj in GameObject.FindGameObjectsWithTag("clear"))
            for (int i = obj.transform.childCount - 1; i >= 0; i--)
                Destroy(obj.transform.GetChild(i).gameObject);

        if (menuUI != null) menuUI.SetActive(false);
        if (playAreaUI != null) playAreaUI.SetActive(true);

        ActivateAllFields();
        StartCoroutine(CreateButtonsAfterSync());
    }

    IEnumerator CreateButtonsAfterSync()
    {
        int waited = 0;
        while (_turnOrder.Count == 0 && waited < 10)
        {
            yield return null;
            waited++;
        }

        if (_turnOrder.Count == 0)
        {
            Debug.LogError("GameStartManager: Turn order still empty after waiting!");
            yield break;
        }

        CreatePlayerButtonsInOrder();
        FieldUIManager.Instance?.ShowField(_turnOrder[0]);
    }

    void ActivateAllFields()
    {
        if (FieldUIManager.Instance == null) return;
        foreach (ulong clientId in FieldUIManager.Instance.GetAllClientIds())
        {
            GameObject field = FieldUIManager.Instance.GetFieldForClient(clientId);
            if (field != null) field.SetActive(true);
        }
    }

    void CreatePlayerButtonsInOrder()
    {
        FieldCycleController cycleController = FindObjectOfType<FieldCycleController>();
        if (cycleController == null)
        {
            Debug.LogWarning("GameStartManager: FieldCycleController not found!");
            return;
        }
        cycleController.CreatePlayerButtonsInOrder(GetTurnOrder());
    }

    void OnTurnOrderChanged(NetworkListEvent<ulong> changeEvent)
    {
        if (_turnOrder.Count == 0) return;
        OnTurnOrderSet?.Invoke(GetTurnOrder());
    }

    void OnGameStartedChanged(bool oldValue, bool newValue)
    {
        UpdateStartButtonVisibility();
        Debug.Log($"GameStartManager: Game started = {newValue}");
    }

    public List<ulong> GetTurnOrder()
    {
        var order = new List<ulong>();
        foreach (ulong id in _turnOrder) order.Add(id);
        return order;
    }

    public ulong GetFirstPlayer() => _turnOrder.Count == 0 ? 0 : _turnOrder[0];
    public bool HasGameStarted() => gameStarted.Value;
}